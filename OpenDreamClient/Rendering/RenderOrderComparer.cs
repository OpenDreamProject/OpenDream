namespace OpenDreamClient.Rendering {
    sealed class RenderOrderComparer : IComparer<DMISpriteComponent> {
        public int Compare(DMISpriteComponent x, DMISpriteComponent y) {
            var xAppearance = x.Icon.Appearance;
            var yAppearance = y.Icon.Appearance;

            //First, sort by layer
            var val = xAppearance.Layer.CompareTo(yAppearance.Layer);
            if (val != 0) {
              return val;
            }

            //If sorting by layer fails, sort by entity UID
            return x.Owner.CompareTo(y.Owner);
        }
    }
}
