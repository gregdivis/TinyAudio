using Microsoft.Win32.SafeHandles;

namespace TinyAudio.OpenAL;

internal sealed class DeviceContext : SafeHandleZeroOrMinusOneIsInvalid
{
    public DeviceContext() : base(true)
    {
    }

    protected override bool ReleaseHandle()
    {
        NativeMethods.alcDestroyContext(this.handle);
        return true;
    }
}
