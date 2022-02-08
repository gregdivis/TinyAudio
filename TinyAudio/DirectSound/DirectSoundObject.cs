﻿using System;
using TinyAudio.DirectSound.Interop;

namespace TinyAudio.DirectSound
{
    /// <summary>
    /// Provides access to a DirectSound device.
    /// </summary>
    internal sealed class DirectSoundObject : IDisposable
    {
        private bool disposed;
        private readonly unsafe DirectSound8Inst* directSound;

        private static WeakReference? instance;
        private static readonly object getInstanceLock = new();

        private const uint bufferFlags = 0x00000008u | 0x00000020u | 0x00000080u | 0x00008000u;

        private DirectSoundObject(IntPtr hwnd)
        {
            unsafe
            {
                NativeMethods.DirectSoundCreate8(null, out var ds8, null);
                this.directSound = ds8;

                uint res = ds8->Vtbl->SetCooperativeLevel(ds8, hwnd, 2);
                if (res != 0)
                    throw new InvalidOperationException("Unable to set DirectSound cooperative level.");
            }
        }
        ~DirectSoundObject()
        {
            this.Dispose(false);
        }

        public DirectSoundBuffer CreateBuffer(AudioFormat format, TimeSpan bufferLength)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(DirectSoundObject));
            if (bufferLength < new TimeSpan(0, 0, 0, 0, 5) || bufferLength > new TimeSpan(1, 0, 0))
                throw new ArgumentOutOfRangeException(nameof(bufferLength));

            double bytesPerSec = format.SampleRate * format.Channels * format.BytesPerSample;
            return CreateBuffer(format, (int)(bufferLength.TotalSeconds * bytesPerSec));
        }
        public DirectSoundBuffer CreateBuffer(AudioFormat format, int bufferSize)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(DirectSoundObject));
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));

            unsafe
            {
                var dsbd = new DSBUFFERDESC
                {
                    dwSize = (uint)sizeof(DSBUFFERDESC),
                    dwFlags = bufferFlags
                };

                var wfx = new WAVEFORMATEX
                {
                    nChannels = (ushort)format.Channels,
                    wBitsPerSample = (ushort)(format.BytesPerSample * 8u),
                    wFormatTag = 1,
                    nSamplesPerSec = (uint)format.SampleRate
                };

                wfx.nBlockAlign = (ushort)(wfx.nChannels * (wfx.wBitsPerSample / 8u));
                wfx.nAvgBytesPerSec = wfx.nSamplesPerSec * wfx.nBlockAlign;

                dsbd.dwBufferBytes = (uint)bufferSize;

                DirectSoundBuffer8Inst* dsbuf = null;
                dsbd.lpwfxFormat = &wfx;

                uint res = this.directSound->Vtbl->CreateBuffer(this.directSound, &dsbd, &dsbuf, null);
                if (res != 0)
                    throw new InvalidOperationException("Unable to create DirectSound buffer.");

                return new DirectSoundBuffer(dsbuf, this);
            }
        }

        /// <summary>
        /// Releases resources used by the DirectSound instance.
        /// </summary>
        void IDisposable.Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Returns the current DirectSound instance or creates a new one if necessary.
        /// </summary>
        /// <param name="hwnd">Main application window handle.</param>
        /// <returns>Current DirectSound instance.</returns>
        public static DirectSoundObject GetInstance(IntPtr hwnd)
        {
            lock (getInstanceLock)
            {
                if (instance?.Target is DirectSoundObject directSound)
                    return directSound;

                directSound = new DirectSoundObject(hwnd);
                instance = new WeakReference(directSound);
                return directSound;
            }
        }

        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                this.disposed = true;
                unsafe
                {
                    this.directSound->Vtbl->Release(this.directSound);
                }
            }
        }
    }
}
