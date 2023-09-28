using JetBrains.Annotations;
using OpenDreamClient.Rendering;
using OpenDreamShared.Dream;
using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace OpenDreamClient.Input.ContextMenu {
    [GenerateTypedNameReferences]
    public sealed partial class ContextMenuItem : PanelContainer {
        private static readonly StyleBox HoverStyle = new StyleBoxFlat(Color.Gray);

        private readonly IUserInterfaceManager _uiManager;

        private readonly MetaDataComponent? _entityMetaData;
        private VerbMenuPopup? _currentVerbMenu;

        public ContextMenuItem(IUserInterfaceManager uiManager, IEntityManager entityManager, EntityUid entity) {
            IoCManager.InjectDependencies(this);
            RobustXamlLoader.Load(this);

            _uiManager = uiManager;

            NameLabel.Margin = new Thickness(2, 0, 4, 0);
            if (entityManager.TryGetComponent(entity, out _entityMetaData)) {
                NameLabel.Text = _entityMetaData.EntityName;
            }

            Icon.Margin = new Thickness(2);
            if (entityManager.TryGetComponent(entity, out DMISpriteComponent? sprite)) {
                Icon.Texture = sprite.Icon.CurrentFrame;
            }

            OnMouseEntered += MouseEntered;
            OnMouseExited += MouseExited;
        }

        private void MouseEntered(GUIMouseHoverEventArgs args) {
            PanelOverride = HoverStyle;

            _currentVerbMenu = new VerbMenuPopup(_entityMetaData);
            _uiManager.ModalRoot.AddChild(_currentVerbMenu);

            Vector2 desiredSize = _currentVerbMenu.DesiredSize;
            _currentVerbMenu.Open(UIBox2.FromDimensions(new Vector2(GlobalPosition.X + Size.X, GlobalPosition.Y), desiredSize));
        }

        private void MouseExited(GUIMouseHoverEventArgs args) {
            PanelOverride = null;

            if (_currentVerbMenu != null) {
                _currentVerbMenu.Close();
                _uiManager.ModalRoot.RemoveChild(_currentVerbMenu);
                _currentVerbMenu = null;
            }
        }
    }
}
