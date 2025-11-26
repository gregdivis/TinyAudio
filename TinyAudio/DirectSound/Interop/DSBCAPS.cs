using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace TinyAudio.DirectSound.Interop;

[SupportedOSPlatform("windows")]
[StructLayout(LayoutKind.Sequential)]
internal struct DSBCAPS
{
    public uint dwSize;
    public uint dwFlags;
    public uint dwBufferBytes;
    public uint dwUnlockTransferRate;
    public uint dwPlayCpuOverhead;
}
