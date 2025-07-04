using System.Diagnostics.CodeAnalysis;
using OpenDreamClient.Interface.Controls.UI;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;

namespace OpenDreamClient.Interface.Html.Tags;

/// <summary>
/// Display an appearance
/// </summary>
public sealed class TagIcon : IMarkupTagHandler {
    public const string AppearanceIdAttribute = "AppearanceId";

    public string Name => "icon";

    public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control) {
        if (node.Value.LongValue is not { } appearanceId) {
            control = null;
            return false;
        }

        control = new AppearanceControl((uint)appearanceId) {
            HorizontalAlignment = Control.HAlignment.Left
        };

        return true;
    }
}
