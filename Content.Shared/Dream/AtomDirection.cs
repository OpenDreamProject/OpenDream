namespace Content.Shared.Dream {
    public enum AtomDirection {
        None = 0,

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
