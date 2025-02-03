using OpenDreamClient.Resources.ResourceTypes;
using OpenDreamShared.Network.Messages;

namespace OpenDreamClient.Audio;

public interface IDreamSoundEngine {
    void Initialize();
    void PlaySound(int channel, MsgSound.FormatType format, ResourceSound sound, float volume, int offset);
    void StopChannel(int channel);
    void StopAllChannels();
}
