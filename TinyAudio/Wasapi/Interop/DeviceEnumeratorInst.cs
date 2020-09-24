using System.Runtime.InteropServices;

namespace TinyAudio.Wasapi.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct DeviceEnumeratorInst
    {
        public DeviceEnumeratorV* Vtbl;
    }
}
