using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace OpenDreamShared.Dream;

[Virtual]
public class VerbSystem : EntitySystem {
    [Serializable, NetSerializable]
    public struct VerbInfo {
        /// <summary>
        /// Display name of the verb
        /// <code>/mob/verb/show_inventory() becomes "show inventory"</code>
        /// or <code>set name = "Show Inventory"</code>
        /// </summary>
        public string Name;

        /// <summary>
        /// The verb panel this verb shows in
        /// <code>set category = "Debug"</code>
        /// </summary>
        public string Category;

        /// <summary>
        /// The "invisibility" attribute of this verb
        /// <code>set invisibility = 50</code>
        /// </summary>
        public sbyte Invisibility;

        /// <summary>
        /// The "hidden" attribute of this verb
        /// <code>set hidden = TRUE</code>
        /// </summary>
        public bool HiddenAttribute;

        /// <summary>
        /// The arguments of this verb
        /// </summary>
        public VerbArg[] Arguments;

        /// <returns>The text used to execute this verb in an INPUT control</returns>
        [Pure]
        public string GetCommandName() =>
            Name.ToLowerInvariant().Replace(" ", "-"); // Case-insensitive, dashes instead of spaces

        public string GetCategoryOrDefault(string defaultCategory) =>
            string.IsNullOrWhiteSpace(Category) ? defaultCategory : Category;

        // TODO: Hidden verbs probably shouldn't be sent to the client in the first place?
        public bool IsHidden(bool ignoreHiddenAttr, sbyte seeInvisibility) =>
            (!ignoreHiddenAttr && HiddenAttribute) || Name.StartsWith('.') || seeInvisibility < Invisibility;

        public override string ToString() => GetCommandName();
    }

    [Serializable, NetSerializable]
    public struct VerbArg {
        /// <summary>
        /// Name of the argument
        /// </summary>
        public string Name;

        /// <summary>
        /// Types the argument is allowed to be
        /// </summary>
        public DMValueType Types;
    }

    [Serializable, NetSerializable]
    public sealed class AllVerbsEvent(List<VerbInfo> verbs) : EntityEventArgs {
        public List<VerbInfo> Verbs = verbs;
    }

    [Serializable, NetSerializable]
    public sealed class RegisterVerbEvent(int verbId, VerbInfo verbInfo) : EntityEventArgs {
        public int VerbId = verbId;
        public VerbInfo VerbInfo = verbInfo;
    }

    [Serializable, NetSerializable]
    public sealed class UpdateClientVerbsEvent(List<int> verbIds) : EntityEventArgs {
        public List<int> VerbIds = verbIds;
    }

    [Serializable, NetSerializable]
    public sealed class ExecuteVerbEvent(ClientObjectReference src, int verbId, object?[] arguments) : EntityEventArgs {
        public ClientObjectReference Src = src;
        public int VerbId = verbId;
        public object?[] Arguments = arguments;
    }
}
