using NAudio.Wave;
using OpenDreamClient.Resources.ResourceTypes;

namespace OpenDreamClient.Audio {
    class DreamSoundChannel {
        public ResourceSound Sound;
        public ISampleProvider SampleProvider;

        public DreamSoundChannel(ResourceSound sound, ISampleProvider sampleProvider) {
            Sound = sound;
            SampleProvider = sampleProvider;
        }

        public void Stop() {
            Sound.Stop(SampleProvider);
        }
    }
}
