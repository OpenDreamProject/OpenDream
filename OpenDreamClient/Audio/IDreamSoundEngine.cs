using OpenDreamClient.Resources.ResourceTypes;
using OpenDreamShared.Net.Packets;

namespace OpenDreamClient.Audio {
    interface IDreamSoundEngine {
        public ISoundEngineData CreateSoundEngineData(ResourceSound sound);
        public void PlaySound(int channel, ResourceSound sound);
        public void StopChannel(int channel);
        public void StopAllChannels();

        public void HandlePacketSound(PacketSound pSound);
    }
}
