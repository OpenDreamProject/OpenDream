using OpenDreamShared.Interface;

namespace OpenDreamClient.Interface.Elements {
    interface IElement {
        public ElementDescriptor ElementDescriptor { get; set; }

        public void UpdateVisuals();
    }
}
