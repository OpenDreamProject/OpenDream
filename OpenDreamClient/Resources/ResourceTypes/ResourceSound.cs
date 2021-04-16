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

        public VorbisWaveReader Play() {
            VorbisWaveReader waveReader = new VorbisWaveReader(new MemoryStream(Data));

            _mixer.AddMixerInput((IWaveProvider)waveReader);
            _outputDevice.Play();

            return waveReader;
        }
    }
}
