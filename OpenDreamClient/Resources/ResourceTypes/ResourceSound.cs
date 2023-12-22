using System.IO;
using JetBrains.Annotations;
using OpenDreamShared.Network.Messages;
using Robust.Client.Audio;

namespace OpenDreamClient.Resources.ResourceTypes;

[UsedImplicitly]
public sealed class ResourceSound(int id, byte[] data) : DreamResource(id, data) {
    private AudioStream? _stream;

    public AudioStream? GetStream(MsgSound.FormatType format, IAudioManager audioManager) {
        if (_stream == null) {
            switch (format) {
                case MsgSound.FormatType.Ogg:
                    _stream = audioManager.LoadAudioOggVorbis(new MemoryStream(Data));
                    break;
                case MsgSound.FormatType.Wav:
                    _stream = audioManager.LoadAudioWav(new MemoryStream(Data));
                    break;
                default:
                    Logger.GetSawmill("opendream.audio").Fatal("Only *.ogg and *.wav audio files are supported.");
                    break;
            }
        }

        return _stream;
    }
}
