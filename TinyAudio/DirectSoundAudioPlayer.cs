using System;
using System.Diagnostics;
using System.Threading;
using TinyAudio.DirectSound;
using TinyAudio.DirectSound.Interop;

namespace TinyAudio
{
    public sealed class DirectSoundAudioPlayer : AudioPlayer
    {
        private readonly DirectSoundBuffer directSoundBuffer;
        private Timer? bufferTimer;
        private readonly uint dataInterval;
        private volatile bool handlingTimer;
        private bool disposed;

        public DirectSoundAudioPlayer(AudioFormat format, TimeSpan bufferLength)
            : base(format)
        {
            IntPtr hwnd;
            using (var p = Process.GetCurrentProcess())
            {
                hwnd = p.MainWindowHandle;
            }

            if (hwnd == IntPtr.Zero)
                hwnd = NativeMethods.GetConsoleWindow();

            this.dataInterval = (uint)(bufferLength.TotalMilliseconds * 0.4);

            var dsound = DirectSoundObject.GetInstance(hwnd);
            this.directSoundBuffer = dsound.CreateBuffer(format, bufferLength);
        }

        protected override void Start()
        {
            uint maxBytes = this.directSoundBuffer.GetFreeBytes();
            if (maxBytes >= 32)
                this.WriteBuffer(maxBytes);

            this.directSoundBuffer.Play(PlaybackMode.LoopContinuously);
            this.bufferTimer = new Timer(_ => this.PollingThread(), null, 0, this.dataInterval);
        }
        protected override void Stop()
        {
            this.directSoundBuffer.Stop();
            this.bufferTimer?.Dispose();
            this.bufferTimer = null;
        }

        protected override void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.StopPlayback();
                    this.directSoundBuffer.Dispose();
                }

                this.disposed = true;
            }
        }

        private void PollingThread()
        {
            if (this.handlingTimer)
                return;

            this.handlingTimer = true;
            try
            {
                uint maxBytes = this.directSoundBuffer.GetFreeBytes() & ~3u;
                if (maxBytes >= 32)
                    this.WriteBuffer(maxBytes);
            }
            finally
            {
                this.handlingTimer = false;
            }
        }

        private void WriteBuffer(uint maxBytes)
        {
            var buffer = this.directSoundBuffer.Acquire(maxBytes);
            if (buffer.Valid)
            {
                uint ptr1Written = 0;
                uint ptr2Written = 0;
                var format = this.Format.SampleFormat;

                try
                {
                    if (format == SampleFormat.SignedPcm16)
                    {
                        var s1 = buffer.GetSpan1<short>();
                        this.RaiseCallback(s1, out ptr1Written);
                        if (buffer.Split && ptr1Written == s1.Length)
                            this.RaiseCallback(buffer.GetSpan2<short>(), out ptr2Written);
                        ptr1Written *= 2;
                        ptr2Written *= 2;
                    }
                    else if (format == SampleFormat.UnsignedPcm8)
                    {
                        var s1 = buffer.GetSpan1<byte>();
                        this.RaiseCallback(s1, out ptr1Written);
                        if (buffer.Split && ptr1Written == s1.Length)
                            this.RaiseCallback(buffer.GetSpan2<byte>(), out ptr2Written);
                    }
                    else if (format == SampleFormat.IeeeFloat32)
                    {
                        var s1 = buffer.GetSpan1<float>();
                        this.RaiseCallback(s1, out ptr1Written);
                        if (buffer.Split && ptr1Written == s1.Length)
                            this.RaiseCallback(buffer.GetSpan2<float>(), out ptr2Written);
                        ptr1Written *= 4;
                        ptr2Written *= 4;
                    }
                    else
                    {
                        throw new InvalidOperationException("Sample format not supported.");
                    }
                }
                finally
                {
                    this.directSoundBuffer.Unlock(buffer.Ptr1, ptr1Written, buffer.Ptr2, ptr2Written);
                }
            }
        }
    }
}
