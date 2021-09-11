using System.Collections.Generic;

namespace OpenDreamShared.Interface {
    public class MenuDescriptor {
        public string Name;
        public List<MenuElementDescriptor> Elements;

        public MenuDescriptor(string name, List<MenuElementDescriptor> elements) {
            Name = name;
            Elements = elements;
        }
    }

    public class MenuElementDescriptor : ElementDescriptor {
        [InterfaceAttribute("command")]
        public string Command;
        [InterfaceAttribute("category")]
        public string Category;
        [InterfaceAttribute("can-check")]
        public bool CanCheck;

        public MenuElementDescriptor(string name) : base(name) { }
    }
}
