using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TinyAudio;

/// <summary>
/// Implements a background audio playback stream.
/// </summary>
public abstract partial class AudioPlayer : IDisposable
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
        ArgumentNullException.ThrowIfNull(format);
        this.Format = format;

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
    /// Creates an instance of <see cref="AudioPlayer"/> suitable for the current operating system.
    /// </summary>
    /// <param name="bufferLength">Desired playback buffer length.</param>
    /// <param name="useCallback">When true, callbacks are used to fill the buffer.</param>
    /// <param name="format">Desired audio format or <see langword="null"/> to use a default format.</param>
    /// <returns><see cref="AudioPlayer"/> instance.</returns>
    public static AudioPlayer CreateDefault(TimeSpan bufferLength, bool useCallback, AudioFormat? format = null)
    {
        if (OperatingSystem.IsWindows())
            return WasapiAudioPlayer.Create(bufferLength, useCallback, format);

        return new OpenALAudioPlayer(format ?? new AudioFormat(44100, 2, SampleFormat.SignedPcm16));
    }

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
        ObjectDisposedException.ThrowIf(this.disposed, this);
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
        ObjectDisposedException.ThrowIf(this.disposed, this);

        if (this.Playing)
        {
            this.Stop();
            this.Playing = false;
            this.callbackRaiser = null;
        }
    }

    /// <summary>
    /// Writes 32-bit IEEE floating point data to the output buffer.
    /// </summary>
    /// <param name="data">Buffer containing data to write.</param>
    /// <returns>Number of samples actually written to the buffer.</returns>
    public int WriteData(ReadOnlySpan<float> data) => this.writer.WriteData(data);
    /// <summary>
    /// Writes 16-bit PCM data to the output buffer.
    /// </summary>
    /// <param name="data">Buffer containing data to write.</param>
    /// <returns>Number of samples actually written to the buffer.</returns>
    public int WriteData(ReadOnlySpan<short> data) => this.writer.WriteData(data);
    /// <summary>
    /// Writes 8-bit PCM data to the output buffer.
    /// </summary>
    /// <param name="data">Buffer containing data to write.</param>
    /// <returns>Number of samples actually written to the buffer.</returns>
    public int WriteData(ReadOnlySpan<byte> data) => this.writer.WriteData(data);

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
    /// <summary>
    /// Writes sample data of the type <typeparamref name="TSample"/> to the output buffer and blocks until all data has been written.
    /// </summary>
    /// <typeparam name="TSample">Sample format.</typeparam>
    /// <param name="data">Buffer containing data to write.</param>
    /// <param name="cancellationToken">Token used to cancel asynchronous operation.</param>
    /// <remarks>
    /// <typeparamref name="TSample"/> can be one of the following:
    /// <list type="bullet">
    /// <item><see cref="byte"/>: 8-bit PCM</item>
    /// <item><see cref="short"/>: 16-bit PCM</item>
    /// <item><see cref="float"/>: 32-bit IEEE float</item>
    /// </list>
    /// </remarks>
    public ValueTask WriteDataRawAsync<TSample>(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default) where TSample : unmanaged => this.writer.WriteDataRawAsync<TSample>(data, cancellationToken);

    /// <inheritdoc/>
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases resources used by the <see cref="AudioPlayer"/>.
    /// </summary>
    /// <param name="disposing">Value indicating whether to release managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        this.disposed = true;
    }
    /// <summary>
    /// Begin playback on the underlying audio system.
    /// </summary>
    /// <param name="useCallback">Value indicating whether callbacks will be used to fill the buffer.</param>
    protected abstract void Start(bool useCallback);
    /// <summary>
    /// Stops playback on the underlying audio system.
    /// </summary>
    protected abstract void Stop();
    /// <summary>
    /// Writes a chunk of data to the underlying audio system for playback.
    /// </summary>
    /// <param name="data">Data to write.</param>
    /// <returns>Number of bytes actually written.</returns>
    protected abstract int WriteDataInternal(ReadOnlySpan<byte> data);

    /// <summary>
    /// Raises the user-supplied callback method to request more data.
    /// </summary>
    /// <param name="buffer">Buffer to be filled with 8-bit PCM data.</param>
    /// <param name="samplesWritten">Number of samples actually written to the buffer.</param>
    protected void RaiseCallback(Span<byte> buffer, out int samplesWritten) => this.RaiseCallbackInternal(buffer, out samplesWritten);
    /// <summary>
    /// Raises the user-supplied callback method to request more data.
    /// </summary>
    /// <param name="buffer">Buffer to be filled with 16-bit PCM data.</param>
    /// <param name="samplesWritten">Number of samples actually written to the buffer.</param>
    protected void RaiseCallback(Span<short> buffer, out int samplesWritten) => this.RaiseCallbackInternal(buffer, out samplesWritten);
    /// <summary>
    /// Raises the user-supplied callback method to request more data.
    /// </summary>
    /// <param name="buffer">Buffer to be filled with 32-bit IEEE data.</param>
    /// <param name="samplesWritten">Number of samples actually written to the buffer.</param>
    protected void RaiseCallback(Span<float> buffer, out int samplesWritten) => this.RaiseCallbackInternal(buffer, out samplesWritten);

    private void RaiseCallbackInternal<TInput>(Span<TInput> buffer, out int samplesWritten) where TInput : unmanaged
    {
        if (this.callbackRaiser != null)
            this.callbackRaiser.RaiseCallback(MemoryMarshal.AsBytes(buffer), out samplesWritten);
        else
            samplesWritten = 0;
    }
    private void BeginPlaybackInternal<TInput>(BufferNeededCallback<TInput> callback) where TInput : unmanaged
    {
        ArgumentNullException.ThrowIfNull(callback);
        ObjectDisposedException.ThrowIf(this.disposed, this);
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
            bytesWritten += this.WriteDataInternal(MemoryMarshal.Cast<T, byte>(data.Span)[bytesWritten..]);
            if (bytesWritten >= byteLength)
                return;

            await Task.Delay(5, cancellationToken).ConfigureAwait(false);
        }
    }
    private async ValueTask WriteDataRawInternalAsync<T>(ReadOnlyMemory<byte> data, CancellationToken cancellationToken) where T : unmanaged
    {
        int bytesWritten = 0;
        int byteLength = Unsafe.SizeOf<T>() * data.Length;

        while (true)
        {
            bytesWritten += this.WriteDataInternal(data.Span[bytesWritten..]);
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

        public abstract int WriteData<TInput>(ReadOnlySpan<TInput> data) where TInput : unmanaged;
        public abstract ValueTask WriteDataAsync<TInput>(ReadOnlyMemory<TInput> data, CancellationToken cancellationToken) where TInput : unmanaged;
        public abstract ValueTask WriteDataRawAsync<TInput>(ReadOnlyMemory<byte> data, CancellationToken cancellationToken) where TInput : unmanaged;
    }

    private sealed class InternalBufferWriter<TOutput>(AudioPlayer player) : InternalBufferWriter
        where TOutput : unmanaged
    {
        private readonly AudioPlayer player = player;
        private TOutput[]? conversionBuffer;

        public override int WriteData<TInput>(ReadOnlySpan<TInput> data)
        {
            // if formats are the same no sample conversion is needed
            if (typeof(TInput) == typeof(TOutput))
                return this.player.WriteDataInternal(MemoryMarshal.AsBytes(data)) / Unsafe.SizeOf<TOutput>();

            int minBufferSize = data.Length;
            if (this.conversionBuffer == null || this.conversionBuffer.Length < minBufferSize)
                Array.Resize(ref this.conversionBuffer, minBufferSize);

            SampleConverter.InternalConvert<TInput, TOutput>(data, this.conversionBuffer);
            return this.player.WriteDataInternal(MemoryMarshal.AsBytes(this.conversionBuffer.AsSpan(0, data.Length))) / Unsafe.SizeOf<TOutput>();
        }
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
        public override ValueTask WriteDataRawAsync<TInput>(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
        {
            // if formats are the same no sample conversion is needed
            if (typeof(TInput) == typeof(TOutput))
                return this.player.WriteDataRawInternalAsync<TInput>(data, cancellationToken);

            int minBufferSize = data.Length / Unsafe.SizeOf<TInput>();
            if (this.conversionBuffer == null || this.conversionBuffer.Length < minBufferSize)
                Array.Resize(ref this.conversionBuffer, minBufferSize);

            SampleConverter.InternalConvert<TInput, TOutput>(MemoryMarshal.Cast<byte, TInput>(data.Span), this.conversionBuffer);
            return this.player.WriteDataInternalAsync<TOutput>(this.conversionBuffer.AsMemory(0, minBufferSize), cancellationToken);
        }
    }

    private abstract class CallbackRaiser
    {
        protected CallbackRaiser()
        {
        }

        public abstract void RaiseCallback(Span<byte> buffer, out int samplesWritten);
    }

    private sealed class CallbackRaiser<TInput, TOutput>(BufferNeededCallback<TInput> callback) : CallbackRaiser
        where TInput : unmanaged
        where TOutput : unmanaged
    {
        private readonly BufferNeededCallback<TInput> callback = callback;
        private TInput[]? conversionBuffer;

        public override void RaiseCallback(Span<byte> buffer, out int samplesWritten)
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
public delegate void BufferNeededCallback<TSample>(Span<TSample> buffer, out int samplesWritten) where TSample : unmanaged;
