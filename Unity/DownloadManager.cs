using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Threading;

public class DownloadManager : MonoBehaviour
{
    public static DownloadManager instance;
    public static DownloadManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<DownloadManager>();
            }
            return instance;
        }
    }

    private void Start()
    {
        modelDownloadGameObject.SetActive(false);
    }

    [Header("Download UI")]
    [SerializeField] private GameObject modelDownloadGameObject;
    [SerializeField] private Image modelDownloadImage;
    [SerializeField] private Text modelDownloadText;

    [Header("Download Settings")]
    [SerializeField] private int chunkSizeMB = 2; // 청크 크기 (MB)
    [SerializeField] private int maxConcurrentDownloads = 4; // 동시 다운로드 수
    [SerializeField] private int timeoutSeconds = 60; // 타임아웃
    [SerializeField] private int maxRetries = 3; // 최대 재시도 횟수

    private bool isDownloading = false;
    private System.Diagnostics.Stopwatch downloadStopwatch;
    private long lastBytesDownloaded = 0;
    private float lastSpeedCheckTime = 0f;

    [System.Serializable]
    public class ChunkInfo
    {
        public long Start;
        public long End;
        public int Index;
        public bool IsCompleted;
        public int RetryCount;
    }

    // 다국어 텍스트 가져오기
    private string GetLocalizedText(string key)
    {
        string uiLanguage = SettingManager.Instance.settings.ui_language; // ko, jp, en
        
        switch (key)
        {
            case "download_in_progress":
                return uiLanguage == "ko" ? "다운로드 진행 중" :
                       uiLanguage == "jp" ? "ダウンロード進行中" : "Download in Progress";
                       
            case "download_in_progress_msg":
                return uiLanguage == "ko" ? "다른 모델을 다운로드 중입니다. 완료 후 다시 시도해주세요." :
                       uiLanguage == "jp" ? "他のモデルをダウンロード中です。完了後に再試行してください。" : "Another model is being downloaded. Please try again after completion.";
                       
            case "confirm":
                return uiLanguage == "ko" ? "확인" :
                       uiLanguage == "jp" ? "確認" : "OK";
                       
            case "model_download":
                return uiLanguage == "ko" ? "모델 다운로드" :
                       uiLanguage == "jp" ? "モデルダウンロード" : "Model Download";
                       
            case "download_confirm_msg":
                return uiLanguage == "ko" ? "모델을 다운로드 하시겠습니까?" :
                       uiLanguage == "jp" ? "モデルをダウンロードしますか？" : "Do you want to download the model?";
                       
            case "filename":
                return uiLanguage == "ko" ? "파일명" :
                       uiLanguage == "jp" ? "ファイル名" : "Filename";
                       
            case "required_space":
                return uiLanguage == "ko" ? "필요 용량" :
                       uiLanguage == "jp" ? "必要容量" : "Required Space";
                       
            case "download":
                return uiLanguage == "ko" ? "다운로드" :
                       uiLanguage == "jp" ? "ダウンロード" : "Download";
                       
            case "cancel":
                return uiLanguage == "ko" ? "취소" :
                       uiLanguage == "jp" ? "キャンセル" : "Cancel";
                       
            case "file_size_error":
                return uiLanguage == "ko" ? "파일 크기를 확인할 수 없습니다." :
                       uiLanguage == "jp" ? "ファイルサイズを確認できません。" : "Cannot determine file size.";
                       
            case "download_incomplete":
                return uiLanguage == "ko" ? "다운로드가 완전하지 않습니다." :
                       uiLanguage == "jp" ? "ダウンロードが完了していません。" : "Download is incomplete.";
                       
            case "download_complete":
                return uiLanguage == "ko" ? "다운로드 완료" :
                       uiLanguage == "jp" ? "ダウンロード完了" : "Download Complete";
                       
            case "download_complete_msg":
                return uiLanguage == "ko" ? "모델이 다운로드 되었습니다." :
                       uiLanguage == "jp" ? "モデルがダウンロードされました。" : "Model has been downloaded.";
                       
            case "elapsed_time":
                return uiLanguage == "ko" ? "소요시간" :
                       uiLanguage == "jp" ? "所要時間" : "Elapsed Time";
                       
            case "average_speed":
                return uiLanguage == "ko" ? "평균속도" :
                       uiLanguage == "jp" ? "平均速度" : "Average Speed";
                       
            case "seconds":
                return uiLanguage == "ko" ? "초" :
                       uiLanguage == "jp" ? "秒" : "sec";
                       
            case "download_failed":
                return uiLanguage == "ko" ? "다운로드 실패" :
                       uiLanguage == "jp" ? "ダウンロード失敗" : "Download Failed";
                       
            case "speed":
                return uiLanguage == "ko" ? "속도" :
                       uiLanguage == "jp" ? "速度" : "Speed";
                       
            case "time":
                return uiLanguage == "ko" ? "시간" :
                       uiLanguage == "jp" ? "時間" : "Time";
                       
            case "calculating":
                return uiLanguage == "ko" ? "계산 중..." :
                       uiLanguage == "jp" ? "計算中..." : "Calculating...";
                       
            default:
                return key;
        }
    }

    // 모델 다운로드 요청
    public void RequestDownload()
    {
        if (isDownloading)
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.DisplayDialog(
                GetLocalizedText("download_in_progress"), 
                GetLocalizedText("download_in_progress_msg"), 
                GetLocalizedText("confirm"));
#endif
            return;
        }

        string modelType = SettingManager.Instance.settings.model_type;
        var model = ServerModelData.ModelOptions.Find(m => m.Id == modelType);
        if (model == null)
        {
            Debug.LogWarning($"[DownloadManager] Unknown model ID: {modelType}");
            return;
        }

        string savePath = Path.Combine(Application.streamingAssetsPath, "model", model.FileName);

#if UNITY_EDITOR
        bool proceed = UnityEditor.EditorUtility.DisplayDialog(
            GetLocalizedText("model_download"),
            $"'{model.DisplayName}' {GetLocalizedText("download_confirm_msg")}\n" +
            $"{GetLocalizedText("filename")}: {model.FileName}\n" +
            $"{GetLocalizedText("required_space")}: {model.FileSizeText}",
            GetLocalizedText("download"), 
            GetLocalizedText("cancel"));

        if (!proceed) return;
#endif

        StartCoroutine(ParallelDownloadCoroutine(model.DownloadUrl, savePath, model.DisplayName, model.FileSizeText));
    }

    // 병렬 다운로드 메인 코루틴
    private IEnumerator ParallelDownloadCoroutine(string url, string savePath, string displayName, string totalSizeText)
    {
        isDownloading = true;
        modelDownloadGameObject.SetActive(true);
        modelDownloadImage.fillAmount = 0f;

        // 다운로드 속도 측정 초기화
        downloadStopwatch = System.Diagnostics.Stopwatch.StartNew();
        lastBytesDownloaded = 0;
        lastSpeedCheckTime = Time.time;

        Debug.Log($"[DownloadManager] Starting parallel download: {url}");

        long totalSize = 0;

        // 1. 파일 크기 확인
        yield return StartCoroutine(GetFileSizeCoroutine(url, (size) => totalSize = size));
        if (totalSize == 0)
        {
            Debug.LogError("[DownloadManager] Could not determine file size");
            ShowErrorDialog(GetLocalizedText("file_size_error"));
            yield break;
        }

        // 2. 청크 정보 생성
        int chunkSize = chunkSizeMB * 1024 * 1024;
        var chunks = CreateChunks(totalSize, chunkSize);
        byte[][] chunkData = new byte[chunks.Count][];

        // 3. 병렬 다운로드 시작
        var activeDownloads = new List<Coroutine>();
        var downloadQueue = new Queue<ChunkInfo>(chunks);

        // 초기 청크 다운로드 시작
        for (int i = 0; i < Mathf.Min(maxConcurrentDownloads, chunks.Count); i++)
        {
            if (downloadQueue.Count > 0)
            {
                var chunk = downloadQueue.Dequeue();
                activeDownloads.Add(StartCoroutine(DownloadChunkCoroutine(url, chunk, chunkData, downloadQueue, activeDownloads)));
            }
        }

        // 4. 다운로드 완료 대기 및 UI 업데이트 반복
        while (true)
        {
            // 완료된 다운로드 제거
            activeDownloads.RemoveAll(coroutine => coroutine == null);

            // 현재 다운로드된 바이트 수 계산 및 UI 업데이트
            long downloadedBytes = CalculateDownloadedBytes(chunkData);
            UpdateUI(downloadedBytes, totalSize, totalSizeText);

            // 모든 청크가 완료되었는지 확인
            bool allChunksCompleted = true;
            foreach (var chunk in chunks)
            {
                if (!chunk.IsCompleted)
                {
                    allChunksCompleted = false;
                    break;
                }
            }

            if (allChunksCompleted)
                break;

            yield return new WaitForSeconds(0.1f);
        }

        // 5. 최종 검증
        long finalDownloadedBytes = CalculateDownloadedBytes(chunkData);
        if (finalDownloadedBytes < totalSize)
        {
            Debug.LogError($"[DownloadManager] Download incomplete: {finalDownloadedBytes}/{totalSize} bytes");
            ShowErrorDialog(GetLocalizedText("download_incomplete"));
            yield break;
        }

        // 6. 파일 병합
        yield return StartCoroutine(MergeChunksToFile(chunkData, savePath));

        // 7. 완료 처리 및 통계 출력
        downloadStopwatch.Stop();
        float totalTimeSeconds = downloadStopwatch.ElapsedMilliseconds / 1000f;
        float avgSpeedMBps = (totalSize / (1024f * 1024f)) / totalTimeSeconds;

        Debug.Log($"[DownloadManager] Download completed in {totalTimeSeconds:F1}s at {avgSpeedMBps:F1} MB/s");

    #if UNITY_EDITOR
        UnityEditor.EditorUtility.DisplayDialog(
            GetLocalizedText("download_complete"),
            $"'{displayName}' {GetLocalizedText("download_complete_msg")}\n" +
            $"{GetLocalizedText("elapsed_time")}: {totalTimeSeconds:F1}{GetLocalizedText("seconds")}\n" +
            $"{GetLocalizedText("average_speed")}: {avgSpeedMBps:F1} MB/s",
            GetLocalizedText("confirm"));
    #endif

        modelDownloadGameObject.SetActive(false);
        isDownloading = false;
    }

    // 파일 크기 확인
    private IEnumerator GetFileSizeCoroutine(string url, System.Action<long> onComplete)
    {
        using (UnityWebRequest headRequest = UnityWebRequest.Head(url))
        {
            headRequest.timeout = timeoutSeconds;
            yield return headRequest.SendWebRequest();
            
            long size = 0;
            if (headRequest.result == UnityWebRequest.Result.Success)
            {
                string contentLength = headRequest.GetResponseHeader("Content-Length");
                if (!string.IsNullOrEmpty(contentLength))
                {
                    long.TryParse(contentLength, out size);
                }
            }
            else
            {
                Debug.LogError($"[DownloadManager] Failed to get file size: {headRequest.error}");
            }
            
            onComplete?.Invoke(size);
        }
    }

    // 청크 정보 생성
    private List<ChunkInfo> CreateChunks(long totalSize, int chunkSize)
    {
        var chunks = new List<ChunkInfo>();
        for (long offset = 0; offset < totalSize; offset += chunkSize)
        {
            long end = Math.Min(offset + chunkSize - 1, totalSize - 1);
            chunks.Add(new ChunkInfo 
            { 
                Start = offset, 
                End = end, 
                Index = chunks.Count,
                IsCompleted = false,
                RetryCount = 0
            });
        }
        return chunks;
    }

    // 개별 청크 다운로드
    private IEnumerator DownloadChunkCoroutine(string url, ChunkInfo chunk, byte[][] chunkData, 
        Queue<ChunkInfo> downloadQueue, List<Coroutine> activeDownloads)
    {
        bool success = false;
        
        while (!success && chunk.RetryCount < maxRetries)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("Range", $"bytes={chunk.Start}-{chunk.End}");
                request.timeout = timeoutSeconds;
                request.SetRequestHeader("Connection", "keep-alive");
                
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    chunkData[chunk.Index] = request.downloadHandler.data;
                    chunk.IsCompleted = true;
                    success = true;
                    
                    Debug.Log($"[DownloadManager] Chunk {chunk.Index} completed ({chunk.Start}-{chunk.End})");
                }
                else
                {
                    chunk.RetryCount++;
                    Debug.LogWarning($"[DownloadManager] Chunk {chunk.Index} failed (attempt {chunk.RetryCount}): {request.error}");
                    
                    if (chunk.RetryCount < maxRetries)
                    {
                        yield return new WaitForSeconds(1f); // 재시도 전 대기
                    }
                }
            }
        }

        if (!success)
        {
            Debug.LogError($"[DownloadManager] Chunk {chunk.Index} failed after {maxRetries} attempts");
        }

        // 큐에 남은 청크가 있으면 새로운 다운로드 시작
        if (downloadQueue.Count > 0)
        {
            var nextChunk = downloadQueue.Dequeue();
            activeDownloads.Add(StartCoroutine(DownloadChunkCoroutine(url, nextChunk, chunkData, downloadQueue, activeDownloads)));
        }
    }

    // 다운로드된 바이트 계산
    private long CalculateDownloadedBytes(byte[][] chunkData)
    {
        long total = 0;
        for (int i = 0; i < chunkData.Length; i++)
        {
            if (chunkData[i] != null)
                total += chunkData[i].Length;
        }
        return total;
    }

    // UI 업데이트
    private void UpdateUI(long downloadedBytes, long totalSize, string totalSizeText)
    {
        float percent = (float)downloadedBytes / totalSize;
        float downloadedGB = downloadedBytes / (1024f * 1024f * 1024f);

        float elapsedSeconds = downloadStopwatch.ElapsedMilliseconds / 1000f;
        float speedMBps = elapsedSeconds > 0 ? (downloadedBytes / (1024f * 1024f)) / elapsedSeconds : 0f;
        float remainingBytes = totalSize - downloadedBytes;
        float estimatedSeconds = speedMBps > 0 ? (remainingBytes / (1024f * 1024f)) / speedMBps : -1f;

        string speedText = $"{speedMBps:F1} MB/s";

        // ETA 계산
        string etaText = GetLocalizedText("calculating");
        if (percent >= 0.01f && estimatedSeconds > 0)  // 진행 1% 이상 및 시간 있음.
        {
            TimeSpan ts = TimeSpan.FromSeconds(estimatedSeconds);
            if (estimatedSeconds >= 3600f)
            {
                etaText = $"{ts:hh\\:mm}m";  // 1시간 이상: 시:분
            }
            else
            {
                etaText = $"{ts:mm\\:ss}s";  // 1시간 미만: 분:초
            }
        }

        modelDownloadImage.fillAmount = percent;
        modelDownloadText.text = $"{downloadedGB:F2} GB / {totalSizeText} ({percent * 100f:F1}%)\n" +
                                $"{GetLocalizedText("speed")}: {speedText} | {GetLocalizedText("time")}: {etaText}";
    }


    // 청크들을 하나의 파일로 합치기 (수정된 버전)
    private IEnumerator MergeChunksToFile(byte[][] chunkData, string savePath)
    {
        Debug.Log($"[DownloadManager] Merging chunks to file: {savePath}");
        
        string directory = Path.GetDirectoryName(savePath);
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        bool mergeSuccess = false;
        string errorMessage = "";

        // 파일 쓰기 작업
        FileStream fileStream = null;
        
        // try-catch 블록을 최소화하고 yield를 밖으로 빼기
        for (int i = 0; i < chunkData.Length; i++)
        {
            // 첫 번째 청크일 때 파일 스트림 생성
            if (i == 0)
            {
                try
                {
                    fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[DownloadManager] Failed to create file stream: {e.Message}");
                    errorMessage = e.Message;
                    break;
                }
            }

            // 청크 데이터 쓰기
            if (chunkData[i] != null && fileStream != null)
            {
                try
                {
                    fileStream.Write(chunkData[i], 0, chunkData[i].Length);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[DownloadManager] Failed to write chunk {i}: {e.Message}");
                    errorMessage = e.Message;
                    break;
                }
            }
            else if (chunkData[i] == null)
            {
                Debug.LogError($"[DownloadManager] Missing chunk data at index {i}");
                errorMessage = $"Missing chunk data at index {i}";
                break;
            }

            // 큰 파일의 경우 프레임 양보 (try-catch 밖에서)
            if (i % 10 == 0)
            {
                yield return null;
            }

            // 마지막 청크 처리 후 성공 표시
            if (i == chunkData.Length - 1 && string.IsNullOrEmpty(errorMessage))
            {
                mergeSuccess = true;
            }
        }

        // 파일 스트림 정리
        if (fileStream != null)
        {
            try
            {
                fileStream.Close();
                fileStream.Dispose();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[DownloadManager] Failed to close file stream: {e.Message}");
                if (string.IsNullOrEmpty(errorMessage))
                    errorMessage = e.Message;
            }
        }

        if (mergeSuccess)
        {
            Debug.Log($"[DownloadManager] File merge completed: {savePath}");
        }
        else
        {
            Debug.LogError($"[DownloadManager] File merge failed: {errorMessage}");
        }
    }

    // 에러 다이얼로그 표시
    private void ShowErrorDialog(string message)
    {
#if UNITY_EDITOR
        UnityEditor.EditorUtility.DisplayDialog(GetLocalizedText("download_failed"), message, GetLocalizedText("confirm"));
#endif
        modelDownloadGameObject.SetActive(false);
        isDownloading = false;
    }

    // 다운로드 취소 (필요시 사용)
    public void CancelDownload()
    {
        if (isDownloading)
        {
            StopAllCoroutines();
            isDownloading = false;
            modelDownloadGameObject.SetActive(false);
            Debug.Log("[DownloadManager] Download cancelled by user");
        }
    }
}
