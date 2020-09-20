using System;
using System.Runtime.InteropServices;

namespace TinyAudio.Wasapi
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct AudioRenderClientInst
    {
        public AudioRenderClientV* Vtbl;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct AudioRenderClientV
    {
        public delegate* unmanaged[Stdcall]<AudioRenderClientInst*, Guid*, void**, uint> QueryInterface;
        public delegate* unmanaged[Stdcall]<AudioRenderClientInst*, uint> AddRef;
        public delegate* unmanaged[Stdcall]<AudioRenderClientInst*, uint> Release;

        public delegate* unmanaged[Stdcall]<AudioRenderClientInst*, uint, byte**, uint> GetBuffer;
        public delegate* unmanaged[Stdcall]<AudioRenderClientInst*, uint, uint, uint> ReleaseBuffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct AudioClientInst
    {
        public AudioClientV* Vtbl;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct AudioClientV
    {
        public delegate* unmanaged[Stdcall]<AudioClientInst*, Guid*, void**, uint> QueryInterface;
        public delegate* unmanaged[Stdcall]<AudioClientInst*, uint> AddRef;
        public delegate* unmanaged[Stdcall]<AudioClientInst*, uint> Release;

        public delegate* unmanaged[Stdcall]<AudioClientInst*, uint, uint, long, long, WAVEFORMATEX*, Guid*, uint> Initialize;
        public delegate* unmanaged[Stdcall]<AudioClientInst*, uint*, uint> GetBufferSize;
        public delegate* unmanaged[Stdcall]<AudioClientInst*, long*, uint> GetStreamLatency;
        public delegate* unmanaged[Stdcall]<AudioClientInst*, uint*, uint> GetCurrentPadding;
        public delegate* unmanaged[Stdcall]<AudioClientInst*, uint, WAVEFORMATEX*, WAVEFORMATEX**, uint> IsFormatSupported;
        public delegate* unmanaged[Stdcall]<AudioClientInst*, WAVEFORMATEX**, uint> GetMixFormat;
        public delegate* unmanaged[Stdcall]<AudioClientInst*, long*, long*, uint> GetDevicePeriod;
        public delegate* unmanaged[Stdcall]<AudioClientInst*, uint> Start;
        public delegate* unmanaged[Stdcall]<AudioClientInst*, uint> Stop;
        public delegate* unmanaged[Stdcall]<AudioClientInst*, uint> Reset;
        public delegate* unmanaged[Stdcall]<AudioClientInst*, IntPtr, uint> SetEventHandle;
        public delegate* unmanaged[Stdcall]<AudioClientInst*, Guid*, void**, uint> GetService;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct MMDeviceInst
    {
        public MMDeviceV* Vtbl;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct MMDeviceV
    {
        public delegate* unmanaged[Stdcall]<MMDeviceInst*, Guid*, void**, uint> QueryInterface;
        public delegate* unmanaged[Stdcall]<MMDeviceInst*, uint> AddRef;
        public delegate* unmanaged[Stdcall]<MMDeviceInst*, uint> Release;

        public delegate* unmanaged[Stdcall]<MMDeviceInst*, Guid*, uint, void*, void**, uint> Activate;
        public IntPtr OpenPropertyStore;
        public IntPtr GetId;
        public IntPtr GetState;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct DeviceEnumeratorInst
    {
        public DeviceEnumeratorV* Vtbl;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct DeviceEnumeratorV
    {
        public delegate* unmanaged[Stdcall]<DeviceEnumeratorInst*, Guid*, void**, uint> QueryInterface;
        public delegate* unmanaged[Stdcall]<DeviceEnumeratorInst*, uint> AddRef;
        public delegate* unmanaged[Stdcall]<DeviceEnumeratorInst*, uint> Release;

        public IntPtr EnumAudioEndpoints;
        public delegate* unmanaged[Stdcall]<DeviceEnumeratorInst*, EDataFlow, ERole, MMDeviceInst**, uint> GetDefaultAudioEndpoint;
        public IntPtr GetDevice;
        public IntPtr RegisterEndpointNotificationCallback;
        public IntPtr UnregisterEndpointNotificationCallback;
    }

    internal enum EDataFlow : uint
    {
        eRender,
        eCapture,
        eAll
    }

    internal enum ERole : uint
    {
        eConsole,
        eMultimedia,
        eCommunications
    }
}
