using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using TinyAudio.Wasapi.Interop;

namespace TinyAudio.Wasapi;

[SupportedOSPlatform("windows")]
internal sealed class AudioClient : IDisposable
{
    private static readonly Guid SessionGuid = Guid.NewGuid();
    private const ushort WAVE_FORMAT_PCM = 1;
    private const ushort WAVE_FORMAT_EXTENSIBLE = 0xfffe;
    private const uint AUDCLNT_STREAMFLAGS_EVENTCALLBACK = 0x00040000;
    private const uint AUDCLNT_STREAMFLAGS_AUTOCONVERTPCM = 0x80000000;
    private readonly unsafe AudioClientInst* inst;
    private unsafe AudioRenderClientInst* renderInst;
    private bool disposed;

    public unsafe AudioClient(AudioClientInst* inst)
    {
        this.inst = inst;

        WAVEFORMATEX* wfx = null;
        try
        {
            uint res = inst->Vtbl->GetMixFormat(inst, &wfx);
            if (res != 0)
                throw new InvalidOperationException();

            this.FrameSize = wfx->nBlockAlign;
            this.SampleSize = wfx->wBitsPerSample / 8u;
            this.MixFormat = GetAudioFormat(wfx) ?? throw new NotSupportedException("Mix format not supported");
        }
        finally
        {
            if (wfx != null)
                Marshal.FreeCoTaskMem(new IntPtr(wfx));
        }
    }
    ~AudioClient()
    {
        this.Dispose(false);
    }

    public AudioFormat MixFormat { get; }
    public uint FrameSize { get; }
    public uint SampleSize { get; }

    public bool IsFormatSupported(AudioFormat format, out AudioFormat? closestMatch)
    {
        ArgumentNullException.ThrowIfNull(format);
        ObjectDisposedException.ThrowIf(this.disposed, this);
        return this.IsFormatSupported(format, out closestMatch, out _);
    }

    public void Initialize(TimeSpan bufferDuration, AudioFormat? audioFormat = null, bool useCallback = false)
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);

        unsafe
        {
            uint flags = 0;

            var mixFormat = this.MixFormat;
            if (audioFormat is null || audioFormat.SampleRate == mixFormat.SampleRate)
                audioFormat = mixFormat;
            else
                audioFormat = this.MixFormat with { SampleRate = audioFormat.SampleRate };

            if (!this.IsFormatSupported(audioFormat, out var actualFormat, out var wfx))
            {
                flags |= AUDCLNT_STREAMFLAGS_AUTOCONVERTPCM;
                if (!TryGetWaveFormat(audioFormat, out wfx))
                    throw new NotSupportedException("Format not supported.");
            }

            if (useCallback)
                flags |= AUDCLNT_STREAMFLAGS_EVENTCALLBACK;

            var sessionId = SessionGuid;
            uint res = this.inst->Vtbl->Initialize(this.inst, 0, flags, bufferDuration.Ticks, 0, (WAVEFORMATEX*)&wfx, &sessionId);
            if (res != 0)
                throw new InvalidOperationException();

            var renderGuid = Guids.IID_IAudioRenderClient;
            void* service = null;
            res = this.inst->Vtbl->GetService(this.inst, &renderGuid, &service);
            if (res != 0)
                throw new InvalidOperationException();

            this.renderInst = (AudioRenderClientInst*)service;
        }
    }

    public void SetEventHandle(SafeHandle handle)
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);

        unsafe
        {
            uint res = this.inst->Vtbl->SetEventHandle(this.inst, handle?.DangerousGetHandle() ?? default);
            if (res != 0)
                throw new InvalidOperationException();
        }
    }

    public void Start()
    {
        unsafe
        {
            uint res = this.inst->Vtbl->Start(this.inst);
            if (res != 0)
                throw new InvalidOperationException();
        }
    }
    public void Stop()
    {
        unsafe
        {
            uint res = this.inst->Vtbl->Stop(this.inst);
            if (res != 0)
                throw new InvalidOperationException();
        }
    }

    public uint GetBufferSize()
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);

        unsafe
        {
            uint size = 0;
            this.inst->Vtbl->GetBufferSize(this.inst, &size);
            return size;
        }
    }
    public uint GetCurrentPadding()
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);

        unsafe
        {
            uint padding = 0;
            this.inst->Vtbl->GetCurrentPadding(this.inst, &padding);
            return padding;
        }
    }

    public bool TryGetBuffer<TSample>(uint framesRequested, out Span<TSample> buffer) where TSample : unmanaged
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);

        unsafe
        {
            byte* ptr = null;
            uint res = this.renderInst->Vtbl->GetBuffer(this.renderInst, framesRequested, &ptr);
            if (res != 0)
            {
                buffer = default;
                return false;
            }

            buffer = new(ptr, (int)(framesRequested * this.MixFormat.BytesPerFrame / sizeof(TSample)));
            return true;
        }
    }
    public unsafe void* GetBuffer(uint framesRequested)
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);

        byte* ptr = null;
        uint res = this.renderInst->Vtbl->GetBuffer(this.renderInst, framesRequested, &ptr);
        if (res != 0)
            throw new InvalidOperationException();

        return ptr;
    }
    public void ReleaseBuffer(uint framesWritten)
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);

        unsafe
        {
            uint res = this.renderInst->Vtbl->ReleaseBuffer(this.renderInst, framesWritten, 0);
            if (res != 0)
                throw new InvalidOperationException();
        }
    }

    private bool IsFormatSupported(AudioFormat format, out AudioFormat? closestMatch, out WAVEFORMATEXTENSIBLE matchWfx)
    {
        unsafe
        {
            closestMatch = null;
            matchWfx = default;
            matchWfx.WaveFormatEx.cbSize = (ushort)sizeof(WAVEFORMATEXTENSIBLE);

            if (!TryGetWaveFormat(format, out matchWfx))
                return false;

            var wfx = matchWfx;
            WAVEFORMATEXTENSIBLE* match = null;
            try
            {
                uint res = this.inst->Vtbl->IsFormatSupported(this.inst, 0, (WAVEFORMATEX*)&wfx, (WAVEFORMATEX**)&match);

                if (match != null)
                {
                    closestMatch = GetAudioFormat(&match->WaveFormatEx);
                    matchWfx = *match;
                }

                return res == 0;
            }
            finally
            {
                if (match != null)
                    Marshal.FreeCoTaskMem(new IntPtr(match));
            }
        }
    }
    private static bool TryGetWaveFormat(AudioFormat format, out WAVEFORMATEXTENSIBLE wfx)
    {
        unsafe
        {
            if (format.SampleFormat == SampleFormat.IeeeFloat32)
            {
                wfx = new WAVEFORMATEXTENSIBLE
                {
                    WaveFormatEx = new WAVEFORMATEX
                    {
                        cbSize = (ushort)sizeof(WAVEFORMATEXTENSIBLE),
                        wBitsPerSample = 32,
                        nChannels = (ushort)format.Channels,
                        nBlockAlign = (ushort)(format.Channels * 4u),
                        nSamplesPerSec = (uint)format.SampleRate,
                        nAvgBytesPerSec = (uint)format.Channels * 4u * (uint)format.SampleRate,
                        wFormatTag = WAVE_FORMAT_EXTENSIBLE
                    },
                    wValidBitsPerSample = 32,
                    dwChannelMask = 3,
                    SubFormat = Guids.KSDATAFORMAT_SUBTYPE_IEEE_FLOAT
                };

                return true;
            }
            else if (format.SampleFormat == SampleFormat.UnsignedPcm8 || format.SampleFormat == SampleFormat.SignedPcm16)
            {
                ushort bitsPerSample = format.SampleFormat == SampleFormat.SignedPcm16 ? (ushort)16 : (ushort)8;

                wfx = new WAVEFORMATEXTENSIBLE
                {
                    WaveFormatEx = new WAVEFORMATEX
                    {
                        cbSize = (ushort)sizeof(WAVEFORMATEXTENSIBLE),
                        wBitsPerSample = bitsPerSample,
                        nChannels = (ushort)format.Channels,
                        nBlockAlign = (ushort)(format.Channels * (bitsPerSample / 8u)),
                        nSamplesPerSec = (uint)format.SampleRate,
                        nAvgBytesPerSec = (uint)format.Channels * (bitsPerSample / 8u) * (uint)format.SampleRate,
                        wFormatTag = WAVE_FORMAT_PCM
                    }
                };

                return true;
            }
            else
            {
                wfx = default;
                return false;
            }
        }
    }
    private static unsafe AudioFormat? GetAudioFormat(WAVEFORMATEX* wfx)
    {
        SampleFormat sampleFormat;
        if (wfx->wFormatTag == WAVE_FORMAT_PCM)
        {
            if (wfx->wBitsPerSample == 8)
                sampleFormat = SampleFormat.UnsignedPcm8;
            else if (wfx->wBitsPerSample == 16)
                sampleFormat = SampleFormat.SignedPcm16;
            else
                return null;
        }
        else if (wfx->wFormatTag == WAVE_FORMAT_EXTENSIBLE)
        {
            var wfx2 = (WAVEFORMATEXTENSIBLE*)wfx;
            if (wfx2->SubFormat == Guids.KSDATAFORMAT_SUBTYPE_PCM)
            {
                if (wfx->wBitsPerSample == 8)
                    sampleFormat = SampleFormat.UnsignedPcm8;
                else if (wfx->wBitsPerSample == 16)
                    sampleFormat = SampleFormat.SignedPcm16;
                else
                    return null;
            }
            else if (wfx2->SubFormat == Guids.KSDATAFORMAT_SUBTYPE_IEEE_FLOAT)
            {
                if (wfx->wBitsPerSample == 32)
                    sampleFormat = SampleFormat.IeeeFloat32;
                else
                    return null;
            }
            else
            {
                return null;
            }
        }
        else
        {
            return null;
        }

        return new((int)wfx->nSamplesPerSec, wfx->nChannels, sampleFormat);
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!this.disposed)
        {
            unsafe
            {
                this.inst->Vtbl->Release(this.inst);
            }

            this.disposed = true;
        }
    }
}
