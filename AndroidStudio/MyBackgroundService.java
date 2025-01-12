package com.example.mylittlejarvisandroid;

import static com.example.mylittlejarvisandroid.Bridge.baseUrl;

import android.Manifest;
import android.app.Notification;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.app.Service;
import android.content.Context;
import android.content.Intent;
import android.content.pm.PackageManager;
import android.media.AudioAttributes;
import android.media.AudioFormat;
import android.media.AudioRecord;
import android.media.AudioTrack;
import android.media.MediaRecorder;
import android.os.Build;
import android.os.Handler;
import android.os.IBinder;
import android.util.Log;

import androidx.core.app.ActivityCompat;
import androidx.core.app.NotificationCompat;
import androidx.core.app.NotificationManagerCompat;

import com.google.gson.JsonObject;
import com.google.gson.JsonParser;

import java.io.ByteArrayOutputStream;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.IOException;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.util.ArrayList;
import java.util.List;

import okhttp3.MediaType;
import okhttp3.MultipartBody;
import okhttp3.RequestBody;
import retrofit2.Call;
import retrofit2.Callback;
import retrofit2.Response;
import retrofit2.Retrofit;
import retrofit2.converter.gson.GsonConverterFactory;

public class MyBackgroundService extends Service {

    // VAD Activation
    private boolean isVADModeActive = false;

    // WAV Data for Saving
    private byte[] wavData;

    // VAD Check
    private AudioRecord audioRecord; // Replaces AudioClip(Unity)
    private AudioTrack audioTrack;
    private final Object lock = new Object(); // 공유자산 정리



    private String microphoneDevice;
//    private boolean madeLoopLap;  // Android Studio에서는 필요없어보임
    private int clipSamples = 16000;  // _clip.samples * _clip.channels;
    private int lastMicPos;
    private boolean isVoiceDetected;

    // VAD Record
    private float[] oldBuffer = new float[0]; // Similar to Array.Empty<float>() in Unity
    private List<Float> newBuffer = new ArrayList<>(); // Equivalent to List<float> in Unity
    private float chunksLengthSec = 0.5f; // 0.5 seconds for chunk length
    private int lastChunkPos;
    private int chunksLength;
    private float vadStopTime = 2.0f; // 2 seconds of silence to stop VAD

    private Long vadStopBegin = null; // Nullable to track VAD stop state

    // Android용 변수
    private static final int SAMPLE_RATE = 16000; // 16kHz
    private static final int CHUNK_DURATION_MS = 500; // 0.5 seconds
    private static final int BUFFER_SIZE = AudioRecord.getMinBufferSize(
            SAMPLE_RATE,
            AudioFormat.CHANNEL_IN_MONO,
            AudioFormat.ENCODING_PCM_16BIT
    );

    private Handler handler;
    private Runnable vadRunnable;

    private int lastMicPosition = 0;
    private List<Short> buffer = new ArrayList<>();

    private int bufferSize;
    private short[] audioClipBuffer; // Audio data 저장
    private int clipChannels = 1; // 기본 1 채널 (Mono)
    private int frequency = 16000;

    public int getClipSamples() {
        return clipSamples * clipChannels;
    }

    @Override
    public void onCreate() {
        super.onCreate();
        Log.i("SERVICE", "Service created.");

//        UnitySendMessage("GameManager", "SayHello", "TESTonCreate!!!");
    }

    private void setupAudioRecord() {
        Log.i("SERVICE", "RECORD_AUDIO checking.");

        int audioSource = MediaRecorder.AudioSource.MIC;
        int channelConfig = AudioFormat.CHANNEL_IN_MONO;
        int audioFormat = AudioFormat.ENCODING_PCM_16BIT;
        bufferSize = AudioRecord.getMinBufferSize(frequency, channelConfig, audioFormat);

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

    private void setupVadRunnable() {
        vadRunnable = new Runnable() {
            @Override
            public void run() {
                if (isVADModeActive) {
                    checkVAD();
                    handler.postDelayed(this, CHUNK_DURATION_MS);
                }
            }
        };
    }

    public void startVAD() {
        Log.d("SERVICE", "startVAD start");

        setupAudioRecord();
        setupAudioTrack();

        if (audioRecord.getState() != AudioRecord.STATE_INITIALIZED) {
            Log.e("SERVICE", "AudioRecord initialization failed!");
            return;
        }

        isVADModeActive = true;
        audioRecord.startRecording();
        Log.d("SERVICE", "startVAD startRecording");

        // 변수 초기화
//        madeLoopLap = false;
        lastChunkPos = 0;
        vadStopBegin = null;
        chunksLength = (int) (frequency * chunksLengthSec);  // 16000 * 0.5s

        Log.d("SERVICE", "VAD started with frequency: " + frequency);

        if (handler != null && vadRunnable != null) {
            Log.d("SERVICE", "VAD CALLED BY startVAD");
            handler.post(vadRunnable);
        }

    }


    private void checkVAD() {
        Log.d("SERVICE", "CheckVAD start : BUFFER_SIZE" + BUFFER_SIZE);

        short[] audioBuffer = new short[BUFFER_SIZE];
        int readSamples = audioRecord.read(audioBuffer, 0, BUFFER_SIZE);
        int micPosition = (lastMicPosition + readSamples);  // % getClipSamples();

        Log.d("SERVICE", "CheckVAD micPosition : " + micPosition + " / readSamples : " + readSamples);

//        if (micPosition < lastMicPosition) {
//            madeLoopLap = true;
//        }
        lastMicPosition = micPosition;

        updateChunks(micPosition);
        updateVAD(micPosition);

        Log.d("SERVICE", "CheckVAD end");
    }

    private void updateChunks(int micPos) {
        Log.d("SERVICE", "updateChunks start : " + chunksLength);
        // Check if chunks length is valid
        if (chunksLength <= 0) {
            return;
        }

        // Get current chunk length
        int chunk = getMicPosDist(lastChunkPos, micPos);
        Log.d("SERVICE", "updateChunks lastChunkPos : " + lastChunkPos + " / chunk : " + chunk);

        // Process new chunks while there is valid size
        while (chunk > chunksLength) {
            short[] allBuffer = new short[micPos];
            short[] audioBuffer = new short[chunksLength];
            int readSamples = audioRecord.read(allBuffer, 0, micPos);

            if (readSamples > 0) {
                // 읽은 데이터의 특정 위치만 outputBuffer로 복사
                System.arraycopy(allBuffer, lastChunkPos, audioBuffer, 0, chunksLength);
            }
            Log.d("SERVICE", "updateChunks audioBuffer : " + audioBuffer.length);

            if (chunksLength > 0) {
                // Convert short buffer to float for processing
                float[] floatBuffer = new float[chunksLength];
                for (int i = 0; i < chunksLength; i++) {
                    floatBuffer[i] = audioBuffer[i] / 32768.0f; // Normalize to -1.0 to 1.0
                }

                if (isVoiceDetected || true) {
                    // Add to new buffer for further processing
                    newBuffer.addAll(convertArrayToList(floatBuffer));
                    getBuffer();

                    Log.d("SERVICE", "newBuffer save Start");
                    // newBuffer의 내용을 WAV 파일로 저장
                    String filePath = getApplicationContext().getExternalFilesDir(null) + "/audio_" + System.currentTimeMillis() + ".wav";
                    Log.d("SERVICE", "newBuffer save filePath");
                    saveBufferToWavFile(newBuffer, filePath, 16000); // 16000은 샘플레이트

                    // 그냥 음성재생체크용
                    playWav("response.wav");
                    sendWav("response.wav");
                }

                // Update last chunk position and recalculate chunk size
                lastChunkPos = (lastChunkPos + chunksLength);  // % getClipSamples();
                chunk = getMicPosDist(lastChunkPos, micPos);
            } else {
                Log.w("SERVICE", "AudioRecord read failed, no data captured.");
                break;
            }
        }
    }

    private int getMicPosDist(int start, int end) {
        if (end >= start) {
            return end - start;
        }
        return clipSamples - start + end; // Handle looping
    }

    private void updateVAD(int micPos) {
        // VAD thresholds
        float vadLastSec = 1.25f;  // VAD energy activation threshold
        float vadThd = 1.0f;       // VAD frequency threshold
        float vadFreqThd = 100.0f; // Optional VAD filter frequency threshold

        float vadContextSec = 30f; // Window size for VAD detection

        // Get microphone buffer data for the specified context window
        float[] data = getMicBufferLast(micPos, vadContextSec);

        // Perform VAD analysis
        boolean vad = simpleVad(data, 16000, vadLastSec, vadThd, vadFreqThd);

        Log.d("SERVICE", "updateVAD vad : " + vad);

        // Check if the VAD detection state has changed
        if (vad != isVoiceDetected) {
            Log.d("SERVICE", "VAD changed: " + vad);
            isVoiceDetected = vad;
            vadStopBegin = vad ? null : System.currentTimeMillis(); // Record start time if no voice detected
            Log.d("SERVICE", "updateVAD vadStopBegin : " + vadStopBegin);
        }

        // Check for VAD stop conditions
//        updateVadStop();
    }

    public static boolean simpleVad(float[] data, int sampleRate, float lastSec, float vadThd, float freqThd) {
        int num = data.length;
        int num2 = (int) (sampleRate * lastSec);
        if (num2 >= num) {
            return false;
        }

        if (freqThd > 0f) {
            highPassFilter(data, freqThd, sampleRate);
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



    private float[] getMicBufferLast(int micPos, float lastSec) {
        // Get the total number of samples currently available in the buffer
        int len = getMicBufferLength(micPos);
        if (len == 0) {
            return new float[0]; // Return an empty array if no samples are available
        }

        // Calculate the number of samples to retrieve based on the lastSec parameter
        int lastSamples = (int) (16000 * lastSec);
        int dataLength = Math.min(lastSamples, len);

        // Calculate the starting position of the buffer
        int offset = micPos - dataLength;
        if (offset < 0) {
            offset += len; // Handle circular buffer wrapping
        }

        // Retrieve the data from the audio buffer
        float[] data = new float[dataLength];
        getData(data, offset); // Assuming audioClip.getData retrieves audio samples
        return data;
    }


    // 원본 : audioClip.getData(data, offset);
    /**
     * 오디오 데이터를 지정된 배열에 채워 넣습니다.
     * @param data 데이터를 저장할 배열
     * @param offsetSamples 가져올 데이터의 오프셋 (샘플 위치)
     */
    public void getData(float[] data, int offsetSamples) {
        int lengthToCopy = Math.min(data.length, oldBuffer.length - offsetSamples);

        // 오디오 데이터를 byte[]에서 float[]로 변환
        for (int i = 0; i < lengthToCopy; i++) {
            data[i] = oldBuffer[offsetSamples + i];
        }
    }

    /**
     * byte 데이터를 float로 변환하는 함수
     * @param byteValue 변환할 byte 값
     * @return 변환된 float 값
     */
    private float byteToFloat(byte byteValue) {
        return ((float) byteValue) / Byte.MAX_VALUE;
    }

    // Unity와는 다른 구현
    private int getMicBufferLength(int micPos) {
        return micPos;
        
//        // Check if we just started recording and stopped it immediately, with no actual recording
//        if (micPos == 0 && !madeLoopLap) {
//            return 0; // No data recorded yet
//        }
//
//        // Calculate the length of the microphone buffer based on the circular buffer status
//        int len = madeLoopLap ? clipSamples : micPos; // If loop has occurred, use the full clip size
//        return len;
    }


    private List<Float> convertArrayToList(float[] array) {
        List<Float> list = new ArrayList<>();
        for (float value : array) {
            list.add(value);
        }
        return list;
    }

    private void getBuffer() {
        int newBufferLen = newBuffer.size();
        int oldBufferLen = oldBuffer.length;
        int nSamplesTake = oldBufferLen; // Take all old samples for now

        // Combine old and new buffer lengths
        int bufferLen = nSamplesTake + newBufferLen;
        float[] buffer = new float[bufferLen];

        // Copy data from old buffer to the new buffer
        System.arraycopy(oldBuffer, oldBufferLen - nSamplesTake, buffer, 0, nSamplesTake);

        // Add new data from newBuffer
        for (int i = 0; i < newBufferLen; i++) {
            buffer[nSamplesTake + i] = newBuffer.get(i);
        }

        // Clear the new buffer
        newBuffer.clear();

        // Save the combined buffer for further use
        oldBuffer = buffer;

        // Optionally save buffer to file for testing
        Log.d("SERVICE", "Saving buffer to file for testing...");
        saveWavFile(buffer, "buffer.wav");
    }

    private void saveWavFile(float[] buffer, String fileName) {
        Log.d("SERVICE", "saveWavFile START");
        try {
            Context appContext = getApplicationContext(); // Application Context 가져오기
            File file = new File(appContext.getFilesDir(), fileName);
            FileOutputStream fos = new FileOutputStream(file);

            // Convert float array to byte array
            byte[] wavData = convertFloatArrayToWav(buffer);
            fos.write(wavData);
            fos.close();

            Log.d("SERVICE", "Buffer saved as: " + file.getAbsolutePath());
        } catch (IOException e) {
            Log.e("SERVICE", "Error saving buffer to file: " + e.getMessage(), e);
        }
    }

    private byte[] convertFloatArrayToWav(float[] buffer) {
        ByteArrayOutputStream baos = new ByteArrayOutputStream();
        for (float sample : buffer) {
            int intSample = (int) (sample * 32767); // Convert to 16-bit PCM
            baos.write(intSample & 0xFF);
            baos.write((intSample >> 8) & 0xFF);
        }
        return baos.toByteArray();
    }

    @Override
    public int onStartCommand(Intent intent, int flags, int startId) {
        Log.i("SERVICE", "onStartCommand notification start");
        createNotificationChannel();
        startNotification();
        Log.i("SERVICE", "onStartCommand notification end");
        super.onCreate();

        Log.i("SERVICE", "startRecording thread call");
        new Thread(this::startVAD).start(); // Start the VAD process on a new thread
        Log.i("SERVICE", "startRecording thread start");

        handler = new Handler();
        setupVadRunnable();
//        handler.post(vadRunnable); // Runnable 시작

        UnitySendMessage("GameManager", "SayHello", "TESTonStartCommand!!!");

        return START_NOT_STICKY;
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
            isVADModeActive = false;
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
                    "Audio Background SERVICE CHANNEL",
                    "Service Channel",
                    NotificationManager.IMPORTANCE_DEFAULT
            );
            final NotificationManagerCompat notificationManager = NotificationManagerCompat.from(this);
            notificationManager.createNotificationChannel(notificationChannel);
        }
    }

    private void startNotification(){
        Intent notificationIntent = new Intent(this, Bridge.unityActivity.getClass());
        PendingIntent pendingIntent = PendingIntent.getActivity(this,
                0, notificationIntent, PendingIntent.FLAG_IMMUTABLE);
        Notification notification = new NotificationCompat.Builder(this, "PedometerLib")
                .setContentTitle("Service Running")
                .setContentText("ARONA is listening...")
                .setSmallIcon(R.drawable.custom_icon)
                .setContentIntent(pendingIntent)
                .setOngoing(true)
                .build();
        startForeground(112, notification);  // SERVICE_NOTIFICATION_ID : 112
    }





    private void saveBufferToWavFile(List<Float> buffer, String filePath, int sampleRate) {
        try {
            // 1. Float 데이터를 Short 데이터로 변환
            short[] shortData = new short[buffer.size()];
            for (int i = 0; i < buffer.size(); i++) {
                shortData[i] = (short) (buffer.get(i) * 32767); // Convert float [-1.0, 1.0] to short [-32768, 32767]
            }

            // 2. WAV 파일 헤더 생성
            byte[] wavHeader = createWavHeader(shortData.length * 2, sampleRate, 1);

            // 3. WAV 파일 쓰기
            FileOutputStream outputStream = new FileOutputStream(filePath);
            outputStream.write(wavHeader); // WAV 헤더 쓰기
            ByteBuffer byteBuffer = ByteBuffer.allocate(shortData.length * 2);
            byteBuffer.order(ByteOrder.LITTLE_ENDIAN);
            for (short s : shortData) {
                byteBuffer.putShort(s);
            }
            outputStream.write(byteBuffer.array()); // 오디오 데이터 쓰기
            outputStream.close();

            Log.d("SERVICE", "WAV file saved: " + filePath);
        } catch (IOException e) {
            Log.e("SERVICE", "Failed to save WAV file", e);
        }
    }

    // WAV 헤더 생성 함수
    private byte[] createWavHeader(int dataSize, int sampleRate, int channels) {
        int totalDataLen = dataSize + 36;
        int byteRate = sampleRate * channels * 2; // 16-bit PCM

        byte[] header = new byte[44];
        header[0] = 'R'; // "RIFF"
        header[1] = 'I';
        header[2] = 'F';
        header[3] = 'F';
        header[4] = (byte) (totalDataLen & 0xff);
        header[5] = (byte) ((totalDataLen >> 8) & 0xff);
        header[6] = (byte) ((totalDataLen >> 16) & 0xff);
        header[7] = (byte) ((totalDataLen >> 24) & 0xff);
        header[8] = 'W'; // "WAVE"
        header[9] = 'A';
        header[10] = 'V';
        header[11] = 'E';
        header[12] = 'f'; // "fmt "
        header[13] = 'm';
        header[14] = 't';
        header[15] = ' ';
        header[16] = 16; // Sub-chunk size (PCM)
        header[17] = 0;
        header[18] = 0;
        header[19] = 0;
        header[20] = 1; // Audio format (1 = PCM)
        header[21] = 0;
        header[22] = (byte) channels; // Number of channels
        header[23] = 0;
        header[24] = (byte) (sampleRate & 0xff);
        header[25] = (byte) ((sampleRate >> 8) & 0xff);
        header[26] = (byte) ((sampleRate >> 16) & 0xff);
        header[27] = (byte) ((sampleRate >> 24) & 0xff);
        header[28] = (byte) (byteRate & 0xff);
        header[29] = (byte) ((byteRate >> 8) & 0xff);
        header[30] = (byte) ((byteRate >> 16) & 0xff);
        header[31] = (byte) ((byteRate >> 24) & 0xff);
        header[32] = (byte) (channels * 2); // Block align
        header[33] = 0;
        header[34] = 16; // Bits per sample
        header[35] = 0;
        header[36] = 'd'; // "data"
        header[37] = 'a';
        header[38] = 't';
        header[39] = 'a';
        header[40] = (byte) (dataSize & 0xff);
        header[41] = (byte) ((dataSize >> 8) & 0xff);
        header[42] = (byte) ((dataSize >> 16) & 0xff);
        header[43] = (byte) ((dataSize >> 24) & 0xff);
        return header;
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

    public void sendWav(String fileName) {
        String serverUrl =  baseUrl + "/stt";
        File file = new File(getExternalFilesDir(null), fileName);

        if (!file.exists()) {
            System.err.println("File not found: " + file.getAbsolutePath());
            return;
        }

        Retrofit retrofit = new Retrofit.Builder()
                .baseUrl(baseUrl)
                .addConverterFactory(GsonConverterFactory.create()) // JSON 파싱 변환기
                .build();
        ApiService apiService = retrofit.create(ApiService.class);

        // 파일 및 텍스트 파라미터 생성
        RequestBody requestFile = RequestBody.create(MediaType.parse("audio/wav"), file);
        MultipartBody.Part filePart = MultipartBody.Part.createFormData("file", file.getName(), requestFile);

        RequestBody lang = RequestBody.create(MediaType.parse("text/plain"), "ko");
        RequestBody level = RequestBody.create(MediaType.parse("text/plain"), "small");
        RequestBody chatIdx = RequestBody.create(MediaType.parse("text/plain"), "1");

        // API 호출
        Call<JsonObject> call = apiService.uploadAudio(filePart, lang, level, chatIdx);
        call.enqueue(new Callback<JsonObject>() {
            @Override
            public void onResponse(Call<JsonObject> call, Response<JsonObject> response) {
                if (response.isSuccessful()) {
                    JsonObject jsonResponse = response.body();
                    if (jsonResponse != null) {
                        String transText = jsonResponse.get("text").getAsString();
                        String transLang = jsonResponse.get("lang").getAsString();
                        int chatIdx = jsonResponse.get("chatIdx").getAsInt();

                        System.out.println("Transcribed Text: " + transText);
                        System.out.println("Language: " + transLang);
                        System.out.println("Chat Index: " + chatIdx);
                    }
                } else {
                    System.err.println("Request failed. Response Code: " + response.code());
                }
            }

            @Override
            public void onFailure(Call<JsonObject> call, Throwable t) {
                t.printStackTrace();
            }
        });
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
