using System;

namespace AppSecInc.ProcessDomain.Utils
{
    public static class AssemblyUtils
    {
        public static string GetFilePathFromFileUri(string uri)
        {
            var fileUri = new Uri(uri);
            return Uri.UnescapeDataString(fileUri.AbsolutePath).Replace('/', '\\');
        }
    }
}