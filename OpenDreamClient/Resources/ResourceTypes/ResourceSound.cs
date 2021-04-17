using NAudio.Vorbis;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.IO;

namespace OpenDreamClient.Resources.ResourceTypes {
    class ResourceSound : Resource {
        private WaveOutEvent _outputDevice = new WaveOutEvent();
        private MixingSampleProvider _mixer = null;

        public ResourceSound(string resourcePath, byte[] data) : base(resourcePath, data) {
            if (!resourcePath.EndsWith(".ogg")) {
                throw new Exception("Only *.ogg audio files are supported");
            }

            _mixer = new MixingSampleProvider(new VorbisSampleProvider(new MemoryStream(data)).WaveFormat);
            _outputDevice.Init(_mixer);
        }

        ~ResourceSound() {
            _outputDevice.Stop();
            _outputDevice.Dispose();
        }

        public ISampleProvider Play(float volume) {
            ISampleProvider sampleProvider = new VorbisSampleProvider(new MemoryStream(Data));

            if (volume != 1.0f) {
                VolumeSampleProvider volumeProvider = new VolumeSampleProvider(sampleProvider);
                volumeProvider.Volume = volume;

                sampleProvider = volumeProvider;
            }

            _mixer.AddMixerInput(sampleProvider);
            _outputDevice.Play();

            return sampleProvider;
        }

        public void Stop(ISampleProvider sampleProvider) {
            _mixer.RemoveMixerInput(sampleProvider);
        }
    }
}
