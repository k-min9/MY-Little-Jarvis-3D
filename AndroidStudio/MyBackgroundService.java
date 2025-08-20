package com.example.mylittlejarvisandroid;

import static com.example.mylittlejarvisandroid.Bridge.baseUrl;

import android.Manifest;
import android.app.Notification;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.app.Service;
import android.content.Intent;
import android.content.pm.PackageManager;
import android.media.AudioAttributes;
import android.media.AudioFormat;
import android.media.AudioRecord;
import android.media.AudioTrack;
import android.media.MediaPlayer;
import android.media.MediaRecorder;
import android.os.Build;
import android.os.IBinder;
import android.util.Log;

import androidx.core.app.ActivityCompat;
import androidx.core.app.NotificationCompat;
import androidx.core.app.NotificationManagerCompat;

import com.google.gson.Gson;
import com.google.gson.JsonArray;
import com.google.gson.JsonElement;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;
import com.google.gson.JsonSyntaxException;

import java.io.BufferedReader;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.io.OutputStream;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.LinkedList;
import java.util.List;
import java.util.Map;
import java.util.Queue;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.TimeUnit;

import okhttp3.MediaType;
import okhttp3.MultipartBody;
import okhttp3.OkHttpClient;
import okhttp3.RequestBody;
import okhttp3.ResponseBody;
import retrofit2.Call;
import retrofit2.Callback;
import retrofit2.Response;
import retrofit2.Retrofit;
import retrofit2.converter.gson.GsonConverterFactory;

public class MyBackgroundService extends Service {

    // 기본 설정

    private static final String CHANNEL_ID = "Tikitaka Channel";
    private AudioRecord audioRecord; // Replaces AudioClip(Unity)
    private AudioTrack audioTrack;
    private int chatIdx = 0;  // 채팅횟수 + 현재 채팅상황
    private String chatIdxSuccess = "-1";  // 최근 가장 성공한 채팅번호(CallConversationStream 참조)

    // VAD Check
    private static final int SAMPLE_RATE = 16000; // 16kHz
    private static final int BUFFER_SIZE = SAMPLE_RATE / 2;  // 0.5초
    private static final float VAD_LAST_SEC = 1.25f; // VAD 판단 기준 (최근 1.25초)
    private static final float VAD_THRESHOLD = 1.0f; // VAD 에너지 활성화 기준
    private static final float VAD_FREQ_THRESHOLD = 100.0f; // VAD 필터 주파수 기준
    private static final float VAD_CONTEXT_SEC = 30f; // 최대 30초간 데이터를 유지
    private static final int CONTEXT_SAMPLES = (int) (SAMPLE_RATE * VAD_CONTEXT_SEC); // 최대 30초의 샘플 개수

    // VAD Status
    private boolean isRecording = false;
    private boolean isVoiceActive = false;
    private List<short[]> recordedAudio = new ArrayList<>();
    private LinkedList<short[]> audioContext = new LinkedList<>(); // 최근 30초간 데이터를 저장
    private static final int GAP_LIMIT = 4; // 유예 기간 (0.5초 * 4 = 2초)
    private int gapCounter = 0;
    private int outputCounter = 0;
    private int sttRecordCounter = 0;
    ExecutorService streamingExecutor = Executors.newFixedThreadPool(5); // 스트림 전용
    ExecutorService apiExecutor = Executors.newFixedThreadPool(10); // 일반 API

    // WAV
    private MediaPlayer mediaPlayer;
    private final Queue<File> audioQueue = new LinkedList<>();




    @Override
    public void onCreate() {
        super.onCreate();
        Log.i("SERVICE", "Service created.");
    }

    @Override
    public int onStartCommand(Intent intent, int flags, int startId) {
        try {
            Log.i("SERVICE", "onStartCommand notification start");
            createNotificationChannel();
            startNotification();
            Log.i("SERVICE", "onStartCommand notification end");
            super.onCreate();

            Log.i("SERVICE", "startVAD thread call");
            startVAD();
            Log.i("SERVICE", "startVAD thread start");
        } catch (Exception e) {
            Log.e("SERVICE", "onStartCommand error" + e.getMessage());
        }

        return START_NOT_STICKY;
    }

    private void setupAudioRecord() {
        Log.i("SERVICE", "RECORD_AUDIO checking.");

        int audioSource = MediaRecorder.AudioSource.MIC;
        int channelConfig = AudioFormat.CHANNEL_IN_MONO;
        int audioFormat = AudioFormat.ENCODING_PCM_16BIT;

        if (ActivityCompat.checkSelfPermission(this, Manifest.permission.RECORD_AUDIO) != PackageManager.PERMISSION_GRANTED) {
            // 임시
            Log.i("SERVICE", "setupAudioRecord no PERMISSION_GRANTED");
            return;
        }

        Log.i("SERVICE", "RECORD_AUDIO call.");
        audioRecord = new AudioRecord(
                audioSource,
                16000,
                channelConfig,
                audioFormat,
                BUFFER_SIZE
        );
        Log.i("SERVICE", "RECORD_AUDIO start.");
    }

    private void setupAudioTrack() {
        audioTrack = new AudioTrack.Builder()
                .setAudioAttributes(new AudioAttributes.Builder()
                        .setUsage(AudioAttributes.USAGE_MEDIA)
                        .setContentType(AudioAttributes.CONTENT_TYPE_MUSIC)
                        .build())
                .setAudioFormat(new AudioFormat.Builder()
                        .setEncoding(AudioFormat.ENCODING_PCM_16BIT)
                        .setSampleRate(32000)  // SAMPLE_RATE 16000이 아니라 Server output 기준 적용 > 그 외에는 getSampleRate 함수 참조
                        .setChannelMask(AudioFormat.CHANNEL_OUT_MONO)
                        .build())
                .setBufferSizeInBytes(BUFFER_SIZE)
                .setTransferMode(AudioTrack.MODE_STREAM)  // 작은 WAV 파일의 경우 MODE_STATIC이 더 적합
                .build();
    }

    public void startVAD() {
        Log.d("SERVICE VAD", "startVAD start");

        setupAudioRecord();
//        setupAudioTrack();

        if (audioRecord.getState() != AudioRecord.STATE_INITIALIZED) {
            Log.e("SERVICE VAD", "AudioRecord initialization failed!");
            return;
        }

        audioRecord.startRecording();
        isRecording = true;
        recordedAudio = new ArrayList<>();

        new Thread(() -> {
            short[] buffer = new short[BUFFER_SIZE];
            short[] initBuffer = null;  // 앞 부분 잘리는거 의식해서 조금 추가
            while (isRecording) {
                int read = audioRecord.read(buffer, 0, buffer.length);
                if (read > 0) {
                    // 최근 데이터 유지 (최대 30초)
                    manageAudioContext(buffer);

                    // VAD 감지
                    boolean isCurrentlyActive = evaluateVad();

                    // 저장할 wav
                    if (isCurrentlyActive) {
                        Log.d("SERVICE VAD", "simpleVad : " + isCurrentlyActive);
                        gapCounter = 0; // 유예 카운터 초기화
                        if (!isVoiceActive) {
                            Log.d("SERVICE VAD", "startVAD recoding start");
                            isVoiceActive = true;
                            recordedAudio.clear(); // 새로운 녹음 세션 시작
                            if (initBuffer != null) {
                                recordedAudio.add(initBuffer.clone()); // 존재할 경우 앞부분 0.5초 데이터를 추가
                            }
                        }
                        recordedAudio.add(buffer.clone());
                    } else if (isVoiceActive) {
                        Log.d("SERVICE VAD", "simpleVad : " + isCurrentlyActive + "(" + gapCounter + ")");
                        gapCounter++;
                        recordedAudio.add(buffer.clone()); // 유예 동안 데이터를 계속 저장
                        if (gapCounter >= GAP_LIMIT) {
                            isVoiceActive = false; // 유예 기간 종료
                            // recordedAudio의 복사본 생성하여 wav 파일 저장
                            List<short[]> audioDataCopy = new ArrayList<>(recordedAudio);
                            saveWavFile(audioDataCopy); // WAV 파일 저장

                            List<short[]> finalBuffer = new ArrayList<>();
                            finalBuffer.add(buffer.clone());
                            saveWavFile(finalBuffer);

                            initBuffer = null;  // 다음 저장을 위해 초기화
                        }
                    }

                    // VAD 비활성 상태에서면 잘리지 않게 최신 데이터 저장
                    if (!isCurrentlyActive) {
                        initBuffer = buffer.clone();
                    }
                }
            }
        }).start();
    }

    // 최근 데이터 관리 (최대 30초 유지)
    private void manageAudioContext(short[] buffer) {
        audioContext.add(buffer.clone());
        int totalSamples = audioContext.size() * BUFFER_SIZE;
        while (totalSamples > CONTEXT_SAMPLES) {
            audioContext.removeFirst();
            totalSamples -= BUFFER_SIZE;
        }
    }

    // VAD 판단 로직
    private boolean evaluateVad() {
        // 최근 1.25초 데이터를 float[]로 추출
        int vadSamples = (int) (SAMPLE_RATE * VAD_CONTEXT_SEC);
        LinkedList<short[]> recentData = new LinkedList<>();
        int totalSamples = 0;

        for (int i = audioContext.size() - 1; i >= 0; i--) {
            short[] chunk = audioContext.get(i);
            recentData.addFirst(chunk);
            totalSamples += chunk.length;
            if (totalSamples >= vadSamples) break;
        }

        // 데이터를 float[]로 병합
        float[] floatData = new float[totalSamples];
        int index = 0;
        for (short[] chunk : recentData) {
            for (short sample : chunk) {
                floatData[index++] = sample / 32768.0f;
            }
        }

        // VAD 알고리즘 실행
        return simpleVad(floatData, SAMPLE_RATE, VAD_LAST_SEC, VAD_THRESHOLD, VAD_FREQ_THRESHOLD);
    }


    // WAV 파일 저장
    private void saveWavFile(List<short[]> audioData) {
        Log.d("SERVICE VAD", "saveWavFile start : " + audioData.size());
        sttRecordCounter = (sttRecordCounter+1)%10;
        File outputFile = new File(getExternalFilesDir(null),
                "recorded_audio" + sttRecordCounter + ".wav");
        try (FileOutputStream fos = new FileOutputStream(outputFile)) {
            // WAV 헤더 작성
            writeWavHeader(fos, audioData.size() * BUFFER_SIZE * 2);

            // 오디오 데이터 작성
            for (short[] buffer : audioData) {
                ByteBuffer byteBuffer = ByteBuffer.allocate(buffer.length * 2);
                byteBuffer.order(ByteOrder.LITTLE_ENDIAN);
                for (short sample : buffer) {
                    byteBuffer.putShort(sample);
                }
                fos.write(byteBuffer.array());
            }

            Log.d("SERVICE VAD", "saveWavFile end : " + sttRecordCounter + ".wav");

            sendWav(outputFile);
        } catch (IOException e) {
            Log.e("SERVICE VAD", "saveWavFile start");
            e.printStackTrace();
        }
    }

    // WAV 헤더 작성
    private void writeWavHeader(FileOutputStream fos, int totalAudioLen) throws IOException {
        int totalDataLen = totalAudioLen + 36;
        int byteRate = SAMPLE_RATE * 2; // 16비트 모노

        byte[] header = new byte[44];
        header[0] = 'R'; header[1] = 'I'; header[2] = 'F'; header[3] = 'F';
        header[4] = (byte) (totalDataLen & 0xff);
        header[5] = (byte) ((totalDataLen >> 8) & 0xff);
        header[6] = (byte) ((totalDataLen >> 16) & 0xff);
        header[7] = (byte) ((totalDataLen >> 24) & 0xff);
        header[8] = 'W'; header[9] = 'A'; header[10] = 'V'; header[11] = 'E';
        header[12] = 'f'; header[13] = 'm'; header[14] = 't'; header[15] = ' ';
        header[16] = 16; header[17] = 0; header[18] = 0; header[19] = 0;
        header[20] = 1; header[21] = 0; header[22] = 1; header[23] = 0;
        header[24] = (byte) (SAMPLE_RATE & 0xff);
        header[25] = (byte) ((SAMPLE_RATE >> 8) & 0xff);
        header[26] = (byte) ((SAMPLE_RATE >> 16) & 0xff);
        header[27] = (byte) ((SAMPLE_RATE >> 24) & 0xff);
        header[28] = (byte) (byteRate & 0xff);
        header[29] = (byte) ((byteRate >> 8) & 0xff);
        header[30] = (byte) ((byteRate >> 16) & 0xff);
        header[31] = (byte) ((byteRate >> 24) & 0xff);
        header[32] = 2; header[33] = 0; header[34] = 16; header[35] = 0;
        header[36] = 'd'; header[37] = 'a'; header[38] = 't'; header[39] = 'a';
        header[40] = (byte) (totalAudioLen & 0xff);
        header[41] = (byte) ((totalAudioLen >> 8) & 0xff);
        header[42] = (byte) ((totalAudioLen >> 16) & 0xff);
        header[43] = (byte) ((totalAudioLen >> 24) & 0xff);

        fos.write(header, 0, 44);
    }

    public static boolean simpleVad(float[] data, int sampleRate, float lastSec, float vadThd, float freqThd) {
        int num = data.length;
        int num2 = (int) (sampleRate * lastSec);
//        Log.d("SERVICE VAD", "num : " + num + " / num2 : " + num2);
        if (num2 >= num) {
            return false;
        }

        if (freqThd > 0f) {
//            Log.d("SERVICE VAD", "highPassFilter start : " + Arrays.toString(data));
            highPassFilter(data, freqThd, sampleRate);
//            Log.d("SERVICE VAD", "highPassFilter end : " + Arrays.toString(data));
        }

        float totalEnergy = 0f;
        float lastSecEnergy = 0f;
        for (int i = 0; i < num; i++) {
            totalEnergy += Math.abs(data[i]);
            if (i >= num - num2) {
                lastSecEnergy += Math.abs(data[i]);
            }
        }

        totalEnergy /= num;
        lastSecEnergy /= num2;

//        Log.d("SERVICE VAD", "totalEnergy : " + totalEnergy + " / lastSecEnergy : " + lastSecEnergy);
        return lastSecEnergy > vadThd * totalEnergy;
    }

    public static void highPassFilter(float[] data, float cutoff, int sampleRate) {
        if (data.length == 0) {
            return;
        }

        float rc = 1f / (2f * (float) Math.PI * cutoff);
        float dt = 1f / sampleRate;
        float alpha = dt / (rc + dt);

        float previous = data[0];
        for (int i = 1; i < data.length; i++) {
            previous = alpha * (previous + data[i] - data[i - 1]);
            data[i] = previous;
        }
    }

    @Override
    public void onDestroy() {
        super.onDestroy();
        stopRecording();
        stopForeground(true);  // 알림 초기화
        Log.i("SERVICE", "Service destroyed.");
    }

    private void stopRecording() {
        if (audioRecord != null) {
            isRecording = false;
            audioRecord.stop();
            audioRecord.release();
            audioRecord = null;
        }
        if (audioTrack != null) {
            audioTrack.release();
            audioTrack = null;
        }
    }

    @Override
    public IBinder onBind(Intent intent) {
        return null;
    }

    // 유니티로 메시지 전송
    private void UnitySendMessage(String gameObject, String methodName, String param) {
        try {
            Log.i("SERVICE", "[Android] ["+methodName+"] send start");
            com.unity3d.player.UnityPlayer.UnitySendMessage(gameObject, methodName, param);
            Log.i("SERVICE", "[Android] ["+methodName+"] send end");
        } catch (Exception e) {
            Log.e("SERVICE", "[Android] 유니티 메시지 전송 오류: " + e.getMessage());
        }
    }

    private void createNotificationChannel() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            final NotificationChannel notificationChannel = new NotificationChannel(
                    CHANNEL_ID,
                    "Service Channel",
                    NotificationManager.IMPORTANCE_LOW
            );
            final NotificationManagerCompat notificationManager = NotificationManagerCompat.from(this);
            notificationManager.createNotificationChannel(notificationChannel);
        }
    }

    private void startNotification(){
        // 알림 삭제 시 수행할 PendingIntent 설정
        Intent deleteIntent = new Intent(this, NotificationDeleteReceiver.class);
        PendingIntent deletePendingIntent = PendingIntent.getBroadcast(
                this,
                0,
                deleteIntent,
                PendingIntent.FLAG_UPDATE_CURRENT | PendingIntent.FLAG_IMMUTABLE
        );
        // 클릭시 프로그램 최대화
        Intent notificationIntent = new Intent(this, Bridge.unityActivity.getClass());
        PendingIntent pendingIntent = PendingIntent.getActivity(this,
                0, notificationIntent, PendingIntent.FLAG_IMMUTABLE);
        // 알림 Start
        Notification notification = new NotificationCompat.Builder(this, CHANNEL_ID)
                .setContentTitle("Service Running")
                .setContentText("ARONA is listening...")
                .setSmallIcon(R.drawable.custom_icon)
                .setContentIntent(pendingIntent)
                .setDeleteIntent(deletePendingIntent)
                .setOngoing(true)
                .build();
        startForeground(112, notification);  // SERVICE_NOTIFICATION_ID : 112
    }

    private void playWav(String fileName) {
        Log.d("SERVICE", "playWav Start : " + fileName);
        File file = new File(getExternalFilesDir(null), fileName);

        if (!file.exists()) {
            Log.e("SERVICE", "WAV file not found: " + file.getAbsolutePath());
            return;
        }

//        Log.d("SERVICE", "playWav File Info sampleRate : " + getSampleRate(file));
        try (FileInputStream inputStream = new FileInputStream(file)) {
            // WAV 헤더 스킵 (44바이트)
            inputStream.skip(44);

            audioTrack.play();
            Log.d("SERVICE", "AudioTrack playing: " + fileName);

            // PCM 데이터를 읽어 재생
            byte[] buffer = new byte[BUFFER_SIZE];
            int bytesRead;
            while ((bytesRead = inputStream.read(buffer)) != -1) {
                audioTrack.write(buffer, 0, bytesRead);
            }

            Log.d("SERVICE", "Audio playback completed: " + fileName);
        } catch (IOException e) {
            Log.e("SERVICE", "Error playing WAV: " + e.getMessage());
        }
    }

    // STT 송신 (Unity VADController.SendWavFile 로직 기반)
    public void sendWav(File file) {
        if (!file.exists()) {
            Log.e("SERVICE", "File not found: " + file.getAbsolutePath());
            return;
        }

        // Unity 로직: chatIdx 증가 및 regenerateCount 초기화
        chatIdx = chatIdx + 1;
        
        // Unity 로직: server_type_idx 체크 (Android에서는 내부 Whisper 없으므로 외부 서버 사용)
        String serverTypeIdx = Bridge.server_type_idx != null ? Bridge.server_type_idx : "0";
        if ("2".equals(serverTypeIdx)) {
            Log.d("SERVICE STT", "Note: Unity uses internal Whisper for server_type_idx=2, but Android sends to external server");
        }
        
        Log.d("SERVICE STT", "Starting STT request - chatIdx: " + chatIdx + ", server_type_idx: " + serverTypeIdx);

        Retrofit retrofit = new Retrofit.Builder()
                .baseUrl(baseUrl)
                .addConverterFactory(GsonConverterFactory.create())
                .callbackExecutor(apiExecutor)
                .build();
        ApiService apiService = retrofit.create(ApiService.class);

        // Unity와 동일한 파라미터 구성
        RequestBody requestFile = RequestBody.create(MediaType.parse("audio/wav"), file);
        MultipartBody.Part filePart = MultipartBody.Part.createFormData("file", "stt.wav", requestFile);

        RequestBody lang = RequestBody.create(MediaType.parse("text/plain"), "ko");
        RequestBody level = RequestBody.create(MediaType.parse("text/plain"), "small");
        RequestBody chatIdxStr = RequestBody.create(MediaType.parse("text/plain"), String.valueOf(chatIdx));

        // API 호출
        Call<JsonObject> call = apiService.uploadAudio(filePart, lang, level, chatIdxStr);
        call.enqueue(new Callback<JsonObject>() {
            @Override
            public void onResponse(Call<JsonObject> call, Response<JsonObject> response) {
                if (response.isSuccessful()) {
                    JsonObject jsonResponse = response.body();
                    if (jsonResponse != null) {
                        try {
                            String transText = jsonResponse.get("text").getAsString();
                            String transLang = jsonResponse.get("lang").getAsString();
                            String responseChatIdx = jsonResponse.get("chatIdx").getAsString();

                            Log.d("SERVICE STT", "STT Success - Text: " + transText + ", Lang: " + transLang + ", ChatIdx: " + responseChatIdx);

                            // Unity 로직: 빈 결과 체크
                            if (transText == null || transText.trim().isEmpty()) {
                                Log.w("SERVICE STT", "STT result is empty. Skipping response processing.");
                                return;
                            }

                            // Unity 스타일 알림 업데이트 (Unity의 NoticeBalloonManager 역할)
                            UnitySendMessage("GameManager", "OnSTTResult", transText);

                            // 대화 스트림 시작
                            callConversationStream(transText, responseChatIdx);

                            // 기존 음성 중지 및 초기화
                            ResetAudio();

                        } catch (Exception e) {
                            Log.e("SERVICE STT", "Error parsing STT response: " + e.getMessage());
                        }
                    }
                } else {
                    Log.e("SERVICE STT", "STT request failed. Response Code: " + response.code());
                    try {
                        String errorBody = response.errorBody() != null ? response.errorBody().string() : "No error details";
                        Log.e("SERVICE STT", "Error details: " + errorBody);
                    } catch (Exception e) {
                        Log.e("SERVICE STT", "Error reading error body: " + e.getMessage());
                    }
                }
            }

            @Override
            public void onFailure(Call<JsonObject> call, Throwable t) {
                Log.e("SERVICE STT", "STT request failed: " + t.getMessage());
                t.printStackTrace();
            }
        });
    }

    public void callConversationStream(String query, String chatIdx) {
        callConversationStream(query, chatIdx, "");
    }

    // Unity CallConversationStream 로직 기반으로 개선된 대화 스트림 호출
    public void callConversationStream(String query, String chatIdx, String aiLangIn) {
        
        // Unity 로직: chatIdxSuccess 업데이트
        if (!"-1".equals(chatIdx)) {
            chatIdxSuccess = chatIdx;
            Log.d("SERVICE API", "Updated chatIdxSuccess: " + chatIdxSuccess);
        }

        // Unity 로직: server_type_idx 분기 처리
        String serverTypeIdx = Bridge.server_type_idx != null ? Bridge.server_type_idx : "0";
        
        String streamUrl;
        if ("2".equals(serverTypeIdx)) {
            // server_type_idx == 2 (Free Gemini): conversation_stream_gemini 사용
            streamUrl = baseUrl + "/conversation_stream_gemini";
            Log.d("SERVICE API", "Using Gemini conversation stream (server_type_idx=2)");
        } else {
            // 기본: conversation_stream 사용
            streamUrl = baseUrl + "/conversation_stream";
            Log.d("SERVICE API", "Using default conversation stream (server_type_idx=" + serverTypeIdx + ")");
        }
        Log.d("SERVICE API", "Starting conversation stream - URL: " + streamUrl);

        // Unity와 동일한 파라미터 구성
        String nickname = Bridge.nickname != null ? Bridge.nickname : "arona";
        String playerName = Bridge.player_name != null ? Bridge.player_name : "sensei";
        String aiLanguage = "";  // 추론 언어 설정
        String aiLanguageIn = aiLangIn;  // STT에서 감지된 언어
        String aiLanguageOut = Bridge.sound_language != null ? Bridge.sound_language : "ko";  // TTS 출력 언어

        // Unity 스타일 메모리 가져오기
        String memoryJson = ConversationManager.getAllConversationMemory();
        Log.d("SERVICE API", "Memory loaded: " + (memoryJson != null ? memoryJson.length() + " chars" : "null"));

        // Unity 스타일 요청 데이터 구성
        Map<String, String> requestData = new HashMap<>();
        requestData.put("query", query);
        requestData.put("player", playerName);
        requestData.put("char", nickname);
        requestData.put("ai_language", aiLanguage);
        requestData.put("ai_language_in", aiLanguageIn);
        requestData.put("ai_language_out", aiLanguageOut);
        requestData.put("ai_emotion", "off");
        requestData.put("memory", memoryJson);
        requestData.put("chatIdx", chatIdx);
        
        // Unity 추가 파라미터들 (기본값으로 설정)
        requestData.put("api_key_Gemini", "");
        requestData.put("api_key_OpenRouter", "");
        requestData.put("api_key_ChatGPT", "");
        requestData.put("guideline_list", "[]");  // Unity UIUserCardManager 역할
        requestData.put("situation", "{}");  // Unity UIChatSituationManager 역할
        requestData.put("intent_web", "off");  // 웹 검색 설정
        requestData.put("intent_image", "off");  // 이미지 생성 설정
        requestData.put("intent_confirm", "false");  // 의도 확인 설정
        requestData.put("intent_confirm_type", "");
        requestData.put("intent_confirm_answer", "");
        requestData.put("regenerate_count", "0");  // Unity 재생성 카운트
        requestData.put("server_type", "Auto");

        Log.d("SERVICE API", "Request data prepared - Query: " + query + ", ChatIdx: " + chatIdx);

        fetchStreamingData(streamUrl, requestData);
    }

    public void fetchStreamingData(String url, Map<String, String> data) {
        String jsonData = new Gson().toJson(data);
        String curChatIdx = data.get("chatIdx");
        // 공용변수 최신화
        if(!"-1".equals(curChatIdx)) {
            chatIdxSuccess = curChatIdx;
        }

        OkHttpClient client = new OkHttpClient.Builder()
                .connectTimeout(100, TimeUnit.SECONDS)
                .readTimeout(100,TimeUnit.SECONDS).build();

        Retrofit retrofit = new Retrofit.Builder()
                .baseUrl(baseUrl)
                .client(client)
                .addConverterFactory(GsonConverterFactory.create())
                .callbackExecutor(streamingExecutor)
                .build();

        ApiService apiService = retrofit.create(ApiService.class);

        RequestBody requestBody = RequestBody.create(MediaType.parse("application/json"), jsonData);
        Call<ResponseBody> call = apiService.streamConversation(requestBody);

        call.enqueue(new Callback<ResponseBody>() {
            @Override
            public void onResponse(Call<ResponseBody> call, Response<ResponseBody> response) {
                if (response.isSuccessful()) {
                    JsonObject lastJsonObject = null;

                    try (BufferedReader reader = new BufferedReader(new InputStreamReader(response.body().byteStream()))) {
                        String line;
                        while ((line = reader.readLine()) != null) {
                            Log.d("SERVICE API","line : " + line);
                            if (!line.isEmpty()) {
                                try {
                                    JsonParser parser = new JsonParser();
                                    JsonObject jsonObject = parser.parse(line).getAsJsonObject();

                                    // 최신 대화 처리 로직 미반영
                                    processReply(jsonObject);

                                    // lastJsonObject 갱신
                                    lastJsonObject = jsonObject;
                                } catch (JsonSyntaxException e) {
                                    Log.e("SERVICE API","JSON decode error: " + e.getMessage());
                                }
                            }
                        }

                        // OnFinalResponseReceived 호출
                        OnFinalResponseReceived(lastJsonObject);

                    } catch (IOException e) {
                        e.printStackTrace();
                    }
                } else {
                    System.err.println("Request failed. Response Code: " + response.code());
                }
            }

            @Override
            public void onFailure(Call<ResponseBody> call, Throwable t) {
                t.printStackTrace();
            }
        });
    }

    // 최후의 대화를 save
    private void OnFinalResponseReceived(JsonObject lastJsonObject) {
        if (lastJsonObject == null) return;
        Log.d("SERVICE API", "OnFinalResponseReceived start.");


        String query_trans = lastJsonObject.getAsJsonObject("query").get("text").getAsString();

        List<String> replyListEn = new ArrayList<>();  // 저장용 대화
        JsonArray replyArray = lastJsonObject.getAsJsonArray("reply_list");
        for (JsonElement replyElement : replyArray) {
            JsonObject reply = replyElement.getAsJsonObject();
            String answerEn = reply.has("answer_en") ? reply.get("answer_en").getAsString() : "";
            replyListEn.add(answerEn);
        }
        String replyEn = String.join(" ", replyListEn);
        
        // 대화 내용 저장
        ConversationManager.saveConversationMemory("player", query_trans, query_trans);
        ConversationManager.saveConversationMemory("character", replyEn, replyEn);
    }

    private void processReply(JsonObject jsonObject) {
        Log.d("SERVICE API","ProcessReply started.");
        Log.d("SERVICE API", "jsonObject : " + String.valueOf(jsonObject));

//         // 말풍선 없으니 필요없긴 함
//        List<String> replyListKo = new ArrayList<>();
//        List<String> replyListJp = new ArrayList<>();
//        List<String> replyListEn = new ArrayList<>();  // 저장용 대화
        String query_trans = jsonObject.getAsJsonObject("query").get("text").getAsString();

        JsonArray replyArray = jsonObject.getAsJsonArray("reply_list");
        String chatIdx = jsonObject.get("chat_idx").getAsString();

        // 현재 대화가 최신인지 체크
        if (chatIdxSuccess == null || !chatIdxSuccess.equals(chatIdx)) {
            Log.d("SERVICE API", "chatIdx Too Old : " + chatIdx + "/"+ chatIdxSuccess);
            return;
        }

        if (replyArray != null) {
            String answerVoice = null;

            for (JsonElement replyElement : replyArray) {
                JsonObject reply = replyElement.getAsJsonObject();

                String answerJp = reply.has("answer_jp") ? reply.get("answer_jp").getAsString() : "";
                String answerKo = reply.has("answer_ko") ? reply.get("answer_ko").getAsString() : "";
                String answerEn = reply.has("answer_en") ? reply.get("answer_en").getAsString() : "";

                answerVoice = answerJp;

                if (!answerJp.isEmpty()) {
//                    replyListJp.add(answerJp);
                    if ("jp".equals(Bridge.sound_language)) {
                        answerVoice = answerJp;
                    }
                }

                if (!answerKo.isEmpty()) {
//                    replyListKo.add(answerKo);
                    if ("ko".equals(Bridge.sound_language)) {
                        answerVoice = answerKo;
                    }
                }

                if (!answerEn.isEmpty()) {
//                    replyListEn.add(answerEn);
                    if ("en".equals(Bridge.sound_language)) {
                        answerVoice = answerEn;
                    }
                }
            }

//            String replyKo = String.join(" ", replyListKo);
//            String replyJp = String.join(" ", replyListJp);
//            String replyEn = String.join(" ", replyListEn);
//            Log.d("SERVICE API","Reply (Ko): " + replyKo);
//            Log.d("SERVICE API","Reply (Jp): " + replyJp);
//            Log.d("SERVICE API","Reply (En): " + replyEn);

            Log.d("SERVICE API","answerVoice: " + answerVoice);

            if (answerVoice != null) {
                if ("ko".equals(Bridge.sound_language) || "en".equals(Bridge.sound_language)) {
                    getKoWavFromAPI(answerVoice, chatIdx);
                } else if ("jp".equals(Bridge.sound_language)) {
                    getJpWavFromAPI(answerVoice, chatIdx);
                } else {
                    getJpWavFromAPI(answerVoice, chatIdx);
                }
            }
        }
    }

    // Unity GetJpWavFromAPI 로직 기반 일본어 TTS
    public void getJpWavFromAPI(String text, String chatIdx) {
        Log.d("SERVICE API", "Starting Japanese TTS - Text: " + text + ", ChatIdx: " + chatIdx);
        
        // Unity 로직: server_type_idx == 2일 때 dev_voice 서버 사용
        String serverTypeIdx = Bridge.server_type_idx != null ? Bridge.server_type_idx : "0";
        String ttsBaseUrl = baseUrl;
        
        if ("2".equals(serverTypeIdx) && Bridge.dev_voice_url != null && !Bridge.dev_voice_url.isEmpty()) {
            ttsBaseUrl = Bridge.dev_voice_url;
            Log.d("SERVICE API", "Using dev_voice server for JP TTS (server_type_idx=2): " + ttsBaseUrl);
        } else {
            Log.d("SERVICE API", "Using default server for JP TTS (server_type_idx=" + serverTypeIdx + "): " + ttsBaseUrl);
        }
        
        // Retrofit 설정
        Retrofit retrofit = new Retrofit.Builder()
                .baseUrl(ttsBaseUrl)
                .addConverterFactory(GsonConverterFactory.create())
                .callbackExecutor(apiExecutor)
                .build();
        ApiService apiService = retrofit.create(ApiService.class);

        // Unity 스타일 요청 데이터 생성
        JsonObject requestData = new JsonObject();
        requestData.addProperty("text", text);
        requestData.addProperty("char", Bridge.nickname != null ? Bridge.nickname : "arona");
        requestData.addProperty("lang", "ja");  // Unity GetJpWavFromAPI와 동일
        requestData.addProperty("speed", Bridge.sound_speed != null ? Bridge.sound_speed : "1.0");
        requestData.addProperty("chatIdx", chatIdx);

        // API 호출
        Log.d("SERVICE API", "Calling JP TTS API with params: " + requestData.toString());
        Call<ResponseBody> call = apiService.synthesizeSound(requestData);
        call.enqueue(new Callback<ResponseBody>() {
            @Override
            public void onResponse(Call<ResponseBody> call, Response<ResponseBody> response) {
                Log.d("SERVICE API", "JP TTS response received");
                if (response.isSuccessful()) {
                    try {
                        // Unity 로직: Chat-Idx 헤더 체크하여 과거 대화 무시
                        String chatIdxHeader = response.headers().get("Chat-Idx");
                        if (chatIdxHeader != null) {
                            int chatIdxHeaderNum = Integer.parseInt(chatIdxHeader);
                            int chatIdxSuccessNum = Integer.parseInt(chatIdxSuccess);
                            if (chatIdxSuccessNum > chatIdxHeaderNum) {
                                Log.d("SERVICE API", "Ignoring old voice: current=" + chatIdxSuccess + ", response=" + chatIdxHeader);
                                return;
                            }
                        }

                        // 응답 파일 저장 경로 설정
                        outputCounter = (outputCounter + 1) % 10;
                        File outputFile = new File(getExternalFilesDir(null), "android_output" + outputCounter + ".wav");

                        // 파일 저장
                        Log.d("SERVICE API", "Saving JP TTS WAV: " + outputFile.getAbsolutePath());
                        if (saveResponseToFile(response.body(), outputFile)) {
                            Log.d("SERVICE API", "JP TTS WAV saved successfully");

                            // Unity 스타일 음성 재생 관리
                            manageAudioPlayback(outputFile);
                        }
                    } catch (Exception e) {
                        Log.e("SERVICE API", "Error processing JP TTS response: " + e.getMessage());
                        e.printStackTrace();
                    }
                } else {
                    Log.e("SERVICE API", "JP TTS request failed, status: " + response.code());
                    try {
                        String errorBody = response.errorBody() != null ? response.errorBody().string() : "No error details";
                        Log.e("SERVICE API", "JP TTS error details: " + errorBody);
                    } catch (Exception e) {
                        Log.e("SERVICE API", "Error reading JP TTS error body: " + e.getMessage());
                    }
                }
            }

            @Override
            public void onFailure(Call<ResponseBody> call, Throwable t) {
                Log.e("SERVICE API", "JP TTS request failed: " + t.getMessage());
                t.printStackTrace();
            }
        });
    }


    // Unity GetKoWavFromAPI 로직 기반 한국어/영어 TTS
    public void getKoWavFromAPI(String text, String chatIdx) {
        Log.d("SERVICE API", "Starting Korean/English TTS - Text: " + text + ", ChatIdx: " + chatIdx);
        
        // Unity 로직: server_type_idx == 2일 때 dev_voice 서버 사용
        String serverTypeIdx = Bridge.server_type_idx != null ? Bridge.server_type_idx : "0";
        String ttsBaseUrl = baseUrl;
        
        if ("2".equals(serverTypeIdx) && Bridge.dev_voice_url != null && !Bridge.dev_voice_url.isEmpty()) {
            ttsBaseUrl = Bridge.dev_voice_url;
            Log.d("SERVICE API", "Using dev_voice server for KO TTS (server_type_idx=2): " + ttsBaseUrl);
        } else {
            Log.d("SERVICE API", "Using default server for KO TTS (server_type_idx=" + serverTypeIdx + "): " + ttsBaseUrl);
        }
        
        // Retrofit 설정
        Retrofit retrofit = new Retrofit.Builder()
                .baseUrl(ttsBaseUrl)
                .addConverterFactory(GsonConverterFactory.create())
                .callbackExecutor(apiExecutor)
                .build();
        ApiService apiService = retrofit.create(ApiService.class);

        // Unity 스타일 요청 데이터 생성
        JsonObject requestData = new JsonObject();
        requestData.addProperty("text", text);
        requestData.addProperty("char", Bridge.nickname != null ? Bridge.nickname : "arona");
        requestData.addProperty("lang", Bridge.sound_language != null ? Bridge.sound_language : "ko");
        requestData.addProperty("speed", Bridge.sound_speed != null ? Bridge.sound_speed : "1.0");
        requestData.addProperty("chatIdx", chatIdx);

        // API 호출
        Log.d("SERVICE API", "Calling KO TTS API with params: " + requestData.toString());
        Call<ResponseBody> call = apiService.synthesizeSoundKo(requestData);
        call.enqueue(new Callback<ResponseBody>() {
            @Override
            public void onResponse(Call<ResponseBody> call, Response<ResponseBody> response) {
                Log.d("SERVICE API", "KO TTS response received");
                if (response.isSuccessful()) {
                    try {
                        // Unity 로직: Chat-Idx 헤더 체크하여 과거 대화 무시
                        String chatIdxHeader = response.headers().get("Chat-Idx");
                        if (chatIdxHeader != null) {
                            int chatIdxHeaderNum = Integer.parseInt(chatIdxHeader);
                            int chatIdxSuccessNum = Integer.parseInt(chatIdxSuccess);
                            if (chatIdxSuccessNum > chatIdxHeaderNum) {
                                Log.d("SERVICE API", "Ignoring old voice: current=" + chatIdxSuccess + ", response=" + chatIdxHeader);
                                return;
                            }
                        }

                        // 응답 파일 저장 경로 설정
                        outputCounter = (outputCounter + 1) % 10;
                        File outputFile = new File(getExternalFilesDir(null), "android_output" + outputCounter + ".wav");

                        // 파일 저장
                        Log.d("SERVICE API", "Saving KO TTS WAV: " + outputFile.getAbsolutePath());
                        if (saveResponseToFile(response.body(), outputFile)) {
                            Log.d("SERVICE API", "KO TTS WAV saved successfully");

                            // Unity 스타일 음성 재생 관리
                            manageAudioPlayback(outputFile);
                        }
                    } catch (Exception e) {
                        Log.e("SERVICE API", "Error processing KO TTS response: " + e.getMessage());
                        e.printStackTrace();
                    }
                } else {
                    Log.e("SERVICE API", "KO TTS request failed, status: " + response.code());
                    try {
                        String errorBody = response.errorBody() != null ? response.errorBody().string() : "No error details";
                        Log.e("SERVICE API", "KO TTS error details: " + errorBody);
                    } catch (Exception e) {
                        Log.e("SERVICE API", "Error reading KO TTS error body: " + e.getMessage());
                    }
                }
            }

            @Override
            public void onFailure(Call<ResponseBody> call, Throwable t) {
                Log.e("SERVICE API", "KO TTS request failed: " + t.getMessage());
                t.printStackTrace();
            }
        });
    }

    private boolean saveResponseToFile(ResponseBody body, File file) {
        try (InputStream inputStream = body.byteStream();
             OutputStream outputStream = new FileOutputStream(file)) {

            byte[] buffer = new byte[4096];
            int bytesRead;
            while ((bytesRead = inputStream.read(buffer)) != -1) {
                outputStream.write(buffer, 0, bytesRead);
            }
            return true;
        } catch (IOException e) {
            Log.e("SERVICE API", "파일 저장 중 오류 발생: " + e.getMessage());
            return false;
        }
    }



    private void manageAudioPlayback(File audioFile) {
        if (mediaPlayer == null) {
            mediaPlayer = new MediaPlayer();
        }

        // 현재 재생 중이라면 큐에 추가
        if (mediaPlayer.isPlaying()) {
            audioQueue.offer(audioFile);
            return;
        }

        try {
            // 음량 값을 설정
            float volume = 1.0f; // 기본값 (100%)
            if (Bridge.sound_volume != null) {
                try {
                    int volumeInt = Integer.parseInt(Bridge.sound_volume);
                    volume = Math.max(0, Math.min(volumeInt / 100f, 1.0f)); // 0.0 ~ 1.0 사이로 클램핑
                } catch (NumberFormatException e) {
                    Log.e("Audio", "Invalid volume value, defaulting to 100%.");
                }
            }

            // 음성 재생 설정
            mediaPlayer.reset();
            mediaPlayer.setDataSource(audioFile.getAbsolutePath());
            mediaPlayer.prepare();
            mediaPlayer.setVolume(volume, volume);  // 음량 적용
            mediaPlayer.start();

            mediaPlayer.setOnCompletionListener(mp -> {
                if (!audioQueue.isEmpty()) {
                    manageAudioPlayback(audioQueue.poll()); // 큐에 다음 음성 재생
                }
            });
        } catch (IOException e) {
            Log.e("Audio", "재생 오류: " + e.getMessage());
        }
    }

    private void ResetAudio() {
        if (mediaPlayer != null) {
            if (mediaPlayer.isPlaying()) {
                mediaPlayer.stop(); // 현재 재생 중인 음성을 멈춤
            }
            mediaPlayer.reset(); // MediaPlayer 리소스 초기화
        }
        audioQueue.clear(); // 큐 초기화
        Log.d("Audio", "Reset Audio from STT");
    }

    // 필요할 경우 음성재생용 sampleRATE를 여기서 설정해서 audioTrack 선언시 사용해야 함
    private int getSampleRate(File file) {
        try (FileInputStream inputStream = new FileInputStream(file)) {
            byte[] header = new byte[44];
            inputStream.read(header, 0, 44);
            // 샘플 레이트는 24~27 바이트에 저장됨 (리틀 엔디안)
            return ((header[27] & 0xFF) << 24) | ((header[26] & 0xFF) << 16)
                    | ((header[25] & 0xFF) << 8) | (header[24] & 0xFF);
        } catch (Exception e) {
            Log.e("SERVICE", "Cannot find sample Rate");
            return 0;
        }
    }
}
