using System.Runtime.Versioning;

namespace TinyAudio.Wasapi.Interop;

[SupportedOSPlatform("windows")]
internal enum ERole : uint
{
    eConsole,
    eMultimedia,
    eCommunications
}
