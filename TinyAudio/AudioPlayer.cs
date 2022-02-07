using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace TinyAudio
{
    /// <summary>
    /// Implements a background audio playback stream.
    /// </summary>
    public abstract class AudioPlayer : IDisposable
    {
        private readonly InternalBufferWriter writer;
        private CallbackRaiser? callbackRaiser;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioPlayer"/> class.
        /// </summary>
        /// <param name="format">Format of the audio stream.</param>
        protected AudioPlayer(AudioFormat format)
        {
            this.Format = format ?? throw new ArgumentNullException(nameof(format));

            this.writer = format.SampleFormat switch
            {
                SampleFormat.UnsignedPcm8 => new InternalBufferWriter<byte>(this),
                SampleFormat.SignedPcm16 => new InternalBufferWriter<short>(this),
                SampleFormat.IeeeFloat32 => new InternalBufferWriter<float>(this),
                _ => throw new ArgumentException("Invalid sample format.")
            };
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
        public void BeginPlayback(BufferNeededCallback<short> callback) => this.BeginPlaybackInternal(callback);
        /// <summary>
        /// Begins playback of the background stream of 32-bit IEEE floating point data.
        /// </summary>
        /// <param name="callback">Delegate invoked when more data is needed for the playback buffer.</param>
        /// <exception cref="ArgumentNullException"><paramref name="callback"/> is null.</exception>
        /// <exception cref="InvalidOperationException">The stream is already playing.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="AudioPlayer"/> instance has been disposed.</exception>
        public void BeginPlayback(BufferNeededCallback<float> callback) => this.BeginPlaybackInternal(callback);
        /// <summary>
        /// Begins playback of the background stream of 8-bit PCM data.
        /// </summary>
        /// <param name="callback">Delegate invoked when more data is needed for the playback buffer.</param>
        /// <exception cref="ArgumentNullException"><paramref name="callback"/> is null.</exception>
        /// <exception cref="InvalidOperationException">The stream is already playing.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="AudioPlayer"/> instance has been disposed.</exception>
        public void BeginPlayback(BufferNeededCallback<byte> callback) => this.BeginPlaybackInternal(callback);
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

            this.callbackRaiser = null;
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
                this.callbackRaiser = null;
            }
        }

        /// <summary>
        /// Writes 32-bit IEEE floating point data to the output buffer.
        /// </summary>
        /// <param name="data">Buffer containing data to write.</param>
        /// <returns>Number of samples actually written to the buffer.</returns>
        public uint WriteData(ReadOnlySpan<float> data) => this.writer.WriteData(data);
        /// <summary>
        /// Writes 16-bit PCM data to the output buffer.
        /// </summary>
        /// <param name="data">Buffer containing data to write.</param>
        /// <returns>Number of samples actually written to the buffer.</returns>
        public uint WriteData(ReadOnlySpan<short> data) => this.writer.WriteData(data);
        /// <summary>
        /// Writes 8-bit PCM data to the output buffer.
        /// </summary>
        /// <param name="data">Buffer containing data to write.</param>
        /// <returns>Number of samples actually written to the buffer.</returns>
        public uint WriteData(ReadOnlySpan<byte> data) => this.writer.WriteData(data);

        /// <summary>
        /// Writes 32-bit IEEE floating point data to the output buffer and blocks until all data has been written.
        /// </summary>
        /// <param name="data">Buffer containing data to write.</param>
        /// <param name="cancellationToken">Token used to cancel asynchronous operation.</param>
        public ValueTask WriteDataAsync(ReadOnlyMemory<float> data, CancellationToken cancellationToken = default) => this.writer.WriteDataAsync(data, cancellationToken);
        /// <summary>
        /// Writes 16-bit PCM data to the output buffer and blocks until all data has been written.
        /// </summary>
        /// <param name="data">Buffer containing data to write.</param>
        /// <param name="cancellationToken">Token used to cancel asynchronous operation.</param>
        public ValueTask WriteDataAsync(ReadOnlyMemory<short> data, CancellationToken cancellationToken = default) => this.writer.WriteDataAsync(data, cancellationToken);
        /// <summary>
        /// Writes 8-bit PCM data to the output buffer and blocks until all data has been written.
        /// </summary>
        /// <param name="data">Buffer containing data to write.</param>
        /// <param name="cancellationToken">Token used to cancel asynchronous operation.</param>
        public ValueTask WriteDataAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default) => this.writer.WriteDataAsync(data, cancellationToken);

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
        protected abstract uint WriteDataInternal(ReadOnlySpan<byte> data);

        protected void RaiseCallback(Span<byte> buffer, out uint samplesWritten) => this.RaiseCallbackInternal(buffer, out samplesWritten);
        protected void RaiseCallback(Span<short> buffer, out uint samplesWritten) => this.RaiseCallbackInternal(buffer, out samplesWritten);
        protected void RaiseCallback(Span<float> buffer, out uint samplesWritten) => this.RaiseCallbackInternal(buffer, out samplesWritten);

        private void RaiseCallbackInternal<TInput>(Span<TInput> buffer, out uint samplesWritten) where TInput : unmanaged
        {
            if (this.callbackRaiser != null)
                this.callbackRaiser.RaiseCallback(MemoryMarshal.AsBytes(buffer), out samplesWritten);
            else
                samplesWritten = 0;
        }
        private void BeginPlaybackInternal<TInput>(BufferNeededCallback<TInput> callback) where TInput : unmanaged
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
            if (this.disposed)
                throw new ObjectDisposedException(nameof(AudioPlayer));
            if (this.Playing)
                throw new InvalidOperationException("Playback has already started.");

            this.callbackRaiser = this.Format.SampleFormat switch
            {
                SampleFormat.UnsignedPcm8 => new CallbackRaiser<TInput, byte>(callback),
                SampleFormat.SignedPcm16 => new CallbackRaiser<TInput, short>(callback),
                SampleFormat.IeeeFloat32 => new CallbackRaiser<TInput, float>(callback),
                _ => throw new ArgumentException("Sample format is not support.")
            };

            this.Playing = true;
            this.Start(true);
        }

        private async ValueTask WriteDataInternalAsync<T>(ReadOnlyMemory<T> data, CancellationToken cancellationToken) where T : unmanaged
        {
            int bytesWritten = 0;
            int byteLength = Unsafe.SizeOf<T>() * data.Length;

            while (true)
            {
                bytesWritten += (int)this.WriteDataInternal(MemoryMarshal.Cast<T, byte>(data.Span)[bytesWritten..]);
                if (bytesWritten >= byteLength)
                    return;

                await Task.Delay(5, cancellationToken).ConfigureAwait(false);
            }
        }

        private abstract class InternalBufferWriter
        {
            protected InternalBufferWriter()
            {
            }

            public abstract uint WriteData<TInput>(ReadOnlySpan<TInput> data) where TInput : unmanaged;
            public abstract ValueTask WriteDataAsync<TInput>(ReadOnlyMemory<TInput> data, CancellationToken cancellationToken) where TInput : unmanaged;
        }

        private sealed class InternalBufferWriter<TOutput> : InternalBufferWriter
            where TOutput : unmanaged
        {
            private readonly AudioPlayer player;
            private TOutput[]? conversionBuffer;

            public InternalBufferWriter(AudioPlayer player) => this.player = player;

            [MethodImpl(MethodImplOptions.AggressiveOptimization)]
            public override uint WriteData<TInput>(ReadOnlySpan<TInput> data)
            {
                // if formats are the same no sample conversion is needed
                if (typeof(TInput) == typeof(TOutput))
                    return this.player.WriteDataInternal(MemoryMarshal.AsBytes(data));

                int minBufferSize = data.Length;
                if (this.conversionBuffer == null || this.conversionBuffer.Length < minBufferSize)
                    Array.Resize(ref this.conversionBuffer, minBufferSize);

                SampleConverter.InternalConvert<TInput, TOutput>(data, this.conversionBuffer);
                return this.player.WriteDataInternal(MemoryMarshal.AsBytes(this.conversionBuffer.AsSpan(0, data.Length))) / (uint)Unsafe.SizeOf<TOutput>();
            }
            [MethodImpl(MethodImplOptions.AggressiveOptimization)]
            public override ValueTask WriteDataAsync<TInput>(ReadOnlyMemory<TInput> data, CancellationToken cancellationToken)
            {
                // if formats are the same no sample conversion is needed
                if (typeof(TInput) == typeof(TOutput))
                    return this.player.WriteDataInternalAsync(data, cancellationToken);

                int minBufferSize = data.Length;
                if (this.conversionBuffer == null || this.conversionBuffer.Length < minBufferSize)
                    Array.Resize(ref this.conversionBuffer, minBufferSize);

                SampleConverter.InternalConvert<TInput, TOutput>(data.Span, this.conversionBuffer);
                return this.player.WriteDataInternalAsync<TOutput>(this.conversionBuffer.AsMemory(0, data.Length), cancellationToken);
            }
        }

        private abstract class CallbackRaiser
        {
            protected CallbackRaiser()
            {
            }

            public abstract void RaiseCallback(Span<byte> buffer, out uint samplesWritten);
        }

        private sealed class CallbackRaiser<TInput, TOutput> : CallbackRaiser
            where TInput : unmanaged
            where TOutput : unmanaged
        {
            private readonly BufferNeededCallback<TInput> callback;
            private TInput[]? conversionBuffer;

            public CallbackRaiser(BufferNeededCallback<TInput> callback)
            {
                this.callback = callback;
            }

            [MethodImpl(MethodImplOptions.AggressiveOptimization)]
            public override void RaiseCallback(Span<byte> buffer, out uint samplesWritten)
            {
                // if formats are the same no sample conversion is needed
                if (typeof(TInput) == typeof(TOutput))
                {
                    this.callback(MemoryMarshal.Cast<byte, TInput>(buffer), out samplesWritten);
                }
                else
                {
                    int minBufferSize = buffer.Length / Unsafe.SizeOf<TOutput>();
                    if (this.conversionBuffer == null || this.conversionBuffer.Length < minBufferSize)
                        Array.Resize(ref this.conversionBuffer, minBufferSize);

                    this.callback(conversionBuffer.AsSpan(0, minBufferSize), out samplesWritten);
                    SampleConverter.InternalConvert<TInput, TOutput>(this.conversionBuffer.AsSpan(0, minBufferSize), MemoryMarshal.Cast<byte, TOutput>(buffer));
                }
            }
        }
    }

    /// <summary>
    /// Invoked when an audio buffer needs to be filled for playback.
    /// </summary>
    /// <param name="buffer">Buffer to write to.</param>
    /// <param name="samplesWritten">Must be set to the number of samples written to the buffer.</param>
    public delegate void BufferNeededCallback<TSample>(Span<TSample> buffer, out uint samplesWritten) where TSample : unmanaged;
}
