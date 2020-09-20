using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TinyAudio.Wasapi
{
    internal static class NativeMethods
    {
        [DllImport("ole32.dll")]
        public static extern unsafe uint CoCreateInstance(Guid* rclsid, void** pUnkOuter, uint dwClsContext, Guid* riid, void** ppv);
    }
}
