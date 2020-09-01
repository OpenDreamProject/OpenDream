using OpenDreamClient.Resources.ResourceTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenDreamClient.Audio.NAudio {
    class NAudioSoundEngine : IDreamSoundEngine {
        private NAudioSoundEngineData[] _channels = new NAudioSoundEngineData[1024];

        public ISoundEngineData CreateSoundEngineData(ResourceSound sound) {
            return new NAudioSoundEngineData(sound.Data);
        }

        public void PlaySound(int channel, ResourceSound sound) {
            NAudioSoundEngineData nAudioData = sound.SoundEngineData as NAudioSoundEngineData;

            if (nAudioData != null) {
                if (channel == 0) { //First available channel
                    for (int i = 0; i < _channels.Length; i++) {
                        if (_channels[i] == null) {
                            channel = i;
                            break;
                        }
                    }

                    if (channel == 0) {
                        return;
                    }
                }

                StopChannel(channel);

                _channels[channel - 1] = nAudioData;
                nAudioData.WaveReader.Seek(0, System.IO.SeekOrigin.Begin);
                nAudioData.OutputDevice.Play();
            }
        }

        public void StopChannel(int channel) {
            if (_channels[channel - 1] != null) {
                _channels[channel - 1].OutputDevice.Stop();
                _channels[channel - 1] = null;
            }
        }

        public void StopAllChannels() {
            for (int i = 0; i < _channels.Length; i++) {
                StopChannel(i + 1);
            }
        }
    }
}
