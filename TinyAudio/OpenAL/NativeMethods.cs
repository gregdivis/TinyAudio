using System.Reflection;
using System.Runtime.InteropServices;

namespace TinyAudio.OpenAL;

internal static partial class NativeMethods
{
    private const string LibraryName = "OpenAL";
    private static IntPtr libraryHandle;

    static NativeMethods() => NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), ImportResolver);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial OpenALDevice alcOpenDevice(string? devicename);

    [LibraryImport(LibraryName)]
    public static partial DeviceContext alcCreateContext(OpenALDevice device, ReadOnlySpan<int> attributeList);

    [LibraryImport(LibraryName)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool alcMakeContextCurrent(DeviceContext context);

    [LibraryImport(LibraryName)]
    public static partial void alcProcessContext(DeviceContext context);

    [LibraryImport(LibraryName)]
    public static partial void alGenBuffers(int n, ReadOnlySpan<int> buffers);

    [LibraryImport(LibraryName)]
    public static partial void alGenSources(int n, Span<int> sources);

    [LibraryImport(LibraryName, EntryPoint = "alSourcei")]
    public static partial void Source(int sid, ALSourceb param, [MarshalAs(UnmanagedType.I1)] bool value);

    [LibraryImport(LibraryName, EntryPoint = "alSourcef")]
    public static partial void Source(int sid, ALSourcef param, float value);

    [LibraryImport(LibraryName, EntryPoint = "alGetSourcei")]
    public static partial void GetSource(int sid, ALGetSourcei param, out int value);

    [LibraryImport(LibraryName)]
    public static partial void alSourceUnqueueBuffers(int sid, int numEntries, Span<int> bids);

    [LibraryImport(LibraryName)]
    public static partial void alBufferData(int bid, ALFormat format, ReadOnlySpan<byte> buffer, int size, int freq);

    [LibraryImport(LibraryName)]
    public static partial void alSourceQueueBuffers(int sid, int numEntries, ReadOnlySpan<int> bids);

    [LibraryImport(LibraryName)]
    public static partial void alSourcePlay(int sid);

    [LibraryImport(LibraryName)]
    public static partial void alSourceStop(int sid);

    [LibraryImport(LibraryName)]
    public static partial void alDeleteSources(int n, ReadOnlySpan<int> sources);

    [LibraryImport(LibraryName)]
    public static partial void alDeleteBuffers(int n, ReadOnlySpan<int> buffers);

    [LibraryImport(LibraryName)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool alcCloseDevice(nint device);

    [LibraryImport(LibraryName)]
    public static partial void alcDestroyContext(nint context);

    private static IntPtr ImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName == LibraryName)
        {
            if (libraryHandle != 0)
                return libraryHandle;

            return libraryHandle = NativeLibrary.Load(GetLibraryName(), assembly, searchPath);
        }
        else
        {
            return NativeLibrary.Load(libraryName, assembly, searchPath);
        }

        static string GetLibraryName()
        {
            return OperatingSystem.IsWindows() ? "openal32.dll"
                : OperatingSystem.IsLinux() ? "libopenal.so.1"
                : (OperatingSystem.IsMacOS() || OperatingSystem.IsIOS()) ? "/System/Library/Frameworks/OpenAL.framework/OpenAL"
                : OperatingSystem.IsAndroid() ? "libopenal.so"
                : throw new PlatformNotSupportedException();
        }
    }
}
