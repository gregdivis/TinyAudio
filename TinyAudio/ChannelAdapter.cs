namespace TinyAudio;

/// <summary>
/// Contains conversion methods for changing the number of channels in audio data.
/// </summary>
public static class ChannelAdapter
{
    /// <summary>
    /// Converts a chunk of stereo data to mono data.
    /// </summary>
    /// <typeparam name="TSample">Sample format.</typeparam>
    /// <param name="source">Source sample data.</param>
    /// <param name="target">Target buffer for output sample data.</param>
    /// <exception cref="ArgumentException"><paramref name="target"/> is too small.</exception>
    public static void MonoToStereo<TSample>(ReadOnlySpan<TSample> source, Span<TSample> target)
    {
        if (target.Length < source.Length * 2)
            throw new ArgumentException("Invalid target length.");

        for (int i = 0; i < source.Length; i++)
        {
            int targetIndex = i * 2;
            target[targetIndex] = source[i];
            target[targetIndex + 1] = source[i];
        }
    }
}
