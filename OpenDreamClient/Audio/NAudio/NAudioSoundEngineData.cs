using NAudio.Vorbis;
using NAudio.Wave;
using System.IO;

namespace OpenDreamClient.Audio.NAudio {
    class NAudioSoundEngineData : ISoundEngineData {
        public WaveOutEvent OutputDevice { get; private set; } = null;
        public VorbisWaveReader WaveReader { get; private set; } = null;

        public NAudioSoundEngineData(byte[] oggData) {
            OutputDevice = new WaveOutEvent();
            WaveReader = new VorbisWaveReader(new MemoryStream(oggData));

            OutputDevice.Init(WaveReader);
        }

        ~NAudioSoundEngineData() {
            OutputDevice.Stop();
            OutputDevice.Dispose();
            WaveReader.Dispose();
        }
    }
}
