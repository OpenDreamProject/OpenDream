using OpenDreamClient.Input;
using OpenDreamClient.Interface.Descriptors;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Serialization.Manager;

namespace OpenDreamClient.Interface;

public sealed class InterfaceMenu : InterfaceElement {
    public readonly Dictionary<string, MenuElement> MenuElements = new();
    public readonly MenuBar MenuBar;

    private readonly bool _pauseMenuCreation;

    public InterfaceMenu(MenuDescriptor descriptor) : base(descriptor) {
        MenuBar = new MenuBar();

        _pauseMenuCreation = true;
        foreach (MenuElementDescriptor menuElement in descriptor.Elements) {
            AddChild(menuElement);
        }

        _pauseMenuCreation = false;
        CreateMenu();
    }

    public override void AddChild(ElementDescriptor descriptor) {
        if (descriptor is not MenuElementDescriptor elementDescriptor)
            throw new ArgumentException($"Attempted to add a {descriptor} to a menu", nameof(descriptor));

        MenuElement element;
        if (elementDescriptor.Category == null) {
            element = new(elementDescriptor, this);
        } else {
            if (!MenuElements.TryGetValue(elementDescriptor.Category, out var parentMenu)) {
                //if category is set but the parent element doesn't exist, create it
                var parentMenuDescriptor = new MenuElementDescriptor() {
                    Name = elementDescriptor.Category
                };

                parentMenu = new(parentMenuDescriptor, this);
                MenuElements.Add(parentMenu.Name, parentMenu);
            }

            //now add this as a child
            element = new MenuElement(elementDescriptor, this);
            parentMenu.Children.Add(element);
        }

        MenuElements.Add(element.Name, element);
        CreateMenu(); // Update the menu to include the new child
    }

    private void CreateMenu() {
        if (_pauseMenuCreation)
            return;

        MenuBar.Menus.Clear();

        foreach (MenuElement menuElement in MenuElements.Values) {
            if (menuElement.Category != null) // We only want the root-level menus here
                continue;

            MenuBar.Menu menu = new() {
                Title = menuElement.Name
            };

            if (menu.Title?.StartsWith("&") ?? false)
                menu.Title = menu.Title[1..]; //TODO: First character in name becomes a selection shortcut

            MenuBar.Menus.Add(menu);
            //visit each node in the tree, populating the menu from that
            foreach (MenuElement child in menuElement.Children)
                menu.Entries.Add(child.CreateMenuEntry());
        }
    }

    public sealed class MenuElement : InterfaceElement {
        public readonly List<MenuElement> Children = new();

        private MenuElementDescriptor MenuElementDescriptor => (MenuElementDescriptor) ElementDescriptor;
        public string Category => MenuElementDescriptor.Category;
        public string Command => MenuElementDescriptor.Command;

        private readonly InterfaceMenu _menu;

        public MenuElement(MenuElementDescriptor data, InterfaceMenu menu) : base(data) {
            _menu = menu;
        }

        public MenuBar.MenuEntry CreateMenuEntry() {
            string text = Name;
            if (text.StartsWith("&"))
                text = text[1..]; //TODO: First character in name becomes a selection shortcut

            if(Children.Count > 0) {
                MenuBar.SubMenu subMenu = new() {
                    Text = text
                };

                foreach(MenuElement child in Children)
                    subMenu.Entries.Add(child.CreateMenuEntry());

                return subMenu;
            }

            if (String.IsNullOrEmpty(text))
                return new MenuBar.MenuSeparator();

            MenuBar.MenuButton menuButton = new() {
                Text = text
            };

            //result.IsCheckable = MenuElementDescriptor.CanCheck;
            if (!String.IsNullOrEmpty(Command))
                menuButton.OnPressed += () => { EntitySystem.Get<DreamCommandSystem>().RunCommand(Command); };
            return menuButton;
        }

        public override void AddChild(ElementDescriptor descriptor) {
            // Set the child's category to this element
            // TODO: The "parent" and "category" attributes seem to be treated differently in BYOND; not the same thing.
            descriptor = ((MenuElementDescriptor) descriptor).WithCategory(IoCManager.Resolve<ISerializationManager>(), MenuElementDescriptor.Name);

            // Pass this on to the parent menu
            _menu.AddChild(descriptor);
        }
    }
}
