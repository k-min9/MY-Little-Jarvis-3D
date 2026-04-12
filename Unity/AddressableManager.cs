using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

// Addressables 로드/다운로드를 중앙에서 관리하는 싱글톤 매니저.
//   LoadIfExist      - 이미 다운로드된 에셋만 로드, 없으면 null 반환 (UI 아이콘 등)
//   LoadWithDownloadable - 없으면 DownloadManager를 통해 다운로드 후 로드 (캐릭터 적용 등)
public class AddressableManager : MonoBehaviour
{
    private static AddressableManager instance;
    public static AddressableManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<AddressableManager>();
            }
            return instance;
        }
    }
    // 세션 내 다운로드 여부(크기 0 확인됨) 캐시
    // true인 경우 로컬에 확실히 존재하므로 GetDownloadSizeAsync 호출을 생략
    private System.Collections.Generic.Dictionary<string, bool> downloadedCache = new System.Collections.Generic.Dictionary<string, bool>();

    // 다운로드 캐시 수동 초기화 (DLC 삭제 등에서 호출 시)
    public void ClearDownloadedCache()
    {
        downloadedCache.Clear();
    }

    // 해당 address 에셋의 잔여 다운로드 크기를 반환합니다.
    public async Task<long> GetPendingSize(string address)
    {
        if (string.IsNullOrEmpty(address)) return -1;

        // 이미 로컬에 있다고 캐싱된 경우 사이즈 체크 통과 (0 반환)
        if (downloadedCache.TryGetValue(address, out bool isDownloaded) && isDownloaded)
        {
            return 0;
        }

        try
        {
            var handle = Addressables.GetDownloadSizeAsync(address);
            await handle.Task;

            long size = (handle.Status == AsyncOperationStatus.Succeeded) ? handle.Result : -1L;
            Addressables.Release(handle);

            // 사이즈가 0(다운로드 완료 혹은 내장 에셋)이면 캐시에 등록하여 다음부터 체크 통과
            if (size == 0)
            {
                downloadedCache[address] = true;
            }

            return size;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AddressableManager] GetPendingSize 실패 ({address}): {e.Message}");
            return -1;
        }
    }

    // 있을 경우만 로드
    public async Task<T> LoadIfExist<T>(string address) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(address)) return null;

        long pending = await GetPendingSize(address);

        // 조회 실패 또는 미다운로드 → null 반환
        if (pending != 0)
        {
            Debug.Log($"[AddressableManager] LoadIfExist: 미다운로드 또는 조회 실패 → null 반환 ({address}, pendingBytes={pending})");
            return null;
        }

        // 이미 다운로드된 경우 로드
        try
        {
            var handle = Addressables.LoadAssetAsync<T>(address);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log($"[AddressableManager] LoadIfExist: 로드 성공 ({address})");
                return handle.Result;
            }
            else
            {
                Debug.LogWarning($"[AddressableManager] LoadIfExist: 로드 실패 ({address})");
                Addressables.Release(handle);
                return null;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AddressableManager] LoadIfExist: 예외 발생 ({address}): {e.Message}");
            return null;
        }
    }

    // Local default Group 전용 직접 로드
    // GetDownloadSizeAsync 체크 없이 LoadAssetAsync를 바로 시도합니다.
    // Remote 의존성 체인에 엮이지 않으므로 Remote 카탈로그 실패와 무관하게 동작합니다.
    public async Task<T> LoadLocal<T>(string address) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(address)) return null;

        try
        {
            var handle = Addressables.LoadAssetAsync<T>(address);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log($"[AddressableManager] LoadLocal: 로드 성공 ({address})");
                downloadedCache[address] = true;
                return handle.Result;
            }
            else
            {
                Debug.LogWarning($"[AddressableManager] LoadLocal: 로드 실패 ({address})");
                Addressables.Release(handle);
                return null;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AddressableManager] LoadLocal: 예외 발생 ({address}): {e.Message}");
            return null;
        }
    }

    // 없으면 다운로드, 있으면 로드
    public async void LoadWithDownloadable<T>(string address, Action<bool, T> onComplete) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(address))
        {
            onComplete?.Invoke(false, null);
            return;
        }

        long pending = await GetPendingSize(address);

        if (pending < 0)
        {
            // 조회 실패: 로드 시도는 해봄 (로컬 에셋일 수도 있음)
            Debug.LogWarning($"[AddressableManager] LoadWithDownloadable: 크기 조회 실패, 로드 강행 ({address})");
            await LoadAndCallback<T>(address, onComplete);
            return;
        }

        if (pending > 0)
        {
            // 미다운로드: DownloadManager를 통해 다운로드 요청
            Debug.Log($"[AddressableManager] LoadWithDownloadable: 다운로드 필요 ({address}, {pending} bytes)");

            DownloadManager.Instance.RequestAddressableDownload(address, pending, async (success) =>
            {
                if (success)
                {
                    await LoadAndCallback<T>(address, onComplete);
                }
                else
                {
                    Debug.LogWarning($"[AddressableManager] LoadWithDownloadable: 다운로드 취소/실패 ({address})");
                    onComplete?.Invoke(false, null);
                }
            });

            return;
        }

        // pending == 0: 이미 다운로드됨 → 바로 로드
        await LoadAndCallback<T>(address, onComplete);
    }

    // 없으면 다운로드, 있으면 로드 (async/await 컨텍스트용 Task 반환 오버로드)
    public Task<T> LoadWithDownloadableAsync<T>(string address) where T : UnityEngine.Object
    {
        var tcs = new TaskCompletionSource<T>();
        LoadWithDownloadable<T>(address, (success, asset) =>
        {
            tcs.TrySetResult(success ? asset : null);
        });
        return tcs.Task;
    }

    // 내부 공통 로드 헬퍼
    private async Task LoadAndCallback<T>(string address, Action<bool, T> onComplete) where T : UnityEngine.Object
    {
        try
        {
            var handle = Addressables.LoadAssetAsync<T>(address);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log($"[AddressableManager] 로드 성공 ({address})");
                onComplete?.Invoke(true, handle.Result);
            }
            else
            {
                Debug.LogError($"[AddressableManager] 로드 실패 ({address})");
                Addressables.Release(handle);
                onComplete?.Invoke(false, null);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[AddressableManager] 로드 예외 ({address}): {e.Message}");
            onComplete?.Invoke(false, null);
        }
    }
}
