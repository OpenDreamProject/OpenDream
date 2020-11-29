using OpenDreamClient.Audio;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenDreamClient.Resources.ResourceTypes {
    class ResourceSound : Resource {
        public ISoundEngineData SoundEngineData;

        public ResourceSound(string resourcePath, byte[] data) : base(resourcePath, data) {
            if (resourcePath.EndsWith(".ogg")) {
                SoundEngineData = Program.OpenDream.SoundEngine.CreateSoundEngineData(this);
            } else {
                throw new Exception("Only *.ogg audio files are supported");
            }
        }
    }
}
