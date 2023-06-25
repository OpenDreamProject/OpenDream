using OpenDreamClient.Resources.ResourceTypes;
using OpenDreamShared.Network.Messages;

namespace OpenDreamClient.Audio;

public interface IDreamSoundEngine {
    void Initialize();
    public void StopFinishedChannels();
    void PlaySound(int channel, MsgSound.FormatType format, ResourceSound sound, float volume);
    void StopChannel(int channel);
    void StopAllChannels();
}
