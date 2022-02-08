﻿namespace TinyAudio
{
    /// <summary>
    /// Specifies an audio playback format.
    /// </summary>
    public sealed record AudioFormat(int SampleRate, int Channels, SampleFormat SampleFormat)
    {
        /// <summary>
        /// Gets the number of bytes per sample of the format.
        /// </summary>
        public int BytesPerSample => this.SampleFormat switch
        {
            SampleFormat.UnsignedPcm8 => 1,
            SampleFormat.SignedPcm16 => 2,
            SampleFormat.IeeeFloat32 => 4,
            _ => 0
        };
        /// <summary>
        /// Gets the number of bytes per frame of the format.
        /// </summary>
        public int BytesPerFrame => this.BytesPerSample * this.Channels;
    }
}
