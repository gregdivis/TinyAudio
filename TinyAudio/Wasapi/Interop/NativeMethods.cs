using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace TinyAudio.Wasapi.Interop;

[SupportedOSPlatform("windows")]
internal static class NativeMethods
{
    [DllImport("ole32.dll")]
    public static extern unsafe uint CoCreateInstance(Guid* rclsid, void** pUnkOuter, uint dwClsContext, Guid* riid, void** ppv);
}
