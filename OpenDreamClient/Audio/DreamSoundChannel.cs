using OpenDreamClient.Resources.ResourceTypes;
using Robust.Shared.Audio;

namespace OpenDreamClient.Audio {
    class DreamSoundChannel {
        public ResourceSound Sound;
        public IPlayingAudioStream Stream;

        public DreamSoundChannel(ResourceSound sound, IPlayingAudioStream stream) {
            Sound = sound;
            Stream = stream;
        }

        public void Stop() {
            Stream.Stop();
        }
    }
}
