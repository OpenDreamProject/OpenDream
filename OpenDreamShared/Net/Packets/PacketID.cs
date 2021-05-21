namespace OpenDreamShared.Net.Packets {
    public enum PacketID {
        Invalid = 0x0,
        ConnectionResult = 0x1,
        RequestConnect = 0x2,
        InterfaceData = 0x3,
        Output = 0x4,
        RequestResource = 0x5,
        Resource = 0x6,
        FullGameState = 0x7,
        DeltaGameState = 0x8,
        KeyboardInput = 0x9,
        Topic = 0xA,
        ClickAtom = 0xB,
        ScreenViewChanges = 0xC,
        Sound = 0xD,
        Browse = 0xE,
        BrowseResource = 0xF,
        Prompt = 0x10,
        PromptResponse = 0x11,
        CallVerb = 0x12,
        UpdateAvailableVerbs = 0x13,
        UpdateStatPanels = 0x14
    }
}
