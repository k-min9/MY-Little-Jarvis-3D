# My Little Jarvis Android

Unity와 Android Native가 연동된 실시간 음성 대화 시스템입니다. 백그라운드에서 지속적으로 음성을 감지하고 처리하여 자연스러운 대화가 가능합니다.

## 🚀 주요 기능

### 음성 처리
- **실시간 VAD (Voice Activity Detection)**: 음성 활동을 실시간으로 감지하여 자동으로 녹음 시작/종료
- **백그라운드 음성 처리**: 앱이 백그라운드 상태에서도 지속적으로 음성 감지 및 처리
- **고품질 음성 녹음**: 16kHz 샘플레이트의 WAV 형식으로 음성 저장

### AI 대화 시스템
- **STT (Speech-to-Text)**: 음성을 텍스트로 변환
- **대화 스트리밍**: 실시간 대화 응답 처리
- **TTS (Text-to-Speech)**: 다국어 음성 합성 (한국어, 일본어, 영어)
- **대화 메모리**: JSON 형식으로 대화 이력 저장 및 관리

### Unity 연동
- **Bridge 시스템**: Unity와 Android Native 간 원활한 데이터 교환
- **실시간 메시지 전송**: Unity에서 Android로, Android에서 Unity로 양방향 통신

## 📱 시스템 아키텍처

```
Unity App
    ↕ (Bridge.java)
Android Foreground Service
    ↕ (ApiService.java)
External API Server
```

### 핵심 컴포넌트

#### 1. **MyBackgroundService.java**
- Android Foreground Service로 백그라운드 실행 보장
- VAD 알고리즘을 통한 실시간 음성 감지
- 음성 녹음, WAV 파일 생성, API 전송 처리
- TTS 응답 수신 및 재생 관리

#### 2. **Bridge.java**
- Unity와 Android 간의 브릿지 역할
- Unity에서 전달받은 설정값 관리 (서버 URL, 캐릭터 설정 등)
- 서비스 시작/중지 제어
- 배터리 최적화 설정 유도

#### 3. **ApiService.java**
- Retrofit 기반 REST API 인터페이스
- STT 업로드 (`/stt`)
- 대화 스트리밍 (`/conversation_stream`)
- TTS 음성 합성 (`/getSound/jp`, `/getSound/ko`)

#### 4. **ConversationManager.java**
- 대화 이력 관리 (JSON 파일 기반)
- 메모리 크기 제한 및 최적화
- 기본 인사말 관리

## 🛠 기술 스택

- **Android**: Java, Foreground Service, AudioRecord/AudioTrack
- **Network**: Retrofit2, OkHttp3
- **Audio**: WAV 파일 처리, MediaPlayer
- **Data**: Gson (JSON 처리)
- **Unity Integration**: Unity Player 연동

## 📋 필요 권한

```xml
<uses-permission android:name="android.permission.FOREGROUND_SERVICE"/>
<uses-permission android:name="android.permission.FOREGROUND_SERVICE_MICROPHONE" />
<uses-permission android:name="android.permission.REQUEST_IGNORE_BATTERY_OPTIMIZATIONS"/>
<uses-permission android:name="android.permission.POST_NOTIFICATIONS" />
<uses-permission android:name="android.permission.RECORD_AUDIO"/>
<uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE" />
<uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" />
<uses-permission android:name="android.permission.INTERNET" />
```

## 🚀 설치 및 실행

### 1. AAR 파일 빌드
이 프로젝트는 Unity에서 사용할 수 있도록 AAR(Android Archive) 파일로 빌드됩니다.

```bash
# Android Studio에서 빌드
./gradlew assembleRelease

# 또는 AAR 파일만 생성
./gradlew bundleReleaseAar
```

생성된 AAR 파일 위치:
```
app/build/outputs/aar/app-release.aar
```

### 2. Unity 프로젝트 설정
```bash
# 1. Unity 프로젝트의 Assets/Plugins/Android/ 폴더에 AAR 파일 복사
cp app/build/outputs/aar/app-release.aar [Unity프로젝트]/Assets/Plugins/Android/

# 2. Unity에서 Android 빌드 설정
# - File > Build Settings > Android
# - Player Settings > Publishing Settings > Build 체크
# - Target API Level: 최소 API 26 이상 권장
```

#### Unity Player Settings 권한 설정
```xml
<!-- Unity Player Settings > Publishing Settings > Custom Main Manifest에 추가 -->
<uses-permission android:name="android.permission.FOREGROUND_SERVICE"/>
<uses-permission android:name="android.permission.FOREGROUND_SERVICE_MICROPHONE" />
<uses-permission android:name="android.permission.RECORD_AUDIO"/>
<uses-permission android:name="android.permission.POST_NOTIFICATIONS" />
<uses-permission android:name="android.permission.REQUEST_IGNORE_BATTERY_OPTIMIZATIONS"/>
<uses-permission android:name="android.permission.INTERNET" />
```

#### 추가 설정
- **Scripting Backend**: IL2CPP 권장
- **Target Architectures**: ARM64 (필수), ARMv7 (선택)
- **Write Permission**: External Storage (SDCard) 또는 Internal

### 3. Unity BackgroundService.cs 수정
Unity 프로젝트의 BackgroundService.cs에 다음 코드를 추가하여 백그라운드 이동 시 필요한 모든 설정값을 Android로 전송해야 합니다:

```csharp
// StartService() 메서드에 추가
String server_type_idx = SettingManager.Instance.settings.server_type_idx.ToString();
pluginClass.CallStatic("ReceiveServerTypeIdx", server_type_idx);
Debug.Log("InitializePlugin Send server_type_idx finish : " + server_type_idx);

// dev_voice URL 전송 (비동기 처리)
ServerManager.Instance.GetServerUrlFromServerId("dev_voice", (devVoiceUrl) =>
{
    if (!string.IsNullOrEmpty(devVoiceUrl))
    {
        pluginClass.CallStatic("ReceiveDevVoiceUrl", devVoiceUrl);
        Debug.Log("InitializePlugin Send dev_voice_url finish : " + devVoiceUrl);
    }
    else
    {
        pluginClass.CallStatic("ReceiveDevVoiceUrl", "");
        Debug.Log("InitializePlugin Send dev_voice_url finish : (empty)");
    }
    
    // dev_voice URL 설정 완료 후 서비스 시작
    pluginClass.CallStatic("StartService");
    Debug.Log("StartService");
});
```

### 4. Unity에서 Android 플러그인 사용 (수동 호출 예시)
```csharp
// Unity C# 스크립트에서 Android 메소드 호출 예시
#if UNITY_ANDROID && !UNITY_EDITOR
    AndroidJavaClass bridgeClass = new AndroidJavaClass("com.example.mylittlejarvisandroid.Bridge");
    
    // 액티비티 인스턴스 전달
    bridgeClass.CallStatic("ReceiveActivityInstance", 
        new AndroidJavaClass("com.unity3d.player.UnityPlayer")
        .GetStatic<AndroidJavaObject>("currentActivity"));
    
    // 설정값 전달
    bridgeClass.CallStatic("ReceiveBaseUrl", "https://your-api-server.com");
    bridgeClass.CallStatic("ReceiveNickname", "arona");
    bridgeClass.CallStatic("ReceivePlayerName", "sensei");
    bridgeClass.CallStatic("ReceiveSoundLanguage", "ko");
    bridgeClass.CallStatic("ReceiveSoundVolume", "80");
    bridgeClass.CallStatic("ReceiveSoundSpeed", "1.0");
    bridgeClass.CallStatic("ReceiveFilePath", Application.persistentDataPath);
    bridgeClass.CallStatic("ReceiveServerTypeIdx", "0");  // 0: Auto, 1: Server, 2: Free(Gemini)
    bridgeClass.CallStatic("ReceiveDevVoiceUrl", "https://dev-voice-server.com");  // dev_voice 서버 URL
    
    // 서비스 시작
    bridgeClass.CallStatic("StartService");
#endif
```

### 5. API 서버 설정
Unity에서 다음 설정값들을 Bridge.java로 전달:
- `baseUrl`: API 서버 주소
- `nickname`: 캐릭터 이름
- `player_name`: 플레이어 이름
- `sound_language`: 음성 언어 (ko/jp/en)
- `sound_volume`: 음성 볼륨 (0-100)
- `sound_speed`: 음성 속도
- `file_path`: 파일 저장 경로
- `server_type_idx`: 서버 타입 (0: Auto, 1: Server, 2: Free(Gemini), 3: Free(OpenRouter), 4: Paid(Gemini))
- `dev_voice_url`: dev_voice 서버 URL (server_type_idx=2일 때 TTS용)

### 6. Unity에서 Android 메시지 수신
Unity에서 Android로부터 메시지를 받는 메소드 구현:

```csharp
public class GameManager : MonoBehaviour
{
    // Android에서 호출되는 메소드
    public void SayHello(string message)
    {
        Debug.Log("Android에서 받은 메시지: " + message);
    }
    
    // STT 결과 수신 (Android에서 호출)
    public void OnSTTResult(string sttText)
    {
        Debug.Log("STT 결과: " + sttText);
        // UI 업데이트 등 처리
    }
    
    // 기타 Unity 콜백 메소드들...
}
```

### 7. 서비스 제어
```csharp
// 서비스 시작
bridgeClass.CallStatic("StartService");

// 배터리 최적화 설정 화면 열기
bridgeClass.CallStatic("OpenBatteryOptiSettings");

// 서비스 중지
bridgeClass.CallStatic("StopService");
```

## 🔧 VAD (Voice Activity Detection) 설정

### 주요 파라미터
- **SAMPLE_RATE**: 16000Hz (16kHz)
- **VAD_LAST_SEC**: 1.25초 (최근 데이터 분석 기간)
- **VAD_THRESHOLD**: 1.0 (음성 활성화 임계값)
- **VAD_FREQ_THRESHOLD**: 100Hz (고주파 필터)
- **GAP_LIMIT**: 2초 (음성 종료 판단 유예 시간)

### VAD 알고리즘
1. 실시간 오디오 스트림 분석
2. 고주파 필터 적용
3. 에너지 기반 음성 활동 감지
4. 유예 기간을 통한 안정적인 음성 구간 판단

## 📊 음성 처리 플로우

```
1. 실시간 음성 감지 (VAD)
2. 음성 활동 감지시 녹음 시작
3. 음성 종료 감지시 WAV 파일 생성
4. STT API로 음성 파일 전송 (/stt)
5. 텍스트 변환 결과 수신
6. 대화 API로 응답 생성 요청 (/conversation_stream 또는 /conversation_stream_gemini)
7. TTS API로 음성 합성 요청 (/getSound/jp 또는 /getSound/ko)
8. 음성 파일 수신 및 재생
9. 대화 이력 저장
```

### 서버 타입별 분기 처리 (server_type_idx)

- **0 (Auto)**: 기본 서버 설정 사용
- **1 (Server)**: 일반 서버 모드
- **2 (Free Gemini)**: 
  - 대화: `/conversation_stream_gemini` 사용
  - TTS: `dev_voice_url` 서버 사용 (설정된 경우)
- **3 (Free OpenRouter)**: OpenRouter API 사용
- **4 (Paid Gemini)**: 유료 Gemini API 사용

## 📱 배터리 최적화 설정

앱이 백그라운드에서 안정적으로 동작하려면 배터리 최적화 설정이 필요합니다:

```java
Bridge.OpenBatteryOptiSettings(); // 배터리 최적화 설정 화면 열기
```

### 지원 제조사
- Samsung, Huawei, Xiaomi, OPPO, Vivo, OnePlus 등
- 각 제조사별 자동 시작 관리 화면 지원

## 🔊 오디오 설정

### 녹음 설정
- **샘플레이트**: 16kHz
- **채널**: 모노
- **포맷**: 16-bit PCM
- **버퍼 크기**: 0.5초 (8000 샘플)

### 재생 설정
- **샘플레이트**: 32kHz (서버 응답 기준)
- **채널**: 모노
- **포맷**: 16-bit PCM
- **음성 큐 관리**: 순차 재생 지원

## 📁 파일 구조

```
MyLittleJarvisAndroid/
├── app/
│   ├── libs/
│   │   └── unity-classes.jar    # Unity 연동용 라이브러리
│   ├── src/main/
│   │   ├── java/com/example/mylittlejarvisandroid/
│   │   │   ├── ApiService.java              # REST API 인터페이스
│   │   │   ├── MyBackgroundService.java     # 백그라운드 서비스 (핵심)
│   │   │   ├── Bridge.java                  # Unity-Android 브릿지
│   │   │   ├── ConversationManager.java     # 대화 이력 관리
│   │   │   ├── Conversation.java            # 대화 데이터 모델
│   │   │   └── NotificationDeleteReceiver.java # 알림 삭제 리시버
│   │   ├── AndroidManifest.xml  # 권한 및 서비스 설정
│   │   └── res/drawable/
│   │       └── custom_icon.png  # 알림 아이콘
│   ├── build.gradle            # 앱 모듈 빌드 설정
│   └── build/outputs/aar/      # AAR 파일 출력 위치
├── build.gradle               # 프로젝트 빌드 설정
└── gradle.properties          # Gradle 설정
```

## 🔧 AAR 사용시 주의사항

### 권한 요청
Unity 앱에서 런타임에 권한을 요청해야 합니다:

```csharp
// Unity에서 권한 요청 예시
if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
{
    Permission.RequestUserPermission(Permission.Microphone);
}
```

### 의존성 관리
AAR 파일과 함께 필요한 라이브러리들:
- **Retrofit2**: API 통신용
- **Gson**: JSON 파싱용
- **OkHttp3**: HTTP 클라이언트

### 빌드 설정
Unity 프로젝트의 Gradle 설정에서 추가 의존성이 필요할 수 있습니다:

```gradle
// Unity 프로젝트의 mainTemplate.gradle에 추가
dependencies {
    implementation 'com.squareup.retrofit2:retrofit:2.9.0'
    implementation 'com.squareup.retrofit2:converter-gson:2.9.0'
    implementation 'com.squareup.okhttp3:okhttp:4.9.0'
}
```

## 🚀 전체 통합 과정 가이드

### 1단계: AAR 파일 빌드

#### Gradle 패널에서 빌드
1. **Gradle 패널 열기**: View → Tool Windows → Gradle
2. **빌드 실행**: MyLittleJarvisAndroid → app → Tasks → build → **build** (더블클릭)
3. **산출물 확인**: 빌드 완료 후 생성된 산출물 중 AAR 파일 사용

#### 생성된 파일 확인
```bash
# AAR 파일 위치
app/build/outputs/aar/app-release.aar

# 파일 크기 및 생성일 확인
ls -la app/build/outputs/aar/
```

### 2단계: Unity 프로젝트 통합

#### 2-1. AAR 파일 복사
```bash
# Unity 프로젝트의 Plugins 폴더에 복사
cp app/build/outputs/aar/app-release.aar [Unity프로젝트]/Assets/Plugins/Android/

# 폴더가 없다면 생성
mkdir -p [Unity프로젝트]/Assets/Plugins/Android/
```

#### 2-2. Unity에서 AAR 파일 설정
1. Unity Editor에서 `Assets/Plugins/Android/app-release.aar` 선택
2. Inspector에서 다음 설정 확인:
   - **Platform settings**: Android 체크
   - **Settings**: 
     - CPU: Any CPU
     - SDK: Android 7.0 'Nougat' (API level 24) 이상

#### 2-3. Unity Player Settings 구성
```xml
<!-- Publishing Settings > Custom Main Manifest 추가 -->
<uses-permission android:name="android.permission.FOREGROUND_SERVICE"/>
<uses-permission android:name="android.permission.FOREGROUND_SERVICE_MICROPHONE" />
<uses-permission android:name="android.permission.RECORD_AUDIO"/>
<uses-permission android:name="android.permission.POST_NOTIFICATIONS" />
<uses-permission android:name="android.permission.REQUEST_IGNORE_BATTERY_OPTIMIZATIONS"/>
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE" />
<uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" />
```

#### 2-4. Unity Build Settings
- **Scripting Backend**: IL2CPP
- **Target Architectures**: ARM64 (필수), ARMv7 (선택)
- **API Compatibility Level**: .NET Standard 2.1
- **Target API Level**: 최소 API 26 이상

### 3단계: 플랫폼별 설정

#### 3-1. PC-Windows vs Android 플랫폼 설정

**플랫폼 전환 방법:**
1. **File → Build Settings**
2. **플랫폼 선택**:
   - **PC-Windows**: PC, Mac & Linux Standalone 선택 → Switch Platform
   - **Android**: Android 선택 → Switch Platform

**플랫폼별 설정 차이:**

| 설정 항목 | PC-Windows | Android |
|-----------|------------|---------|
| **Target Platform** | PC, Mac & Linux Standalone | Android |
| **Canvas Scaler** | 2560 x 1440 (가로) | 1440 x 2560 (세로) |
| **Player Settings** | 기본 설정 | 권한 설정 필요 |
| **Build Target** | Executable | APK |

#### 3-2. Canvas Scaler 설정

**Canvas 오브젝트 선택 후 Canvas Scaler 컴포넌트에서:**

```
PC-Windows 설정:
- UI Scale Mode: Scale With Screen Size
- Reference Resolution: X=2560, Y=1440
- Match: Width Or Height = 0.5

Android 설정:
- UI Scale Mode: Scale With Screen Size  
- Reference Resolution: X=1440, Y=2560
- Match: Width Or Height = 0.5
```

**스크립트로 자동 설정 (권장):**
```csharp
public class PlatformUIScaler : MonoBehaviour
{
    private CanvasScaler canvasScaler;
    
    void Start()
    {
        canvasScaler = GetComponent<CanvasScaler>();
        SetPlatformResolution();
    }
    
    void SetPlatformResolution()
    {
        #if UNITY_STANDALONE_WIN
            // PC-Windows: 가로형
            canvasScaler.referenceResolution = new Vector2(2560, 1440);
        #elif UNITY_ANDROID
            // Android: 세로형
            canvasScaler.referenceResolution = new Vector2(1440, 2560);
        #endif
        
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.matchWidthOrHeight = 0.5f;
    }
}
```

#### 3-3. 플랫폼별 빌드 설정

**PC-Windows 빌드:**
```
Build Settings:
- Target Platform: Windows
- Architecture: x86_64
- Scripting Backend: Mono 또는 IL2CPP
```

**Android 빌드:**
```
Build Settings:
- Target Platform: Android
- Texture Compression: ASTC
- Scripting Backend: IL2CPP (권장)
- Target Architectures: ARM64 (필수), ARMv7 (선택)
- API Compatibility Level: .NET Standard 2.1
```

### 4단계: Unity 스크립트 통합

#### 4-1. BackgroundService.cs 추가/수정
프로젝트의 `referenceUnity/BackgroundService.cs`를 Unity 프로젝트로 복사하고 수정:

```csharp
public class BackgroundService : MonoBehaviour
{
    private AndroidJavaClass pluginClass;
    
    void Start()
    {
        InitializePlugin();
    }
    
    private void InitializePlugin()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        
        pluginClass = new AndroidJavaClass("com.example.mylittlejarvisandroid.Bridge");
        pluginClass.CallStatic("ReceiveActivityInstance", unityActivity);
        #endif
    }
    
    void OnApplicationPause(bool isPaused)
    {
        if (isPaused)
        {
            StartService();
        }
        else
        {
            StopService();
        }
    }
    
    public void StartService()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        // 모든 설정값 전송 (위에서 제공한 코드 사용)
        // ...
        #endif
    }
}
```

#### 4-2. GameManager.cs 수정
Android로부터 메시지를 받을 수 있도록 GameManager에 메서드 추가:

```csharp
public class GameManager : MonoBehaviour
{
    // Android에서 호출하는 메서드들
    public void SayHello(string message)
    {
        Debug.Log($"[Android → Unity] SayHello: {message}");
    }
    
    public void OnSTTResult(string sttText)
    {
        Debug.Log($"[Android → Unity] STT 결과: {sttText}");
        // UI 업데이트 등 처리
    }
}
```

### 5단계: 테스트 및 디버깅

#### 5-1. Unity 빌드 및 설치

**Android 빌드 (AAR 사용):**
1. **USB로 Android 폰 연결**
   - 개발자 옵션 활성화
   - USB 디버깅 허용
2. **Build Settings 열기**: **File → Build Settings**
3. **연결된 디바이스 확인**: Run Device에서 연결된 폰 선택
4. **빌드 및 설치**: **Build And Run** 버튼 클릭

**PC-Windows 빌드:**
1. **Build Settings 열기**: **File → Build Settings**
2. **플랫폼 확인**: PC, Mac & Linux Standalone 선택
3. **빌드**: **Build** 버튼 클릭하여 실행 파일(.exe) 생성

#### 5-2. 로그 모니터링

**방법 1: Android Studio GUI Logcat (권장)**
1. **Logcat 창 열기**: **Window → Analysis → Android Logcat**
2. **디바이스 선택**: 상단에서 연결된 Android 디바이스 선택
3. **필터 설정**:
   - **검색창에 입력**: `SERVICE|BRIDGE|Unity`
   - **Log Level 선택**: Debug, Info, Warn, Error 등
   - **Package 필터**: 앱 패키지명으로 필터링
4. **실시간 로그 확인**: 자동으로 실시간 업데이트

**방법 2: 터미널 Logcat 사용:**
```bash
# 전체 로그
adb logcat

# 특정 태그만 필터링
adb logcat -s "SERVICE" "BRIDGE" "Unity"

# 실시간 로그 (권장)
adb logcat | grep -E "(SERVICE|BRIDGE|Unity)"
```

**주요 로그 태그:**
- `SERVICE`: Android 백그라운드 서비스 로그
- `SERVICE API`: API 호출 관련 로그  
- `SERVICE STT`: STT 처리 로그
- `SERVICE VAD`: VAD 음성 감지 로그
- `BRIDGE`: Unity ↔ Android 통신 로그
- `Unity`: Unity 로그
