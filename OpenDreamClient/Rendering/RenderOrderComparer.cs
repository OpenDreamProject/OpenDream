namespace OpenDreamClient.Rendering {
    sealed class RenderOrderComparer : IComparer<DMISpriteComponent> {
        public int Compare(DMISpriteComponent x, DMISpriteComponent y) {
            var xAppearance = x.Icon.Appearance;
            var yAppearance = y.Icon.Appearance;
            int val = 0;
            //Plane
            val = xAppearance.Plane.CompareTo(yAppearance.Plane);
            if (val != 0) {
              return val;
            }
            //subplane (ie, HUD vs not HUD)
            //TODO

            //depending on world.map_format, either layer or physical position
            //TODO
            val = xAppearance.Layer.CompareTo(yAppearance.Layer);
            if (val != 0) {
              return val;
            }

            //Finally, tie-breaker - in BYOND, this is order of creation of the sprites
            //If sorting by layer fails, sort by entity UID
            return x.Owner.CompareTo(y.Owner);
        }
    }
}
