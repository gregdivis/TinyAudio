using System;
using System.Runtime.InteropServices;

namespace TinyAudio.DirectSound
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct DirectSound8Inst
    {
        public unsafe DirectSound8V* Vtbl;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct DirectSound8V
    {
        public delegate* unmanaged[Stdcall]<DirectSound8Inst*, Guid*, void**, uint> QueryInterface;
        public delegate* unmanaged[Stdcall]<DirectSound8Inst*, uint> AddRef;
        public delegate* unmanaged[Stdcall]<DirectSound8Inst*, uint> Release;

        public delegate* unmanaged[Stdcall]<DirectSound8Inst*, DSBUFFERDESC*, void*, DirectSoundBuffer8Inst*, uint> CreateBuffer;
        public delegate* unmanaged[Stdcall]<DirectSound8Inst*, DSCAPS*, uint> GetCaps;
        public delegate* unmanaged[Stdcall]<DirectSound8Inst*, void*, void**, uint> DuplicateSoundBuffer;
        public delegate* unmanaged[Stdcall]<DirectSound8Inst*, IntPtr, uint, uint> SetCooperativeLevel;

        public IntPtr Compact;
        public IntPtr GetSpeakerConfig;
        public IntPtr SetSpeakerConfig;
        public IntPtr Initialize;
        public IntPtr VerifyCertification;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DSBUFFERDESC
    {
        public uint dwSize;
        public uint dwFlags;
        public uint dwBufferBytes;
        public uint dwReserved;
        public unsafe WAVEFORMATEX* lpwfxFormat;
        public unsafe fixed byte guid3DAlgorithm[16];
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DSCAPS
    {
        public uint dwSize;
        public uint dwFlags;
        public uint dwMinSecondarySampleRate;
        public uint dwMaxSecondarySampleRate;
        public uint dwPrimaryBuffers;
        public uint dwMaxHwMixingAllBuffers;
        public uint dwMaxHwMixingStaticBuffers;
        public uint dwMaxHwMixingStreamingBuffers;
        public uint dwFreeHwMixingAllBuffers;
        public uint dwFreeHwMixingStaticBuffers;
        public uint dwFreeHwMixingStreamingBuffers;
        public uint dwMaxHw3DAllBuffers;
        public uint dwMaxHw3DStaticBuffers;
        public uint dwMaxHw3DStreamingBuffers;
        public uint dwFreeHw3DAllBuffers;
        public uint dwFreeHw3DStaticBuffers;
        public uint dwFreeHw3DStreamingBuffers;
        public uint dwTotalHwMemBytes;
        public uint dwFreeHwMemBytes;
        public uint dwMaxContigFreeHwMemBytes;
        public uint dwUnlockTransferRateHwBuffers;
        public uint dwPlayCpuOverheadSwBuffers;
        public uint dwReserved1;
        public uint dwReserved2;
    }
}
