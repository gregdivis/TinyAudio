using System.Runtime.InteropServices;
using TinyAudio.OpenAL;

namespace TinyAudio;

/// <summary>
/// Backround audio player implemented using OpenAL.
/// </summary>
/// <remarks>
/// OpenAL will work on any platform as long as its binaries are present.
/// </remarks>
public sealed class OpenALAudioPlayer : AudioPlayer
{
    private const int FrequencyAttribute = 4103;
    private readonly int[] buffers = new int[4];
    private readonly int source;
    private int nextBufferIndex;
    private readonly ALFormat alFormat;
    private bool disposed;
    private readonly OpenALDevice device;
    private readonly DeviceContext context;
    private CancellationTokenSource? playbackTokenSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenALAudioPlayer"/> class.
    /// </summary>
    /// <param name="format">Desired audio format.</param>
    /// <exception cref="NotSupportedException"><paramref name="format"/> is not supported.</exception>
    public OpenALAudioPlayer(AudioFormat format) : base(format)
    {
        this.alFormat = format switch
        {
            { SampleFormat: SampleFormat.UnsignedPcm8, Channels: 1 } => ALFormat.Mono8,
            { SampleFormat: SampleFormat.UnsignedPcm8, Channels: 2 } => ALFormat.Stereo8,
            { SampleFormat: SampleFormat.SignedPcm16, Channels: 1 } => ALFormat.Mono16,
            { SampleFormat: SampleFormat.SignedPcm16, Channels: 2 } => ALFormat.Stereo16,
            { SampleFormat: SampleFormat.IeeeFloat32, Channels: 1 } => ALFormat.MonoFloat32Ext,
            { SampleFormat: SampleFormat.IeeeFloat32, Channels: 2 } => ALFormat.StereoFloat32Ext,
            _ => throw new NotSupportedException("Format not supported.")
        };

        this.device = NativeMethods.alcOpenDevice(null);
        this.context = NativeMethods.alcCreateContext(this.device, [FrequencyAttribute, format.SampleRate, 0]);
        NativeMethods.alcMakeContextCurrent(this.context);
        NativeMethods.alcProcessContext(this.context);
        NativeMethods.alGenBuffers(this.buffers.Length, this.buffers);
        NativeMethods.alGenSources(1, new Span<int>(ref this.source));
        NativeMethods.Source(this.source, ALSourceb.Looping, false);
        NativeMethods.Source(this.source, ALSourcef.Gain, 1.0f);
    }

    /// <inheritdoc/>
    protected override void Start(bool useCallback)
    {
        if (useCallback)
        {
            var buffer = new byte[8192];
            this.RaiseCallbackInternal(buffer);
            this.playbackTokenSource = new CancellationTokenSource();
            Task.Run(this.PlaybackAsync);
        }

        NativeMethods.alSourcePlay(this.source);
    }
    /// <inheritdoc/>
    protected override void Stop()
    {
        this.playbackTokenSource?.Cancel();
        NativeMethods.alSourceStop(this.source);
    }
    /// <inheritdoc/>
    protected override int WriteDataInternal(ReadOnlySpan<byte> data)
    {
        NativeMethods.GetSource(this.source, OpenAL.ALGetSourcei.BuffersProcessed, out int processed);
        if (processed > 0)
            NativeMethods.alSourceUnqueueBuffers(this.source, processed, stackalloc int[processed]);

        NativeMethods.GetSource(this.source, OpenAL.ALGetSourcei.BuffersQueued, out int queued);

        if (queued < 3)  // Keep 3 ahead for safety (adjust to 2 if latency-sensitive)
        {
            int currentBuffer = this.buffers[this.nextBufferIndex];
            NativeMethods.alBufferData(currentBuffer, this.alFormat, data, data.Length, this.Format.SampleRate);
            NativeMethods.alSourceQueueBuffers(this.source, 1, [currentBuffer]);
            this.nextBufferIndex = (this.nextBufferIndex + 1) % this.buffers.Length;
            queued++;
        }
        else
        {
            return 0;
        }

        NativeMethods.GetSource(this.source, OpenAL.ALGetSourcei.SourceState, out int state);
        if (state == (int)ALSourceState.Stopped && queued > 0)
            NativeMethods.alSourcePlay(this.source);

        return data.Length;
    }
    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (!this.disposed)
        {
            if (disposing)
            {
                this.StopPlayback();
                NativeMethods.alDeleteSources(1, [this.source]);
                NativeMethods.alDeleteBuffers(this.buffers.Length, this.buffers);
                this.context.Dispose();
                this.device.Dispose();
            }

            this.disposed = true;
        }

        base.Dispose(disposing);
    }

    private async Task PlaybackAsync()
    {
        try
        {
            var buffer = new byte[8192];
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(10));
            while (await timer.WaitForNextTickAsync(this.playbackTokenSource!.Token))
            {
                this.RaiseCallbackInternal(buffer);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
    private void RaiseCallbackInternal(byte[] buffer)
    {
        NativeMethods.GetSource(this.source, OpenAL.ALGetSourcei.BuffersProcessed, out int processed);

        if (processed > 0)
            NativeMethods.alSourceUnqueueBuffers(this.source, processed, stackalloc int[processed]);

        NativeMethods.GetSource(this.source, OpenAL.ALGetSourcei.BuffersQueued, out int queued);
        while (queued < 3)
        {
            this.RaiseCallbackRaw(buffer, out int bytesWritten);
            if (bytesWritten > 0)
            {
                int currentBuffer = buffers[this.nextBufferIndex];
                NativeMethods.alBufferData(currentBuffer, this.alFormat, buffer.AsSpan(0, bytesWritten), bytesWritten, this.Format.SampleRate);
                NativeMethods.alSourceQueueBuffers(this.source, 1, [currentBuffer]);
                this.nextBufferIndex = (this.nextBufferIndex + 1) % this.buffers.Length;
                queued++;
            }
            else
            {
                break;
            }
        }
    }
    private void RaiseCallbackRaw(Span<byte> buffer, out int bytesWritten)
    {
        var format = this.Format.SampleFormat;
        if (format == SampleFormat.UnsignedPcm8)
        {
            this.RaiseCallback(buffer, out int samplesWritten);
            bytesWritten = samplesWritten;
        }
        else if (format == SampleFormat.SignedPcm16)
        {
            this.RaiseCallback(MemoryMarshal.Cast<byte, short>(buffer), out int samplesWritten);
            bytesWritten = samplesWritten * 2;
        }
        else if (format == SampleFormat.IeeeFloat32)
        {
            this.RaiseCallback(MemoryMarshal.Cast<byte, float>(buffer), out int samplesWritten);
            bytesWritten = samplesWritten * 4;
        }
        else
        {
            throw new NotSupportedException();
        }
    }
}
