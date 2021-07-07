using System;
using System.IO;
using Robust.Client.Audio;
using Robust.Client.Graphics;
using Robust.Shared.Audio;
using Robust.Shared.IoC;
using Robust.Shared.Player;

namespace OpenDreamClient.Resources.ResourceTypes {
    class ResourceSound : Resource
    {
        private readonly AudioStream _stream;

        public ResourceSound(string resourcePath, byte[] data) : base(resourcePath, data) {
            if (!resourcePath.EndsWith(".ogg")) {
                throw new Exception("Only *.ogg audio files are supported");
            }

            _stream = IoCManager.Resolve<IClydeAudio>().LoadAudioOggVorbis(new MemoryStream(data), resourcePath);
        }

        public IPlayingAudioStream Play(AudioParams parameters)
        {
            return SoundSystem.Play(Filter.Local(), ResourcePath, parameters);
        }

        public void Stop(IPlayingAudioStream playingStream) {
            playingStream.Stop();
        }
    }
}
