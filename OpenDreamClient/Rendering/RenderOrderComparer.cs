using System.Collections.Generic;

namespace OpenDreamClient.Rendering {
    class RenderOrderComparer : IComparer<DMISpriteComponent> {
        public int Compare(DMISpriteComponent x, DMISpriteComponent y) {
            //First, sort by layer
            //var val = x.Layer.CompareTo(y.Layer);
            //if (val != 0) {
            //  return val;
            //}

            //If sorting by layer fails, sort by entity UID
            return x.Owner.Uid.CompareTo(y.Owner.Uid);
        }
    }
}
