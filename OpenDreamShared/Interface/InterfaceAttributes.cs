﻿using System;

namespace OpenDreamShared.Interface {
    [AttributeUsage(AttributeTargets.Field)]
    public class InterfaceAttributeAttribute : Attribute {
        public string Name;

        public InterfaceAttributeAttribute(string name) {
            Name = name;
        }
    }
}
