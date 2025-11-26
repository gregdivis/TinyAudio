using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace TinyAudio.DirectSound.Interop;

[SupportedOSPlatform("windows")]
[StructLayout(LayoutKind.Sequential)]
internal struct DirectSound8Inst
{
    public unsafe DirectSound8V* Vtbl;
}
