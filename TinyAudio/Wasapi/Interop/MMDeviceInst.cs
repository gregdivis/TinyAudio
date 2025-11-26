using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace TinyAudio.Wasapi.Interop;

[SupportedOSPlatform("windows")]
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct MMDeviceInst
{
    public MMDeviceV* Vtbl;
}
