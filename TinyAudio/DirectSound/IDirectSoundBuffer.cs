using System;
using System.Runtime.InteropServices;

namespace TinyAudio.DirectSound
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct DirectSoundBuffer8Inst
    {
        public unsafe DirectSoundBuffer8V* Vtbl;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct DirectSoundBuffer8V
    {
        public delegate* unmanaged[Stdcall]<DirectSoundBuffer8Inst*, Guid*, void**, uint> QueryInterface;
        public delegate* unmanaged[Stdcall]<DirectSoundBuffer8Inst*, uint> AddRef;
        public delegate* unmanaged[Stdcall]<DirectSoundBuffer8Inst*, uint> Release;

        public delegate* unmanaged[Stdcall]<DirectSoundBuffer8Inst*, DSBCAPS*, uint> GetCaps;
        public delegate* unmanaged[Stdcall]<DirectSoundBuffer8Inst*, uint*, uint*, uint> GetCurrentPosition;
        public IntPtr GetFormat;
        public delegate* unmanaged[Stdcall]<DirectSoundBuffer8Inst*, int*, uint> GetVolume;
        public delegate* unmanaged[Stdcall]<DirectSoundBuffer8Inst*, int*, uint> GetPan;
        public delegate* unmanaged[Stdcall]<DirectSoundBuffer8Inst*, uint*, uint> GetFrequency;
        public delegate* unmanaged[Stdcall]<DirectSoundBuffer8Inst*, uint*, uint> GetStatus;
        public IntPtr Initialize;
        public delegate* unmanaged[Stdcall]<DirectSoundBuffer8Inst*, uint, uint, void**, uint*, void**, uint*, uint, uint> Lock;
        public delegate* unmanaged[Stdcall]<DirectSoundBuffer8Inst*, uint, uint, uint, uint> Play;
        public delegate* unmanaged[Stdcall]<DirectSoundBuffer8Inst*, uint, uint> SetCurrentPosition;
        public IntPtr SetFormat;
        public delegate* unmanaged[Stdcall]<DirectSoundBuffer8Inst*, int, uint> SetVolume;
        public delegate* unmanaged[Stdcall]<DirectSoundBuffer8Inst*, int, uint> SetPan;
        public delegate* unmanaged[Stdcall]<DirectSoundBuffer8Inst*, uint, uint> SetFrequency;
        public delegate* unmanaged[Stdcall]<DirectSoundBuffer8Inst*, uint> Stop;
        public delegate* unmanaged[Stdcall]<DirectSoundBuffer8Inst*, void*, uint, void*, uint, uint> Unlock;
        public IntPtr Restore;
        public IntPtr SetFX;
        public IntPtr AcquireResources;
        public IntPtr GetObjectInPath;
    }

    //[StructLayout(LayoutKind.Sequential)]
    //internal struct DirectSoundBuffer8V
    //{
    //    public IntPtr QueryInterface;
    //    public IntPtr AddRef;
    //    public IntPtr Release;

    //    public IntPtr GetCaps;
    //    public IntPtr GetCurrentPosition;
    //    public IntPtr GetFormat;
    //    public IntPtr GetVolume;
    //    public IntPtr GetPan;
    //    public IntPtr GetFrequency;
    //    public IntPtr GetStatus;
    //    public IntPtr Initialize;
    //    public IntPtr Lock;
    //    public IntPtr Play;
    //    public IntPtr SetCurrentPosition;
    //    public IntPtr SetFormat;
    //    public IntPtr SetVolume;
    //    public IntPtr SetPan;
    //    public IntPtr SetFrequency;
    //    public IntPtr Stop;
    //    public IntPtr Unlock;
    //    public IntPtr Restore;
    //    public IntPtr SetFX;
    //    public IntPtr AcquireResources;
    //    public IntPtr GetObjectInPath;
    //}

    [StructLayout(LayoutKind.Sequential)]
    internal struct DSBCAPS
    {
        public uint dwSize;
        public uint dwFlags;
        public uint dwBufferBytes;
        public uint dwUnlockTransferRate;
        public uint dwPlayCpuOverhead;
    }
}
