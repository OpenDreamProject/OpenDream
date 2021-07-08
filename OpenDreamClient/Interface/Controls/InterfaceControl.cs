using OpenDreamShared.Interface;
using System;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace OpenDreamClient.Interface.Controls
{
    class InterfaceControl : Control, IInterfaceElement
    {
        private ControlDescriptor _controlDescriptor;

        ElementDescriptor IInterfaceElement.ElementDescriptor
        {
            get => _controlDescriptor;
            set
            {
                if (value is not ControlDescriptor controlDescriptor)
                    throw new Exception($"ElementDescriptor must be able to be casted to {nameof(controlDescriptor)}!");
                _controlDescriptor = controlDescriptor;
            }
        }

        public void SetAttribute(string name, object value)
        {
            throw new NotImplementedException();
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            return new(_controlDescriptor.Size?.Width ?? 0, _controlDescriptor.Size?.Height ?? 0);
        }

        public void UpdateElementDescriptor()
        {
            LayoutContainer.SetAnchorLeft(this, _controlDescriptor.Anchor1?.X ?? 0);
            LayoutContainer.SetAnchorTop(this, _controlDescriptor.Anchor1?.Y ?? 0);
            LayoutContainer.SetAnchorRight(this, _controlDescriptor.Anchor2?.X ?? 0);
            LayoutContainer.SetAnchorBottom(this, _controlDescriptor.Anchor2?.Y ?? 0);

            // TODO ROBUST: Pos?

            Visible = _controlDescriptor.IsVisible;
        }

        public void Shutdown()
        {
            Dispose();
        }
    }
}
