using OpenDreamShared.Network.Messages;
using Robust.Client.Audio;
using Robust.Shared.Audio.Components;

namespace OpenDreamClient.Audio;

public sealed class DreamSoundChannel(AudioSystem audioSystem, (EntityUid Entity, AudioComponent Component) source, SoundData soundData) {
    public readonly (EntityUid Entity, AudioComponent Component) Source = source;
    public readonly SoundData SoundData = soundData;

    public void Stop() {
        audioSystem.Stop(Source.Entity, Source.Component);
    }
}
