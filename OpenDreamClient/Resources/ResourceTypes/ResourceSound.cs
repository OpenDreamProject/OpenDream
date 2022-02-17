using System.IO;
using Robust.Client.Audio;
using Robust.Client.Graphics;
using Robust.Shared.Audio;

namespace OpenDreamClient.Resources.ResourceTypes
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class ResourceSound : DreamResource {
        private readonly AudioStream _stream;

        public ResourceSound(string resourcePath, byte[] data) : base(resourcePath, data) {
            if (resourcePath.EndsWith(".ogg"))
                _stream = IoCManager.Resolve<IClydeAudio>().LoadAudioOggVorbis(new MemoryStream(data), resourcePath);
            else if (resourcePath.EndsWith(".wav"))
                _stream = IoCManager.Resolve<IClydeAudio>().LoadAudioWav(new MemoryStream(data), resourcePath);
            else
                Logger.Fatal("Only *.ogg and *.wav audio files are supported.");
        }

        public IClydeAudioSource Play(AudioParams audioParams)
        {
            var source = IoCManager.Resolve<IClydeAudio>().CreateAudioSource(_stream);

            // TODO: Positional audio.
            source.SetGlobal();
            source.SetPitch(audioParams.PitchScale);
            source.SetVolume(audioParams.Volume);
            source.SetPlaybackPosition(audioParams.PlayOffsetSeconds);
            source.IsLooping = audioParams.Loop;

            source.StartPlaying();
            return source;
        }
    }
}
