using System.Runtime.InteropServices;

namespace TinyAudio.DirectSound.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct DirectSoundBuffer8Inst
    {
        public unsafe DirectSoundBuffer8V* Vtbl;
    }
}
