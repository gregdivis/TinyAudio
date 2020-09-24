using System;
using System.Runtime.InteropServices;

namespace TinyAudio.Wasapi.Interop
{
    internal static class NativeMethods
    {
        [DllImport("ole32.dll")]
        public static extern unsafe uint CoCreateInstance(Guid* rclsid, void** pUnkOuter, uint dwClsContext, Guid* riid, void** ppv);
    }
}
