using OpenDreamClient.Resources;
using OpenDreamClient.Resources.ResourceTypes;
using OpenDreamShared.Network.Messages;
using Robust.Client.Graphics;
using Robust.Shared.Audio;
using Robust.Shared.Network;

namespace OpenDreamClient.Audio {
    public sealed class DreamSoundEngine : IDreamSoundEngine {
        private const int SoundChannelLimit = 1024;

        [Dependency] private readonly IDreamResourceManager _resourceManager = default!;
        [Dependency] private readonly ILogManager _logManager = default!;
        [Dependency] private readonly INetManager _netManager = default!;

        private ISawmill _sawmill = default!;

        private readonly DreamSoundChannel?[] _channels = new DreamSoundChannel[SoundChannelLimit];

        public void Initialize() {
            _sawmill = _logManager.GetSawmill("opendream.audio");

            _netManager.RegisterNetMessage<MsgSound>(RxSound);

            _netManager.Disconnect += DisconnectedFromServer;
        }

        public void StopFinishedChannels() {
            for (int i = 0; i < SoundChannelLimit; i++) {
                if (_channels[i]?.Source.IsPlaying is false or null)
                    StopChannel(i + 1);
            }
        }

        public void PlaySound(int channel, MsgSound.FormatType format, ResourceSound sound, float volume) {
            if (channel == 0) {
                //First available channel
                for (int i = 0; i < _channels.Length; i++) {
                    if (_channels[i] == null) {
                        channel = i + 1;
                        break;
                    }
                }

                if (channel == 0) {
                    _sawmill.Error("Failed to find a free audio channel to play a sound on");
                    return;
                }
            }

            StopChannel(channel);

            // convert from DM volume (0-100) to OpenAL volume (db)
            IClydeAudioSource? source = sound.Play(format, AudioParams.Default.WithVolume(20 * MathF.Log10(volume)));
            if (source == null)
                return;

            _channels[channel - 1] = new DreamSoundChannel(source);
        }


        public void StopChannel(int channel) {
            ref DreamSoundChannel? ch = ref _channels[channel - 1];

            ch?.Dispose();
            // This will null the corresponding index in the array.
            ch = null;
        }

        public void StopAllChannels() {
            for (int i = 0; i < SoundChannelLimit; i++) {
                StopChannel(i + 1);
            }
        }

        private void RxSound(MsgSound msg) {
            if (msg.ResourceId.HasValue) {
                _resourceManager.LoadResourceAsync<ResourceSound>(msg.ResourceId.Value,
                    sound => PlaySound(msg.Channel, msg.Format!.Value, sound, msg.Volume / 100.0f));
            } else {
                StopChannel(msg.Channel);
            }
        }

        private void DisconnectedFromServer(object? sender, NetDisconnectedArgs e) {
            StopAllChannels();
        }
    }
}
