namespace OpenDreamShared.Dream {
    public enum AtomDirection {
        North = 1,
        South = 2,
        East = 4,
        West = 8,

        Northeast = North | East,
        Southeast = South | East,
        Southwest = South | West,
        Northwest = North | West
    }
}
