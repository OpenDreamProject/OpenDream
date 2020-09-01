using OpenDreamClient.Resources.ResourceTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenDreamClient.Audio {
    interface IDreamSoundEngine {
        public ISoundEngineData CreateSoundEngineData(ResourceSound sound);
        public void PlaySound(int channel, ResourceSound sound);
        public void StopChannel(int channel);
        public void StopAllChannels();
    }
}
