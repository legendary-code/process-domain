using System.Runtime.InteropServices;
using System.Text;

namespace AppSecInc.ProcessDomain
{
    internal static class WinApi
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetDllDirectory(string lpPathName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int GetDllDirectory(int bufferLength, StringBuilder directory);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern void SetLastError(uint dwErrCode);
    }
}
