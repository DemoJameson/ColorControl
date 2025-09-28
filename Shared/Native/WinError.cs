﻿using System.Runtime.InteropServices;
using System.Text;

namespace ColorControl.Shared.Native;

public static class WinError
{
    #region Win32

    [DllImport("Kernel32.dll", SetLastError = true)]
    private static extern uint FormatMessage(
        uint dwFlags,
        IntPtr lpSource,
        uint dwMessageId,
        uint dwLanguageId,
        StringBuilder lpBuffer,
        int nSize,
        IntPtr Arguments);

    private const uint FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;

    #endregion

    public static (int errorCode, string message) GetCodeMessage()
    {
        var errorCode = Marshal.GetLastWin32Error();
        return (errorCode, GetMessage(errorCode));
    }

    public static string GetMessage() => GetMessage(Marshal.GetLastWin32Error());

    public static string GetMessage(int errorCode)
    {
        var message = new StringBuilder($"Code: 0x{errorCode:X8}");

        var buffer = new StringBuilder(512); // This 512 capacity is arbitrary.
        if (FormatMessage(
            FORMAT_MESSAGE_FROM_SYSTEM,
            IntPtr.Zero,
            (uint)errorCode,
            0x0409, // US (English)
            buffer,
            buffer.Capacity,
            IntPtr.Zero) > 0)
        {
            message.Append(", Message: ").Append(buffer);
        }

        return message.ToString();
    }
}
