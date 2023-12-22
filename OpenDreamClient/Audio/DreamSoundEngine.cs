using OpenDreamClient.Resources;
using OpenDreamClient.Resources.ResourceTypes;
using OpenDreamShared.Network.Messages;
using Robust.Client.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Network;

namespace OpenDreamClient.Audio {
    public sealed class DreamSoundEngine : IDreamSoundEngine {
        private const int SoundChannelLimit = 1024;

        [Dependency] private readonly IDreamResourceManager _resourceManager = default!;
        [Dependency] private readonly ILogManager _logManager = default!;
        [Dependency] private readonly INetManager _netManager = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly IAudioManager _audioManager = default!;
        private AudioSystem? _audioSystem;

        private ISawmill _sawmill = default!;

        private readonly DreamSoundChannel?[] _channels = new DreamSoundChannel[SoundChannelLimit];

        public void Initialize() {
            _sawmill = _logManager.GetSawmill("opendream.audio");

            _netManager.RegisterNetMessage<MsgSound>(RxSound);

            _netManager.Disconnect += DisconnectedFromServer;
        }

        public void StopFinishedChannels() {
            for (int i = 0; i < SoundChannelLimit; i++) {
                if (_channels[i]?.Source.Component.Playing is false or null)
                    StopChannel(i + 1);
            }
        }

        public void PlaySound(int channel, MsgSound.FormatType format, ResourceSound sound, float volume) {
            if (_audioSystem == null)
                _entitySystemManager.Resolve(ref _audioSystem);

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

            var stream = sound.GetStream(format, _audioManager);
            if (stream == null) {
                _sawmill.Error($"Failed to load audio ${sound}");
                return;
            }

            var db = 20 * MathF.Log10(volume); // convert from DM volume (0-100) to OpenAL volume (db)
            var source = _audioSystem.PlayGlobal(stream, AudioParams.Default.WithVolume(db)); // TODO: Positional audio.
            if (source == null) {
                _sawmill.Error($"Failed to play audio ${sound}");
                return;
            }

            _channels[channel - 1] = new DreamSoundChannel(_audioSystem, source.Value);
        }


        public void StopChannel(int channel) {
            ref DreamSoundChannel? ch = ref _channels[channel - 1];

            ch?.Stop();
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
