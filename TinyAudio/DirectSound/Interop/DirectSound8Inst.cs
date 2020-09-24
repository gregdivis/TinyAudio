using System.Runtime.InteropServices;

namespace TinyAudio.DirectSound.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct DirectSound8Inst
    {
        public unsafe DirectSound8V* Vtbl;
    }
}
