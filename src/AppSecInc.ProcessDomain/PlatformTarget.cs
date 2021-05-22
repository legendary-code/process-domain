using System;

namespace AppSecInc.ProcessDomain
{
    public enum PlatformTarget
    {
        AnyCPU,
        x86,
        Itanium,
        x64
    }

    public static class PlatformTargetUtil
    {
        public static string GetCompilerArgument(PlatformTarget target)
        {
            switch (target)
            {
                case PlatformTarget.AnyCPU:
                    return "/platform:anycpu";
                case PlatformTarget.Itanium:
                    return "/platform:Itanium";
                case PlatformTarget.x64:
                    return "/platform:x64";
                case PlatformTarget.x86:
                    return "/platform:x86";
            }

            throw new NotSupportedException("Unknown platform target specified");
        }
    }
}
