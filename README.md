# TinyAudio

TinyAudio is a super simple C# library for realtime audio playback. It's intended to make it really easy
to write PCM/IEEE waveform data directly to the system's default audio device. This is not a full-featured
playback library; it only provides the minimum interface needed to stream audio data from an arbitrary source.
It is cross-platform, working on Windows using WASAPI, Linux using PipeWire, and on any other platform with
OpenAL present.
