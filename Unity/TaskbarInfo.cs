using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class TaskbarInfo : MonoBehaviour
{
    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public static Rect GetTaskbarRect()
    {
        IntPtr taskbarHandle = FindWindow("Shell_TrayWnd", null); // 작업 표시줄의 핸들을 찾음
        if (taskbarHandle != IntPtr.Zero)
        {
            RECT rect;
            if (GetWindowRect(taskbarHandle, out rect))
            {
                return new Rect(rect.Left, Screen.height - (rect.Bottom - rect.Top), rect.Right - rect.Left, rect.Bottom - rect.Top);
            }
        }
        return Rect.zero;
    }

    public Rect TaskbarRectangle;

    void Start()
    {
        TaskbarRectangle = GetTaskbarRect();
        Debug.Log("Taskbar Position: " + TaskbarRectangle);
    }
}
