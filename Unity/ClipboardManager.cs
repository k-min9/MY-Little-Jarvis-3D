using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

public class ClipboardManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    private static ClipboardManager instance;
    public static ClipboardManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ClipboardManager>();
            }
            return instance;
        }
    }

    // Windows API 선언
    [DllImport("user32.dll")]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll")]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll")]
    private static extern bool IsClipboardFormatAvailable(uint format);

    [DllImport("user32.dll")]
    private static extern IntPtr GetClipboardData(uint uFormat);

    [DllImport("user32.dll")]
    private static extern uint EnumClipboardFormats(uint format);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll")]
    private static extern bool GlobalUnlock(IntPtr hMem);

    [DllImport("kernel32.dll")]
    private static extern int GlobalSize(IntPtr hMem);

    [DllImport("user32.dll")]
    private static extern uint GetClipboardSequenceNumber();

    private const uint CF_DIB = 8;
    private const uint CF_BITMAP = 2;
    private const uint CF_TEXT = 1;
    private const uint CF_UNICODETEXT = 13;

    // 클립보드 텍스트 저장 변수
    public string clipboardText = "";
    
    // 클립보드 변경 감지용
    private uint lastClipboardSequence = 0;
    private float clipboardCheckInterval = 0.5f; // 0.5초마다 체크
    private float clipboardCheckTimer = 0f;

    private void Update()
    {
        // 클립보드 변경 감지
        clipboardCheckTimer += Time.deltaTime;
        if (clipboardCheckTimer >= clipboardCheckInterval)
        {
            clipboardCheckTimer = 0f;
            CheckClipboardChange();
        }
    }

    // 클립보드 변경 체크
    private void CheckClipboardChange()
    {
        uint currentSequence = GetClipboardSequenceNumber();
        
        // 초기화
        if (lastClipboardSequence == 0)
        {
            lastClipboardSequence = currentSequence;
            return;
        }
        
        // 클립보드 변경 감지
        if (currentSequence != lastClipboardSequence)
        {
            lastClipboardSequence = currentSequence;
            OnClipboardChanged();
        }
    }

    // 클립보드 변경 시 호출되는 메서드
    private void OnClipboardChanged()
    {
        Debug.Log("[ClipboardManager] Clipboard changed detected!");
        
        // 이미지가 있는지 확인
        bool hasImage = HasImageInClipboard();
        
        if (hasImage)
        {
            Debug.Log("[ClipboardManager] Clipboard contains image");
            
            // ChatBalloonManager에 마지막 이미지 소스 업데이트
            if (ChatBalloonManager.Instance != null)
            {
                ChatBalloonManager.Instance.SetLastImageSource("clipboard");
            }
            
            // ChatBalloonManager에 알림 (채팅창이 열려있을 때만 UI 갱신)
            if (ChatBalloonManager.Instance != null && StatusManager.Instance.IsChatting)
            {
                Debug.Log("[ClipboardManager] Notified ChatBalloonManager (chat window is open)");
            }
        }
    }

    // 클립보드에 이미지가 있는지 확인하는 메서드 (저장하지 않고 확인만)
    public bool HasImageInClipboard()
    {
        try
        {
            if (!OpenClipboard(IntPtr.Zero))
            {
                return false;
            }
            
            try
            {
                bool hasImage = IsClipboardFormatAvailable(CF_BITMAP) || IsClipboardFormatAvailable(CF_DIB);
                return hasImage;
            }
            finally
            {
                CloseClipboard();
            }
        }
        catch
        {
            return false;
        }
    }

    // 클립보드 내용을 자동으로 판단하여 처리
    public void GetContentFromClipboard()
    {
        try
        {
            // 클립보드 열기
            if (!OpenClipboard(IntPtr.Zero))
            {
                Debug.LogWarning("Clipboard를 열 수 없습니다.");
                return;
            }

            try
            {
                // 이미지가 있는지 확인
                bool hasImage = IsClipboardFormatAvailable(CF_BITMAP) || IsClipboardFormatAvailable(CF_DIB);
                
                // 텍스트가 있는지 확인
                bool hasText = IsClipboardFormatAvailable(CF_UNICODETEXT) || IsClipboardFormatAvailable(CF_TEXT);

                if (hasImage)
                {
                    // 이미지 처리
                    CloseClipboard();
                    SaveImageFromClipboard();
                }
                else if (hasText)
                {
                    // 텍스트 처리
                    CloseClipboard();
                    GetTextFromClipboard();
                }
                else
                {
                    Debug.LogWarning("Clipboard에 이미지나 텍스트가 없습니다.");
                }
            }
            finally
            {
                // 이미지나 텍스트 처리 시 이미 CloseClipboard()가 호출되었을 수 있으므로
                // 다시 열려있는지 확인 후 닫기
                try
                {
                    CloseClipboard();
                }
                catch { }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Clipboard 내용 가져오기 실패: {ex.Message}");
        }
    }

    // 클립보드에서 이미지를 byte[]로 가져오기 (파일 저장 없이)
    public byte[] GetImageBytesFromClipboard()
    {
        try
        {
            // 클립보드 열기
            if (!OpenClipboard(IntPtr.Zero))
            {
                Debug.LogWarning("Clipboard를 열 수 없습니다.");
                return null;
            }

            try
            {
                // 클립보드에 이미지가 있는지 확인
                if (!IsClipboardFormatAvailable(CF_BITMAP) && !IsClipboardFormatAvailable(CF_DIB))
                {
                    Debug.LogWarning("Clipboard에 이미지가 없습니다.");
                    return null;
                }

                System.Drawing.Bitmap bitmap = null;

                // CF_BITMAP 형식으로 이미지 가져오기 (우선순위)
                if (IsClipboardFormatAvailable(CF_BITMAP))
                {
                    IntPtr hBitmap = GetClipboardData(CF_BITMAP);
                    if (hBitmap != IntPtr.Zero)
                    {
                        bitmap = System.Drawing.Bitmap.FromHbitmap(hBitmap);
                    }
                }
                // CF_DIB 형식 처리
                else if (IsClipboardFormatAvailable(CF_DIB))
                {
                    IntPtr hMem = GetClipboardData(CF_DIB);
                    if (hMem != IntPtr.Zero)
                    {
                        IntPtr pDib = GlobalLock(hMem);
                        if (pDib != IntPtr.Zero)
                        {
                            try
                            {
                                int size = GlobalSize(hMem);
                                byte[] dibData = new byte[size];
                                System.Runtime.InteropServices.Marshal.Copy(pDib, dibData, 0, size);
                                
                                using (MemoryStream ms = new MemoryStream(dibData))
                                {
                                    bitmap = new System.Drawing.Bitmap(ms);
                                }
                            }
                            finally
                            {
                                GlobalUnlock(hMem);
                            }
                        }
                    }
                }

                if (bitmap == null)
                {
                    Debug.LogWarning("Clipboard에서 이미지를 가져올 수 없습니다.");
                    return null;
                }

                // Bitmap을 PNG byte[]로 변환
                using (MemoryStream ms = new MemoryStream())
                {
                    bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    bitmap.Dispose();
                    byte[] imageBytes = ms.ToArray();
                    Debug.Log($"Clipboard 이미지를 메모리에 로드했습니다: {imageBytes.Length} bytes");
                    return imageBytes;
                }
            }
            finally
            {
                CloseClipboard();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Clipboard 이미지 로드 실패: {ex.Message}");
            return null;
        }
    }

    // 클립보드에서 이미지를 가져와서 파일로 저장
    public bool SaveImageFromClipboard()
    {
        try
        {
            // 클립보드 열기
            if (!OpenClipboard(IntPtr.Zero))
            {
                Debug.LogWarning("Clipboard를 열 수 없습니다.");
                return false;
            }

            try
            {
                // 클립보드에 이미지가 있는지 확인 (CF_BITMAP 우선)
                if (!IsClipboardFormatAvailable(CF_BITMAP) && !IsClipboardFormatAvailable(CF_DIB))
                {
                    Debug.LogWarning("Clipboard에 이미지가 없습니다.");
                    return false;
                }

                System.Drawing.Bitmap bitmap = null;

                // CF_BITMAP 형식으로 이미지 가져오기 (우선순위)
                if (IsClipboardFormatAvailable(CF_BITMAP))
                {
                    IntPtr hBitmap = GetClipboardData(CF_BITMAP);
                    if (hBitmap != IntPtr.Zero)
                    {
                        bitmap = System.Drawing.Bitmap.FromHbitmap(hBitmap);
                    }
                }
                // CF_DIB 형식 처리
                else if (IsClipboardFormatAvailable(CF_DIB))
                {
                    IntPtr hMem = GetClipboardData(CF_DIB);
                    if (hMem != IntPtr.Zero)
                    {
                        IntPtr pDib = GlobalLock(hMem);
                        if (pDib != IntPtr.Zero)
                        {
                            try
                            {
                                int size = GlobalSize(hMem);
                                byte[] dibData = new byte[size];
                                System.Runtime.InteropServices.Marshal.Copy(pDib, dibData, 0, size);
                                
                                // DIB 데이터를 MemoryStream으로 변환하여 Bitmap 생성
                                using (MemoryStream ms = new MemoryStream(dibData))
                                {
                                    bitmap = new System.Drawing.Bitmap(ms);
                                }
                            }
                            finally
                            {
                                GlobalUnlock(hMem);
                            }
                        }
                    }
                }

                if (bitmap == null)
                {
                    Debug.LogWarning("Clipboard에서 이미지를 가져올 수 없습니다.");
                    return false;
                }

                // 디렉토리 생성
                string directory = Path.Combine(UnityEngine.Application.persistentDataPath, "Screenshots");
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 파일로 저장
                string filePath = Path.Combine(directory, "clipboard.png");
                bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);

                bitmap.Dispose();

                Debug.Log($"Clipboard 이미지가 저장되었습니다: {filePath}");
                return true;
            }
            finally
            {
                CloseClipboard();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Clipboard 이미지 저장 실패: {ex.Message}");
            return false;
        }
    }

    // 클립보드에서 텍스트를 가져와서 변수에 저장
    public string GetTextFromClipboard()
    {
        try
        {
            // 클립보드 열기
            if (!OpenClipboard(IntPtr.Zero))
            {
                Debug.LogWarning("Clipboard를 열 수 없습니다.");
                clipboardText = "";
                return null;
            }

            try
            {
                // 클립보드에 텍스트가 있는지 확인 (CF_UNICODETEXT 우선)
                if (!IsClipboardFormatAvailable(CF_UNICODETEXT) && !IsClipboardFormatAvailable(CF_TEXT))
                {
                    Debug.LogWarning("Clipboard에 텍스트가 없습니다.");
                    clipboardText = "";
                    return null;
                }

                string text = null;

                // CF_UNICODETEXT 형식으로 텍스트 가져오기 (우선순위)
                if (IsClipboardFormatAvailable(CF_UNICODETEXT))
                {
                    IntPtr hMem = GetClipboardData(CF_UNICODETEXT);
                    if (hMem != IntPtr.Zero)
                    {
                        IntPtr pText = GlobalLock(hMem);
                        if (pText != IntPtr.Zero)
                        {
                            try
                            {
                                text = Marshal.PtrToStringUni(pText);
                            }
                            finally
                            {
                                GlobalUnlock(hMem);
                            }
                        }
                    }
                }
                // CF_TEXT 형식 처리
                else if (IsClipboardFormatAvailable(CF_TEXT))
                {
                    IntPtr hMem = GetClipboardData(CF_TEXT);
                    if (hMem != IntPtr.Zero)
                    {
                        IntPtr pText = GlobalLock(hMem);
                        if (pText != IntPtr.Zero)
                        {
                            try
                            {
                                text = Marshal.PtrToStringAnsi(pText);
                            }
                            finally
                            {
                                GlobalUnlock(hMem);
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(text))
                {
                    Debug.LogWarning("Clipboard에서 텍스트를 가져올 수 없습니다.");
                    clipboardText = "";
                    return null;
                }

                // public 변수에 저장
                clipboardText = text;
                
                // Debug.Log에 표시
                Debug.Log($"Clipboard 텍스트: {clipboardText}");
                return clipboardText;
            }
            finally
            {
                CloseClipboard();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Clipboard 텍스트 가져오기 실패: {ex.Message}");
            clipboardText = "";
            return null;
        }
    }
}

