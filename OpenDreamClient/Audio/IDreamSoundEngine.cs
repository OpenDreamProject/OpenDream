using OpenDreamClient.Resources.ResourceTypes;
using OpenDreamShared.Network.Messages;

namespace OpenDreamClient.Audio;

public interface IDreamSoundEngine {
    void Initialize();
    void PlaySound(SoundData soundData, MsgSound.FormatType format, ResourceSound sound);
    void StopChannel(int channel);
    void StopAllChannels();

    List<SoundData>? GetSoundQuery();
}
