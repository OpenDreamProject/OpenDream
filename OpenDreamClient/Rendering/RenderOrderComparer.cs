namespace OpenDreamClient.Rendering {
    sealed class RenderOrderComparer : IComparer<(DreamIcon, Vector2, EntityUid, Boolean)> {
        public int Compare((DreamIcon, Vector2, EntityUid, Boolean) x, (DreamIcon, Vector2, EntityUid, Boolean) y) {
            var xAppearance = x.Item1.Appearance;
            var yAppearance = y.Item1.Appearance;
            int val = 0;
            //Plane
            val = xAppearance.Plane.CompareTo(yAppearance.Plane);
            if (val != 0) {
              return val;
            }
            //subplane (ie, HUD vs not HUD)
            val = x.Item4.CompareTo(y.Item4);
            if (val != 0) {
              return val;
            }

            //depending on world.map_format, either layer or physical position
            //TODO
            val = xAppearance.Layer.CompareTo(yAppearance.Layer);
            if (val != 0) {
              return val;
            }

            //Finally, tie-breaker - in BYOND, this is order of creation of the sprites

            return x.Item3.CompareTo(y.Item3);
        }
    }
}
