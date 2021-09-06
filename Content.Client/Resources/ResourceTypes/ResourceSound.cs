using System;
using System.IO;
using Robust.Client.Audio;
using Robust.Client.Graphics;
using Robust.Shared.Audio;
using Robust.Shared.IoC;

namespace Content.Client.Resources.ResourceTypes {
    public class ResourceSound : DreamResource {
        private readonly AudioStream _stream;

        public ResourceSound(string resourcePath, byte[] data) : base(resourcePath, data) {
            if (!resourcePath.EndsWith(".ogg")) {
                throw new Exception("Only *.ogg audio files are supported");
            }

            _stream = IoCManager.Resolve<IClydeAudio>().LoadAudioOggVorbis(new MemoryStream(data), resourcePath);
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
