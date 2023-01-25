namespace OpenDreamClient.Rendering {
    sealed class RenderOrderComparer : IComparer<RendererMetaData> {
        public int Compare(RendererMetaData x, RendererMetaData y) {
            return x.CompareTo(y);
        }
    }
}
