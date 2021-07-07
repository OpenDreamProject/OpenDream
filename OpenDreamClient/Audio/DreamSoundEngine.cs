using OpenDreamClient.Resources.ResourceTypes;
using OpenDreamShared.Net.Packets;
using Robust.Shared.Audio;

namespace OpenDreamClient.Audio {
    class DreamSoundEngine {
        private DreamSoundChannel[] _channels = new DreamSoundChannel[1024];

        public DreamSoundEngine(OpenDream openDream) {
            openDream.DisconnectedFromServer += OpenDream_DisconnectedFromServer;
        }

        private void OpenDream_DisconnectedFromServer() {
            StopAllChannels();
        }

        public void PlaySound(int channel, ResourceSound sound, float volume) {
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

            var stream = sound.Play(AudioParams.Default.WithVolume(volume));
            _channels[channel - 1] = new DreamSoundChannel(sound, stream);
        }

        public void StopChannel(int channel) {
            if (_channels[channel - 1] != null) {
                _channels[channel - 1].Stop();
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
                    Program.OpenDream.SoundEngine.PlaySound(pSound.Channel, sound, pSound.Volume / 100.0f);
                });
            } else {
                Program.OpenDream.SoundEngine.StopChannel(pSound.Channel);
            }
        }
    }
}
