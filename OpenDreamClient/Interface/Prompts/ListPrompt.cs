using OpenDreamShared.Dream;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;

namespace OpenDreamClient.Interface.Prompts;

internal sealed class ListPrompt : InputWindow {
    private readonly ItemList _itemList;

    public ListPrompt(string title, string message, string defaultValue, bool canCancel, string[] values,
        Action<DMValueType, object?>? onClose) : base(title, message, canCancel, onClose) {
        _itemList = new();

        bool foundDefault = false;
        foreach (string value in values) {
            ItemList.Item item = new(_itemList) {
                Text = value
            };

            _itemList.Add(item);
            if (value == defaultValue) {
                item.Selected = true;
                foundDefault = true;
            }
        }

        if (!foundDefault) _itemList[0].Selected = true;
        _itemList.OnKeyBindDown += ItemList_KeyBindDown;
        SetPromptControl(_itemList, grabKeyboard: false);
    }

    protected override void OkButtonClicked() {;
        foreach (ItemList.Item item in _itemList) {
            if (!item.Selected)
                continue;

            FinishPrompt(DMValueType.Num, (float)_itemList.IndexOf(item));
            return;
        }

        // Prompt is not finished if nothing was selected
    }

    private void ItemList_KeyBindDown(GUIBoundKeyEventArgs e) {
        if (e.Function == EngineKeyFunctions.TextSubmit) {
            e.Handle();
            ButtonClicked(DefaultButton);
        }
    }
}
