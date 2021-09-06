using System;
using System.Runtime.InteropServices;

namespace TinyAudio
{
    /// <summary>
    /// Implements a background audio playback stream.
    /// </summary>
    public abstract class AudioPlayer : IDisposable
    {
        private BufferNeededCallback<short>? pcm16Callback;
        private BufferNeededCallback<float>? float32Callback;
        private BufferNeededCallback<byte>? pcm8Callback;
        private byte[]? conversionBuffer;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioPlayer"/> class.
        /// </summary>
        /// <param name="format">Format of the audio stream.</param>
        protected AudioPlayer(AudioFormat format)
        {
            this.Format = format!;
        }

        /// <summary>
        /// Gets the playback audio format.
        /// </summary>
        public AudioFormat Format { get; }
        /// <summary>
        /// Gets a value indicating whether the player is active.
        /// </summary>
        public bool Playing { get; private set; }

        /// <summary>
        /// Begins playback of the background stream of 16-bit PCM data.
        /// </summary>
        /// <param name="callback">Delegate invoked when more data is needed for the playback buffer.</param>
        /// <exception cref="ArgumentNullException"><paramref name="callback"/> is null.</exception>
        /// <exception cref="InvalidOperationException">The stream is already playing.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="AudioPlayer"/> instance has been disposed.</exception>
        public void BeginPlayback(BufferNeededCallback<short> callback)
        {
            if (this.disposed)
                throw new ObjectDisposedException(nameof(AudioPlayer));
            if (this.Playing)
                throw new InvalidOperationException("Playback has already started.");

            this.pcm16Callback = callback!;
            this.pcm8Callback = null;
            this.float32Callback = null;
            this.Playing = true;
            this.Start(true);
        }
        /// <summary>
        /// Begins playback of the background stream of 326-bit IEEE floating point data.
        /// </summary>
        /// <param name="callback">Delegate invoked when more data is needed for the playback buffer.</param>
        /// <exception cref="ArgumentNullException"><paramref name="callback"/> is null.</exception>
        /// <exception cref="InvalidOperationException">The stream is already playing.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="AudioPlayer"/> instance has been disposed.</exception>
        public void BeginPlayback(BufferNeededCallback<float> callback)
        {
            if (this.disposed)
                throw new ObjectDisposedException(nameof(AudioPlayer));
            if (this.Playing)
                throw new InvalidOperationException("Playback has already started.");

            this.float32Callback = callback!;
            this.pcm16Callback = null;
            this.pcm8Callback = null;
            this.Playing = true;
            this.Start(true);
        }
        /// <summary>
        /// Begins playback of the background stream of 8-bit PCM data.
        /// </summary>
        /// <param name="callback">Delegate invoked when more data is needed for the playback buffer.</param>
        /// <exception cref="ArgumentNullException"><paramref name="callback"/> is null.</exception>
        /// <exception cref="InvalidOperationException">The stream is already playing.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="AudioPlayer"/> instance has been disposed.</exception>
        public void BeginPlayback(BufferNeededCallback<byte> callback)
        {
            if (this.disposed)
                throw new ObjectDisposedException(nameof(AudioPlayer));
            if (this.Playing)
                throw new InvalidOperationException("Playback has already started.");

            this.pcm8Callback = callback!;
            this.pcm16Callback = null;
            this.float32Callback = null;
            this.Playing = true;
            this.Start(true);
        }
        /// <summary>
        /// Begins playback of the background stream.
        /// </summary>
        /// <exception cref="InvalidOperationException">The stream is already playing.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="AudioPlayer"/> instance has been disposed.</exception>
        public void BeginPlayback()
        {
            if (this.disposed)
                throw new ObjectDisposedException(nameof(AudioPlayer));
            if (this.Playing)
                throw new InvalidOperationException("Playback has already started.");

            this.pcm8Callback = null;
            this.pcm16Callback = null;
            this.float32Callback = null;
            this.Playing = true;
            this.Start(false);
        }
        /// <summary>
        /// Stops audio playback if it is currently playing.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The <see cref="AudioPlayer"/> instance has been disposed.</exception>
        public void StopPlayback()
        {
            if (this.disposed)
                throw new ObjectDisposedException(nameof(AudioPlayer));

            if (this.Playing)
            {
                this.Stop();
                this.pcm8Callback = null;
                this.pcm16Callback = null;
                this.float32Callback = null;
            }
        }

        public uint WriteData(ReadOnlySpan<float> data)
        {
            var format = this.Format.SampleFormat;

            if (format == SampleFormat.IeeeFloat32)
            {
                return this.WriteDataInternal(MemoryMarshal.Cast<float, byte>(data)) / 4u;
            }
            else if (format == SampleFormat.SignedPcm16)
            {
                int minBufferSize = data.Length * 2;
                if (this.conversionBuffer == null || this.conversionBuffer.Length < minBufferSize)
                    Array.Resize(ref this.conversionBuffer, minBufferSize);

                var tempSpan = MemoryMarshal.Cast<byte, short>(this.conversionBuffer.AsSpan(0, minBufferSize));
                SampleConverter.FloatToPcm16(data, tempSpan);
                return this.WriteDataInternal(this.conversionBuffer.AsSpan(0, tempSpan.Length * 2)) / 2u;
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        public uint WriteData(ReadOnlySpan<short> data)
        {
            var format = this.Format.SampleFormat;

            if (format == SampleFormat.SignedPcm16)
            {
                return this.WriteDataInternal(MemoryMarshal.Cast<short, byte>(data)) / 2u;
            }
            else if (format == SampleFormat.IeeeFloat32)
            {
                int minBufferSize = data.Length * 4;
                if (this.conversionBuffer == null || this.conversionBuffer.Length < minBufferSize)
                    Array.Resize(ref this.conversionBuffer, minBufferSize);

                var tempSpan = MemoryMarshal.Cast<byte, float>(this.conversionBuffer.AsSpan(0, minBufferSize));
                SampleConverter.Pcm16ToFloat(data, tempSpan);
                return this.WriteDataInternal(this.conversionBuffer.AsSpan(0, tempSpan.Length * 4)) / 4u;
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        public uint WriteData(ReadOnlySpan<byte> data)
        {
            var format = this.Format.SampleFormat;

            if (format == SampleFormat.UnsignedPcm8)
            {
                return this.WriteDataInternal(data);
            }
            else if (format == SampleFormat.SignedPcm16)
            {
                int minBufferSize = data.Length * 2;
                if (this.conversionBuffer == null || this.conversionBuffer.Length < minBufferSize)
                    Array.Resize(ref this.conversionBuffer, minBufferSize);

                var tempSpan = MemoryMarshal.Cast<byte, short>(this.conversionBuffer.AsSpan(0, minBufferSize));
                SampleConverter.Pcm8ToPcm16(data, tempSpan);
                return this.WriteDataInternal(this.conversionBuffer.AsSpan(0, tempSpan.Length * 2)) / 2u;
            }
            else if (format == SampleFormat.IeeeFloat32)
            {
                int minBufferSize = data.Length * 4;
                if (this.conversionBuffer == null || this.conversionBuffer.Length < minBufferSize)
                    Array.Resize(ref this.conversionBuffer, minBufferSize);

                var tempSpan = MemoryMarshal.Cast<byte, float>(this.conversionBuffer.AsSpan(0, minBufferSize));
                SampleConverter.Pcm8ToFloat(data, tempSpan);
                return this.WriteDataInternal(this.conversionBuffer.AsSpan(0, tempSpan.Length * 4)) / 4u;
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            this.disposed = true;
        }
        protected abstract void Start(bool useCallback);
        protected abstract void Stop();

        protected void RaiseCallback(Span<byte> buffer, out uint samplesWritten)
        {
            if (this.pcm8Callback != null)
            {
                this.pcm8Callback(buffer, out samplesWritten);
                return;
            }

            throw new NotSupportedException("There is no automatic conversion to 8-bit PCM.");
        }
        protected void RaiseCallback(Span<short> buffer, out uint samplesWritten)
        {
            if (this.pcm16Callback != null)
            {
                this.pcm16Callback(buffer, out samplesWritten);
                return;
            }

            if (this.float32Callback != null)
            {
                int minBufferSize = buffer.Length * 4;
                if (this.conversionBuffer == null || this.conversionBuffer.Length < minBufferSize)
                    Array.Resize(ref this.conversionBuffer, minBufferSize);

                var tempSpan = MemoryMarshal.Cast<byte, float>(this.conversionBuffer.AsSpan(0, minBufferSize));
                this.float32Callback(tempSpan, out samplesWritten);
                SampleConverter.FloatToPcm16(tempSpan.Slice(0, (int)samplesWritten), buffer);
                return;
            }

            if (this.pcm8Callback != null)
            {
                int minBufferSize = buffer.Length;
                if (this.conversionBuffer == null || this.conversionBuffer.Length < minBufferSize)
                    Array.Resize(ref this.conversionBuffer, minBufferSize);

                var tempSpan = this.conversionBuffer.AsSpan(0, minBufferSize);
                this.pcm8Callback(tempSpan, out samplesWritten);
                SampleConverter.Pcm8ToPcm16(tempSpan.Slice(0, (int)samplesWritten), buffer);
                return;
            }

            throw new NotSupportedException();
        }
        protected void RaiseCallback(Span<float> buffer, out uint samplesWritten)
        {
            if (this.float32Callback != null)
            {
                this.float32Callback(buffer, out samplesWritten);
                return;
            }

            if (this.pcm16Callback != null)
            {
                int minBufferSize = buffer.Length * 2;
                if (this.conversionBuffer == null || this.conversionBuffer.Length < minBufferSize)
                    Array.Resize(ref this.conversionBuffer, minBufferSize);

                var tempSpan = MemoryMarshal.Cast<byte, short>(this.conversionBuffer.AsSpan(0, minBufferSize));
                this.pcm16Callback(tempSpan, out samplesWritten);
                SampleConverter.Pcm16ToFloat(tempSpan.Slice(0, (int)samplesWritten), buffer);
                return;
            }

            if (this.pcm8Callback != null)
            {
                int minBufferSize = buffer.Length;
                if (this.conversionBuffer == null || this.conversionBuffer.Length < minBufferSize)
                    Array.Resize(ref this.conversionBuffer, minBufferSize);

                var tempSpan = this.conversionBuffer.AsSpan(0, minBufferSize);
                this.pcm8Callback(tempSpan, out samplesWritten);
                SampleConverter.Pcm8ToFloat(tempSpan.Slice(0, (int)samplesWritten), buffer);
                return;
            }

            throw new NotSupportedException();
        }
        protected abstract uint WriteDataInternal(ReadOnlySpan<byte> data);
    }

    /// <summary>
    /// Invoked when an audio buffer needs to be filled for playback.
    /// </summary>
    /// <param name="buffer">Buffer to write to.</param>
    /// <param name="samplesWritten">Must be set to the number of samples written to the buffer.</param>
    public delegate void BufferNeededCallback<TSample>(Span<TSample> buffer, out uint samplesWritten) where TSample : unmanaged;
}
