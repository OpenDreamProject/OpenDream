using NAudio.Vorbis;
using OpenDreamClient.Resources.ResourceTypes;
using OpenDreamShared.Net.Packets;

namespace OpenDreamClient.Audio {
    class DreamSoundEngine {
        private VorbisWaveReader[] _channels = new VorbisWaveReader[1024];

        public void PlaySound(int channel, ResourceSound sound) {
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

            _channels[channel - 1] = sound.Play();
        }

        public void StopChannel(int channel) {
            if (_channels[channel - 1] != null) {
                _channels[channel - 1].Seek(0, System.IO.SeekOrigin.End);
                _channels[channel - 1] = null;
            }
        }

        public void StopAllChannels() {
            for (int i = 0; i < _channels.Length; i++) {
                StopChannel(i + 1);
            }
        }

        public void HandlePacketSound(PacketSound pSound) {
            if (pSound.File != null) {
                Program.OpenDream.ResourceManager.LoadResourceAsync<ResourceSound>(pSound.File, (ResourceSound sound) => {
                    Program.OpenDream.SoundEngine.PlaySound(pSound.Channel, sound);
                });
            } else {
                Program.OpenDream.SoundEngine.StopChannel(pSound.Channel);
            }
        }
    }
}
