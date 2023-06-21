using System.IO;
using OpenDreamShared.Network.Messages;
using Robust.Client.Audio;
using Robust.Client.Graphics;
using Robust.Shared.Audio;

namespace OpenDreamClient.Resources.ResourceTypes {
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class ResourceSound : DreamResource {
        private AudioStream _stream;

        public ResourceSound(int id, byte[] data) : base(id, data) { }

        public IClydeAudioSource Play(MsgSound.FormatType format, AudioParams audioParams) {
            LoadStream(format);

            // TODO: Positional audio.
            var source = IoCManager.Resolve<IClydeAudio>().CreateAudioSource(_stream);
            source.SetGlobal();
            source.SetPitch(audioParams.PitchScale);
            source.SetVolume(audioParams.Volume);
            source.SetPlaybackPosition(audioParams.PlayOffsetSeconds);
            source.IsLooping = audioParams.Loop;

            source.StartPlaying();
            return source;
        }

        private void LoadStream(MsgSound.FormatType format) {
            if (_stream != null)
                return;

            switch (format) {
                case MsgSound.FormatType.Ogg:
                    _stream = IoCManager.Resolve<IClydeAudio>().LoadAudioOggVorbis(new MemoryStream(Data));
                    break;
                case MsgSound.FormatType.Wav:
                    _stream = IoCManager.Resolve<IClydeAudio>().LoadAudioWav(new MemoryStream(Data));
                    break;
                default:
                    Logger.GetSawmill("opendream.audio").Fatal("Only *.ogg and *.wav audio files are supported.");
                    break;
            }
        }
    }
}
