﻿using OpenDreamClient.Interface.Descriptors;
using OpenDreamClient.Interface.DMF;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Serialization.Manager;

namespace OpenDreamClient.Interface;

public sealed class InterfaceMenu : InterfaceElement {
    public readonly Dictionary<string, MenuElement> MenuElements = new();
    public readonly MenuBar MenuBar;

    private readonly bool _pauseMenuCreation;

    public InterfaceMenu(MenuDescriptor descriptor) : base(descriptor) {
        MenuBar = new MenuBar {
            Margin = new(4, 0)
        };

        _pauseMenuCreation = true;
        foreach (MenuElementDescriptor menuElement in descriptor.Elements) {
            AddChild(menuElement);
        }

        _pauseMenuCreation = false;
        CreateMenu();
    }

    public void SetGroupChecked(string group, string id) {
        foreach (MenuElement menuElement in MenuElements.Values) {
            if (menuElement.ElementDescriptor is not MenuElementDescriptor menuElementDescriptor)
                continue;

            if (menuElementDescriptor.Group.AsRaw() == group) {
                menuElementDescriptor.IsChecked = new DMFPropertyBool(menuElementDescriptor.Id.AsRaw() == id);
            }
        }
    }

    public override void AddChild(ElementDescriptor descriptor) {
        if (descriptor is not MenuElementDescriptor elementDescriptor)
            throw new ArgumentException($"Attempted to add a {descriptor} to a menu", nameof(descriptor));

        MenuElement element;
        if (string.IsNullOrEmpty(elementDescriptor.Category.Value)) {
            element = new(elementDescriptor, this);
        } else {
            if (!MenuElements.TryGetValue(elementDescriptor.Category.Value, out var parentMenu)) {
                //if category is set but the parent element doesn't exist, create it
                var parentMenuDescriptor = new MenuElementDescriptor() {
                    Id = elementDescriptor.Category
                };

                parentMenu = new(parentMenuDescriptor, this);
                MenuElements.Add(parentMenu.Id.AsRaw(), parentMenu);
            }

            //now add this as a child
            element = new MenuElement(elementDescriptor, this);
            parentMenu.Children.Add(element);
        }

        MenuElements.Add(element.Id.AsRaw(), element);
        CreateMenu(); // Update the menu to include the new child
    }

    private void CreateMenu() {
        if (_pauseMenuCreation)
            return;

        MenuBar.Menus.Clear();

        foreach (MenuElement menuElement in MenuElements.Values) {
            if (!string.IsNullOrEmpty(menuElement.Category.Value)) // We only want the root-level menus here
                continue;

            MenuBar.Menu menu = new() {
                Title = menuElement.ElementDescriptor.Name.AsRaw()
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
        public DMFPropertyString Category => MenuElementDescriptor.Category;
        public DMFPropertyString Command => MenuElementDescriptor.Command;
        private readonly InterfaceMenu _menu;

        public MenuElement(MenuElementDescriptor data, InterfaceMenu menu) : base(data) {
            _menu = menu;
        }

        public MenuBar.MenuEntry CreateMenuEntry() {
            string text = ElementDescriptor.Name.AsRaw();
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

            if(MenuElementDescriptor.CanCheck.Value)
                if(MenuElementDescriptor.IsChecked.Value)
                    text =  text + " ☑";

            MenuBar.MenuButton menuButton = new() {
                Text = text
            };


            menuButton.OnPressed += () => {
                if(MenuElementDescriptor.CanCheck.Value)
                    if(!string.IsNullOrEmpty(MenuElementDescriptor.Group.Value))
                        _menu.SetGroupChecked(MenuElementDescriptor.Group.Value, MenuElementDescriptor.Id.AsRaw());
                    else
                        MenuElementDescriptor.IsChecked = new DMFPropertyBool(!MenuElementDescriptor.IsChecked.Value);
                _menu.CreateMenu();
                if(!string.IsNullOrEmpty(MenuElementDescriptor.Command.Value))
                    _interfaceManager.RunCommand(Command.AsRaw());
            };
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
