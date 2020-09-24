using System.Runtime.InteropServices;

namespace TinyAudio.Wasapi.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct AudioClientInst
    {
        public AudioClientV* Vtbl;
    }
}
