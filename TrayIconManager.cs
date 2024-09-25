/**
나중에 다시 해보는걸로
*/
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using UnityEngine;

public class TrayIconManager : MonoBehaviour
{
    private NotifyIcon trayIcon;

    // Windows API imports for window style manipulation
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    private const int GWL_EXSTYLE = -20; // Extended window styles
    private const int WS_EX_TOOLWINDOW = 0x00000080; // ToolWindow style (Hides from taskbar)
    private const int WS_EX_APPWINDOW = 0x00040000; // AppWindow style (Shows in taskbar)

    private void Start()
    {
        // Create the tray icon
        trayIcon = new NotifyIcon();
        trayIcon.Icon = new System.Drawing.Icon("Assets/Icons/icon_arona.ico"); // Your tray icon
        trayIcon.Text = "MY-Little-JARVIS";
        trayIcon.Visible = true;

        // Create context menu
        System.Windows.Forms.ContextMenu trayMenu = new System.Windows.Forms.ContextMenu();
        trayMenu.MenuItems.Add("Minimize", MinimizeApp);
        trayMenu.MenuItems.Add("Exit", ExitApp);
        trayIcon.ContextMenu = trayMenu;

        // Hide Unity from the taskbar
        HideFromTaskbar();
    }

    private void HideFromTaskbar()
    {
        // Get the current Unity window handle
        IntPtr windowHandle = GetActiveWindow();

        // Get current window style
        int style = GetWindowLong(windowHandle, GWL_EXSTYLE);

        // Apply the ToolWindow style (removes from taskbar)
        SetWindowLong(windowHandle, GWL_EXSTYLE, (style | WS_EX_TOOLWINDOW) & ~WS_EX_APPWINDOW);
    }

    private void MinimizeApp(object sender, EventArgs e)
    {
        IntPtr windowHandle = GetForegroundWindow();
        ShowWindow(windowHandle, 6); // Minimize the window
    }

    private void ExitApp(object sender, EventArgs e)
    {
        // Remove tray icon and exit the application
        trayIcon.Visible = false;
        trayIcon.Dispose();
        UnityEngine.Application.Quit();
    }

    private void OnApplicationQuit()
    {
        // Clean up the tray icon
        trayIcon.Visible = false;
    }

    // Import the ShowWindow function to minimize the window
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
}
