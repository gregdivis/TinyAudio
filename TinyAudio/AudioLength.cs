using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinyAudio
{
    public readonly struct AudioLength
    {
        private readonly byte channels;
        private readonly byte bytesPerSample;

        public AudioLength(uint frames, uint channels, uint bytesPerSample)
        {
            this.Frames = frames;
            this.channels = (byte)channels;
            this.bytesPerSample = (byte)bytesPerSample;
        }

        public uint Frames { get; }
        public uint Channels => this.channels;
        public uint BytesPerSample => this.bytesPerSample;
        public uint Samples => this.Frames * this.Channels;
        public uint TotalBytes => this.Frames * this.BytesPerSample * this.Channels;
    }
}
