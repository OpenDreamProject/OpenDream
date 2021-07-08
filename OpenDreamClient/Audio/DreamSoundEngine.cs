using JetBrains.Annotations;
using OpenDreamClient.Resources;
using OpenDreamClient.Resources.ResourceTypes;
using OpenDreamShared.Net.Packets;
using Robust.Shared.Audio;
using Robust.Shared.IoC;

namespace OpenDreamClient.Audio
{
    [UsedImplicitly]
    internal class DreamSoundEngine
    {
        [Dependency] private readonly DreamResourceManager _resourceManager = default!;

        private readonly DreamSoundChannel[] _channels = new DreamSoundChannel[1024];

        public DreamSoundEngine(OpenDream openDream)
        {
            openDream.DisconnectedFromServer += OpenDream_DisconnectedFromServer;
        }

        private void OpenDream_DisconnectedFromServer()
        {
            StopAllChannels();
        }

        public void PlaySound(int channel, ResourceSound sound, float volume)
        {
            if (channel == 0)
            {
                //First available channel
                for (int i = 0; i < _channels.Length; i++)
                {
                    if (_channels[i] == null || !_channels[i].Source.IsPlaying)
                    {
                        _channels[i]?.Dispose();
                        channel = i;
                        break;
                    }
                }

                if (channel == 0)
                    return;
            }

            StopChannel(channel);

            var source = sound.Play(AudioParams.Default.WithVolume(volume));
            _channels[channel - 1] = new DreamSoundChannel(source);
        }

        public void StopChannel(int channel)
        {
            if (_channels[channel - 1] != null)
            {
                var ch = _channels[channel - 1];
                ch.Dispose();
                _channels[channel - 1] = null;
            }
        }

        public void StopAllChannels()
        {
            for (int i = 0; i < _channels.Length; i++) {
                StopChannel(i + 1);
            }
        }

        public void HandlePacketSound(PacketSound pSound)
        {
            if (pSound.File != null)
            {
                _resourceManager.LoadResourceAsync<ResourceSound>(pSound.File, (ResourceSound sound) =>
                {
                    PlaySound(pSound.Channel, sound, pSound.Volume / 100.0f);
                });
            }
            else
            {
                StopChannel(pSound.Channel);
            }
        }
    }
}
