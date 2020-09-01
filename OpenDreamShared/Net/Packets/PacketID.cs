namespace OpenDreamShared.Net.Packets {
    enum PacketID {
        Invalid = 0x0,
        ConnectionResult = 0x1,
		RequestConnect = 0x2,
		InterfaceData = 0x3,
		Output = 0x4,
		AtomTypes = 0x5,
		RequestResource = 0x6,
		Resource = 0x7,
		FullGameState = 0x8,
		DeltaGameState = 0x9,
		KeyboardInput = 0xA,
		Topic = 0xB,
		ClickAtom = 0xC,
		ScreenViewChanges = 0xD
    }
}
