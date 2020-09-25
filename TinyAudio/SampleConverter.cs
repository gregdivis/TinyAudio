using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace TinyAudio
{
    /// <summary>
    /// Converts between various sample formats.
    /// </summary>
    public static class SampleConverter
    {
        /// <summary>
        /// Converts 16-bit PCM samples to 32-bit IEEE float samples.
        /// </summary>
        /// <param name="source">Source samples.</param>
        /// <param name="target">Target sample buffer.</param>
        public static void Pcm16ToFloat(ReadOnlySpan<short> source, Span<float> target)
        {
            if (Vector.IsHardwareAccelerated)
            {
                var srcVector = MemoryMarshal.Cast<short, Vector<short>>(source);
                var destVector = MemoryMarshal.Cast<float, Vector<float>>(target);

                for (int i = 0; i < srcVector.Length; i++)
                {
                    int targetIndex = i * 2;
                    Vector.Widen(srcVector[i], out var iSrc1, out var iSrc2);
                    destVector[targetIndex] = Vector.Multiply(Vector.ConvertToSingle(iSrc1), 1f / 32768);
                    destVector[targetIndex + 1] = Vector.Multiply(Vector.ConvertToSingle(iSrc2), 1f / 32768);
                }

                for(int i = srcVector.Length * Vector<short>.Count; i < source.Length; i++)
                    target[i] = source[i] / 32768f;
            }
            else
            {
                for (int i = 0; i < source.Length; i++)
                    target[i] = source[i] / 32768f;
            }
        }
        /// <summary>
        /// Converts 8-bit PCM samples to 32-bit IEEE float samples.
        /// </summary>
        /// <param name="source">Source samples.</param>
        /// <param name="target">Target sample buffer.</param>
        public static void Pcm8ToFloat(ReadOnlySpan<byte> source, Span<float> target)
        {
            if (Vector.IsHardwareAccelerated)
            {
                var srcVector = MemoryMarshal.Cast<byte, Vector<byte>>(source);
                var destVector = MemoryMarshal.Cast<float, Vector<float>>(target);
                var addVector = new Vector<short>(-127);

                for (int i = 0; i < srcVector.Length; i++)
                {
                    int targetIndex = i * 4;
                    Vector.Widen(srcVector[i], out var usSrc1, out var usSrc2);

                    var sSrc1 = Vector.Add(Vector.AsVectorInt16(usSrc1), addVector);
                    var sSrc2 = Vector.Add(Vector.AsVectorInt16(usSrc2), addVector);

                    Vector.Widen(sSrc1, out var iSrc1, out var iSrc2);
                    Vector.Widen(sSrc2, out var iSrc3, out var iSrc4);

                    destVector[targetIndex] = Vector.Multiply(Vector.ConvertToSingle(iSrc1), 1f / 128);
                    destVector[targetIndex + 1] = Vector.Multiply(Vector.ConvertToSingle(iSrc2), 1f / 128);
                    destVector[targetIndex + 2] = Vector.Multiply(Vector.ConvertToSingle(iSrc3), 1f / 128);
                    destVector[targetIndex + 3] = Vector.Multiply(Vector.ConvertToSingle(iSrc4), 1f / 128);
                }

                for (int i = srcVector.Length * Vector<byte>.Count; i < source.Length; i++)
                    target[i] = (source[i] - 127) / 128f;
            }
            else
            {
                for (int i = 0; i < source.Length; i++)
                    target[i] = (source[i] - 127) / 128f;
            }
        }

        /// <summary>
        /// Converts 8-bit PCM samples to 16-bit IEEE float samples.
        /// </summary>
        /// <param name="source">Source samples.</param>
        /// <param name="target">Target sample buffer.</param>
        public static void Pcm8ToPcm16(ReadOnlySpan<byte> source, Span<short> target)
        {
            if (Vector.IsHardwareAccelerated)
            {
                var srcVector = MemoryMarshal.Cast<byte, Vector<byte>>(source);
                var destVector = MemoryMarshal.Cast<short, Vector<short>>(target);
                var addVector = new Vector<short>(-127);

                for (int i = 0; i < srcVector.Length; i++)
                {
                    int targetIndex = i * 2;
                    Vector.Widen(srcVector[i], out var usSrc1, out var usSrc2);

                    var sSrc1 = Vector.Multiply(Vector.Add(Vector.AsVectorInt16(usSrc1), addVector), (short)128);
                    var sSrc2 = Vector.Multiply(Vector.Add(Vector.AsVectorInt16(usSrc2), addVector), (short)128);

                    destVector[targetIndex] = Vector.Multiply(sSrc1, (short)256);
                    destVector[targetIndex + 1] = Vector.Multiply(sSrc2, (short)256);
                }

                for (int i = srcVector.Length * Vector<byte>.Count; i < source.Length; i++)
                    target[i] = (short)((source[i] - 127) * 256);
            }
            else
            {
                for (int i = 0; i < source.Length; i++)
                    target[i] = (short)((source[i] - 127) * 256);
            }
        }
        /// <summary>
        /// Converts 32-bit IEEE float samples tp 16-bit PCM samples.
        /// </summary>
        /// <param name="source">Source samples.</param>
        /// <param name="target">Target sample buffer.</param>
        public static void FloatToPcm16(ReadOnlySpan<float> source, Span<short> target)
        {
            if (Vector.IsHardwareAccelerated)
            {
                var srcVector = MemoryMarshal.Cast<float, Vector<float>>(source);
                var destVector = MemoryMarshal.Cast<short, Vector<short>>(target);

                for (int i = 0; i < srcVector.Length - 1; i += 2)
                {
                    int targetIndex = i / 2;

                    var src1 = Vector.ConvertToInt32(Vector.Multiply(srcVector[i], 32767f));
                    var src2 = Vector.ConvertToInt32(Vector.Multiply(srcVector[i + 1], 32767f));

                    destVector[targetIndex] = Vector.Narrow(src1, src2);
                }

                for (int i = srcVector.Length * Vector<float>.Count; i < source.Length; i++)
                    target[i] = (short)(source[i] * 32767f);
            }
            else
            {
                for (int i = 0; i < source.Length; i++)
                    target[i] = (short)(source[i] * 32767f);
            }
        }
    }
}
