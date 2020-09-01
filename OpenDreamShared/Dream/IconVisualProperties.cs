namespace OpenDreamShared.Dream {
    public struct IconVisualProperties {
        public string Icon, IconState;
        public AtomDirection Direction;
        public float Layer;

        public bool Equals(IconVisualProperties other) {
            if (other.Icon != Icon) return false;
            if (other.IconState != IconState) return false;
            if (other.Direction != Direction) return false;
            if (other.Layer != Layer) return false;

            return true;
        }

        public IconVisualProperties Merge(IconVisualProperties other) {
            IconVisualProperties newVisualProperties = this;

            if (other.Icon != default) newVisualProperties.Icon = other.Icon;
            if (other.IconState != default) newVisualProperties.IconState = other.IconState;
            if (other.Direction != default) newVisualProperties.Direction = other.Direction;
            if (other.Layer != default) newVisualProperties.Layer = other.Layer;

            return newVisualProperties;
        }

        public bool IsDefault() {
            if (Icon != default) return false;
            if (IconState != default) return false;
            if (Direction != default) return false;
            if (Layer != default) return false;

            return true;
        }
    }
}
