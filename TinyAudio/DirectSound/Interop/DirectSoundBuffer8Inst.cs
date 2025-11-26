using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace TinyAudio.DirectSound.Interop;

[SupportedOSPlatform("windows")]
[StructLayout(LayoutKind.Sequential)]
internal struct DirectSoundBuffer8Inst
{
    public unsafe DirectSoundBuffer8V* Vtbl;
}
