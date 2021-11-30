using System;
using OpenDreamClient.Resources;
using OpenDreamClient.Resources.ResourceTypes;
using OpenDreamShared.Network.Messages;
using Robust.Shared.Audio;
using Robust.Shared.IoC;
using Robust.Shared.Network;

namespace OpenDreamClient.Audio
{
    public class DreamSoundEngine : IDreamSoundEngine
    {
        private const int SoundChannelLimit = 1024;

        [Dependency] private readonly IDreamResourceManager _resourceManager = default!;
        [Dependency] private readonly INetManager _netManager = default!;

        private readonly DreamSoundChannel[] _channels = new DreamSoundChannel[SoundChannelLimit];

        public void Initialize()
        {
            _netManager.RegisterNetMessage<MsgSound>(RxSound);

            _netManager.Disconnect += DisconnectedFromServer;
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
                        channel = i + 1;
                        break;
                    }
                }

                if (channel == 0)
                    return;
            }

            StopChannel(channel);

            var source = sound.Play(AudioParams.Default.WithVolume(20 * MathF.Log10(volume)));
            _channels[channel - 1] = new DreamSoundChannel(source);
        }


        public void StopChannel(int channel)
        {
            if (_channels[channel - 1] != null)
            {
                ref var ch = ref _channels[channel - 1];
                ch.Dispose();
                // This will null the corresponding index in the array.
                ch = null;
            }
        }

        public void StopAllChannels()
        {
            for (int i = 0; i < SoundChannelLimit; i++)
            {
                StopChannel(i + 1);
            }
        }

        private void RxSound(MsgSound msg)
        {
            if (!string.IsNullOrEmpty(msg.File))
            {
                _resourceManager.LoadResourceAsync<ResourceSound>(msg.File,
                    sound => { PlaySound(msg.Channel, sound, msg.Volume / 100.0f); });
            }
            else
            {
                StopChannel(msg.Channel);
            }
        }

        private void DisconnectedFromServer(object? sender, NetDisconnectedArgs e)
        {
            StopAllChannels();
        }
    }
}
