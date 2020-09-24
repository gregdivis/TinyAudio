using System.Runtime.InteropServices;

namespace TinyAudio.DirectSound.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct DSBCAPS
    {
        public uint dwSize;
        public uint dwFlags;
        public uint dwBufferBytes;
        public uint dwUnlockTransferRate;
        public uint dwPlayCpuOverhead;
    }
}
