using System.Runtime.InteropServices;

namespace TinyAudio.DirectSound.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct DSBUFFERDESC
    {
        public uint dwSize;
        public uint dwFlags;
        public uint dwBufferBytes;
        public uint dwReserved;
        public unsafe WAVEFORMATEX* lpwfxFormat;
        public unsafe fixed byte guid3DAlgorithm[16];
    }
}
