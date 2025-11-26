using System.Runtime.Versioning;

namespace TinyAudio.Wasapi.Interop;

[SupportedOSPlatform("windows")]
internal enum EDataFlow : uint
{
    eRender,
    eCapture,
    eAll
}
