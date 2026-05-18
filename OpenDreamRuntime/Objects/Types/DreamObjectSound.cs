namespace OpenDreamRuntime.Objects.Types;

public sealed class DreamObjectSound : DreamObject {
    public ushort Channel, Volume;
    public float Offset;
    public byte Repeat { get; set => field = Math.Clamp(value, (byte)0, (byte)2); }
    public DreamValue File;

    public DreamObjectSound(DreamObjectDefinition objectDefinition) : base(objectDefinition) {
        if (objectDefinition.TryGetVariable("channel", out var channel)) {
            channel.TryGetValueAsInteger(out var channelValue);
            Channel = (ushort)channelValue;
        }

        if (objectDefinition.TryGetVariable("volume", out var volume)) {
            volume.TryGetValueAsInteger(out var volumeValue);
            Volume = (ushort)volumeValue;
        }

        if (objectDefinition.TryGetVariable("offset", out var offset)) {
            offset.TryGetValueAsFloat(out Offset);
        }

        if (objectDefinition.TryGetVariable("repeat", out var repeat)) {
            Repeat = (byte)repeat.UnsafeGetValueAsFloat();
        }

        objectDefinition.TryGetVariable("file", out File);
    }

    protected override bool TryGetVar(string varName, out DreamValue value) {
        switch (varName) {
            case "channel": value = new(Channel); return true;
            case "volume": value = new(Volume); return true;
            case "offset": value = new(Offset); return true;
            case "repeat": value = new(Repeat); return true;
            case "file": File.IncRef(); value = File; return true;
            default: return base.TryGetVar(varName, out value);
        }
    }

    protected override void SetVar(string varName, DreamValue value) {
        switch (varName) {
            case "channel": Channel = (ushort)value.UnsafeGetValueAsFloat(); break;
            case "volume": Volume = (ushort)value.UnsafeGetValueAsFloat(); break;
            case "offset": Offset = value.UnsafeGetValueAsFloat(); break;
            case "repeat": Repeat = (byte)value.UnsafeGetValueAsFloat(); break;
            case "file":
                value.IncRef();
                File.DecRef();
                File = value;
                break;
            default: base.SetVar(varName, value); break;
        }
    }
}
