using OpenDreamClient.Resources.ResourceTypes;

namespace OpenDreamClient.Audio
{
    public interface IDreamSoundEngine
    {
        void Initialize();
        void PlaySound(int channel, ResourceSound sound, float volume);
        void StopChannel(int channel);
        void StopAllChannels();
    }
}
