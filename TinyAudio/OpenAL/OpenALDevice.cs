using Microsoft.Win32.SafeHandles;

namespace TinyAudio.OpenAL;

internal sealed class OpenALDevice : SafeHandleZeroOrMinusOneIsInvalid
{
    public OpenALDevice() : base(true)
    {
    }

    protected override bool ReleaseHandle() => NativeMethods.alcCloseDevice(this.handle);
}
