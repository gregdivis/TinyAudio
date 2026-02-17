using System.Numerics;
using System.Runtime.Versioning;
using PipeWireSharp;

namespace TinyAudio;

/// <summary>
/// Background audio player implemented using PipeWire.
/// </summary>
[SupportedOSPlatform("linux")]
public sealed class PipeWireAudioPlayer : AudioPlayer
{
    private static PipeWireContext? pipeWireContext;
    private static readonly Lock contextLock = new();
    private readonly PipeWireStream stream;
    private CircularBuffer? miniBuffer;
    private readonly int bufferLength;

    /// <summary>
    /// Initializes a new instance of the <see cref="PipeWireAudioPlayer"/> class.
    /// </summary>
    /// <param name="format">Desired audio format.</param>
    /// <param name="bufferLength">Buffer length for non-callback mode.</param>
    public PipeWireAudioPlayer(AudioFormat format, TimeSpan bufferLength) : base(format)
    {
        ArgumentNullException.ThrowIfNull(format);

        var context = GetContext();
        this.stream = context.CreateStream(
            new PipeWireStreamOptions(
                "TinyAudioStream",
                format.SampleFormat switch
                {
                    SampleFormat.UnsignedPcm8 => SpaAudioFormat.U8,
                    SampleFormat.SignedPcm16 => BitConverter.IsLittleEndian ? SpaAudioFormat.S16_LE : SpaAudioFormat.S16_BE,
                    SampleFormat.IeeeFloat32 => BitConverter.IsLittleEndian ? SpaAudioFormat.F32_LE : SpaAudioFormat.S32_BE,
                    _ => throw new ArgumentOutOfRangeException(nameof(format))
                },
                format.SampleRate,
                format.Channels,
                this.WriteData
            )
        );

        this.bufferLength = (int)BitOperations.RoundUpToPowerOf2((uint)(bufferLength.TotalSeconds * format.BytesPerFrame * format.SampleRate));
    }

    /// <inheritdoc/>
    protected override void Start(bool useCallback)
    {
        if (useCallback)
            this.miniBuffer = null;
        else
            this.miniBuffer ??= new CircularBuffer(this.bufferLength);

        this.stream.SetActive(true);
    }

    /// <inheritdoc/>
    protected override void Stop()
    {
        this.stream.SetActive(false);
    }

    /// <inheritdoc/>
    protected override int WriteDataInternal(ReadOnlySpan<byte> data)
    {
        if (this.miniBuffer is null)
            throw new InvalidOperationException();

        return this.miniBuffer.Write(data);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            this.stream.Dispose();

        base.Dispose(disposing);
    }

    private int WriteData(Span<byte> buffer)
    {
        if (this.miniBuffer is null)
        {
            this.RaiseCallback(buffer, out int samples);
            return samples * this.Format.BytesPerSample;
        }
        else
        {
            return this.miniBuffer.Read(buffer);
        }
    }

    private static PipeWireContext GetContext()
    {
        lock (contextLock)
        {
            return pipeWireContext ??= new PipeWireContext();
        }
    }
}
