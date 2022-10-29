using OpenDreamShared.Dream.Procs;
using Robust.Client.UserInterface.Controls;

namespace OpenDreamClient.Interface.Prompts;

sealed class ListPrompt : InputWindow {
    private readonly ItemList _itemList;

    public ListPrompt(int promptId, String title, String message, String defaultValue, bool canCancel, string[] values) : base(
        promptId, title, message, defaultValue, canCancel) {

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
}
