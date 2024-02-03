using Robust.Client.Audio;
using Robust.Shared.Audio.Components;

namespace OpenDreamClient.Audio;

public sealed class DreamSoundChannel(AudioSystem audioSystem, (EntityUid Entity, AudioComponent Component) source) {
    public readonly (EntityUid Entity, AudioComponent Component) Source = source;

    public void Stop() {
        audioSystem.Stop(Source.Entity, Source.Component);
    }
}
