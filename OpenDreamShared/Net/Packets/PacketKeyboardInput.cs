namespace OpenDreamShared.Net.Packets {
    public class PacketKeyboardInput : IPacket {
        public PacketID PacketID => PacketID.KeyboardInput;

        public int[] KeysDown;
        public int[] KeysUp;

        public PacketKeyboardInput() { }

        public PacketKeyboardInput(int[] keysDown, int[] keysUp) {
            KeysDown = keysDown;
            KeysUp = keysUp;
        }

        public void ReadFromStream(PacketStream stream) {

            int keyDownCount = stream.ReadByte();
            KeysDown = new int[keyDownCount];
            for (int i = 0; i < keyDownCount; i++) {
                KeysDown[i] = stream.ReadByte();
            }

            int keyUpCount = stream.ReadByte();
            KeysUp = new int[keyUpCount];
            for (int i = 0; i < keyUpCount; i++) {
                KeysUp[i] = stream.ReadByte();
            }
        }

        public void WriteToStream(PacketStream stream) {
            stream.WriteByte((byte)KeysDown.Length);
            foreach (int key in KeysDown) {
                stream.WriteByte((byte)key);
            }

            stream.WriteByte((byte)KeysUp.Length);
            foreach (int key in KeysUp) {
                stream.WriteByte((byte)key);
            }
        }
    }
}
