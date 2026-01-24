using OpenDreamShared.Network.Messages;
using Robust.Client.Audio;
using Robust.Shared.Audio.Components;

namespace OpenDreamClient.Audio;

public sealed class DreamSoundChannel(AudioSystem audioSystem, (EntityUid Entity, AudioComponent Component) source, SoundData soundData) {
    public readonly (EntityUid Entity, AudioComponent Component) Source = source;
    private SoundData _soundData = soundData;

    public SoundData SoundData {
        get {
            _soundData.Offset = Source.Component.PlaybackPosition;
            _soundData.Length = (float)audioSystem.GetAudioLength(Source.Component.FileName).TotalSeconds;
            return _soundData;
        }
    }

    public void Stop() {
        audioSystem.Stop(Source.Entity, Source.Component);
    }
}
