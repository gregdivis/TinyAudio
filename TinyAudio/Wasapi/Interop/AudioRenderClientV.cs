﻿using System;
using System.Runtime.InteropServices;

namespace TinyAudio.Wasapi.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct AudioRenderClientV
    {
        public delegate* unmanaged[Stdcall]<AudioRenderClientInst*, Guid*, void**, uint> QueryInterface;
        public delegate* unmanaged[Stdcall]<AudioRenderClientInst*, uint> AddRef;
        public delegate* unmanaged[Stdcall]<AudioRenderClientInst*, uint> Release;

        public delegate* unmanaged[Stdcall]<AudioRenderClientInst*, uint, byte**, uint> GetBuffer;
        public delegate* unmanaged[Stdcall]<AudioRenderClientInst*, uint, uint, uint> ReleaseBuffer;
    }
}
