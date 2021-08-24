using System;
using System.Collections.Generic;
using Content.Client.Input;
using Content.Client.Interface.Controls;
using Content.Client.Interface.Prompts;
using Content.Client.Resources;
using Content.Shared.Interface;
using Content.Shared.Network.Messages;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Interface {
    class DreamInterfaceManager : IDreamInterfaceManager {
        [Dependency] private readonly IClyde _clyde = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IDreamMacroManager _macroManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IClientNetManager _netManager = default!;

        public InterfaceDescriptor InterfaceDescriptor { get; private set; }

        public ControlWindow DefaultWindow;
        public ControlOutput DefaultOutput;
        public ControlInfo DefaultInfo;
        public ControlMap DefaultMap;

        public string[] AvailableVerbs { get; private set; } = Array.Empty<string>();

        // private IClydeWindow _window;

        public readonly Dictionary<string, ControlWindow> Windows = new();

        public void LoadDMF(ResourcePath dmfPath) {
            if (!_resourceCache.TryGetResource(dmfPath, out DMFResource dmf) || dmf.Interface == null) {
                Logger.Error($"Error(s) while loading DMF '{dmfPath}'");

                return;
            }

            LoadInterface(dmf.Interface);
        }

        public void Initialize()
        {
            _userInterfaceManager.MainViewport.Visible = false;

            _netManager.RegisterNetMessage<MsgUpdateStatPanels>(RxUpdateStatPanels);
            _netManager.RegisterNetMessage<MsgSelectStatPanel>(RxSelectStatPanel);
            _netManager.RegisterNetMessage<MsgUpdateAvailableVerbs>(RxUpdateAvailableVerbs);
            _netManager.RegisterNetMessage<MsgOutput>(RxOutput);
            _netManager.RegisterNetMessage<MsgAlert>(RxAlert);
            _netManager.RegisterNetMessage<MsgPromptResponse>();
        }

        private void RxUpdateStatPanels(MsgUpdateStatPanels message)
        {
            DefaultInfo?.UpdateStatPanels(message);
        }

        private void RxSelectStatPanel(MsgSelectStatPanel message)
        {
            DefaultInfo?.SelectStatPanel(message.StatPanel);
        }

        private void RxUpdateAvailableVerbs(MsgUpdateAvailableVerbs message)
        {
            AvailableVerbs = message.AvailableVerbs;
            DefaultInfo?.RefreshVerbs();
        }

        public void RxOutput(MsgOutput pOutput) {
            InterfaceControl interfaceElement;
            string data = null;

            if (pOutput.Control != null) {
                string[] split = pOutput.Control.Split(":");

                interfaceElement = (InterfaceControl)FindElementWithName(split[0]);
                if (split.Length > 1) data = split[1];
            } else {
                interfaceElement = DefaultOutput;
            }

            if (interfaceElement != null) interfaceElement.Output(pOutput.Value, data);
        }

        private void RxAlert(MsgAlert message)
        {
            OpenAlert(
                message.PromptId,
                message.Title,
                message.Message,
                message.Button1, message.Button2, message.Button3);
        }

        public void OpenAlert(int promptId, string title, string message, string button1, string button2="", string button3="")
        {
            var alert = new AlertWindow(
                promptId,
                title,
                message,
                button1, button2, button3);

            alert.Owner = _clyde.MainWindow;
            alert.Show();
        }

        public void FrameUpdate(FrameEventArgs frameEventArgs)
        {
            DefaultMap.Viewport.Eye = _eyeManager.CurrentEye;
        }


        public InterfaceElement FindElementWithName(string name) {
            string[] split = name.Split(".");

            if (split.Length == 2) {
                string windowName = split[0];
                string elementName = split[1];
                ControlWindow window = null;

                if (Windows.ContainsKey(windowName)) {
                    window = Windows[windowName];
                } /*else if (PopupWindows.TryGetValue(windowName, out BrowsePopup popup)) {
                    window = popup.WindowElement;
                }*/

                if (window != null) {
                    foreach (InterfaceControl element in window.ChildControls) {
                        if (element.Name == elementName) return element;
                    }
                }
            } else {
                string elementName = split[0];

                foreach (ControlWindow window in Windows.Values) {
                    foreach (InterfaceControl element in window.ChildControls) {
                        if (element.Name == elementName) return element;
                    }
                }

                if (Windows.TryGetValue(elementName, out ControlWindow windowElement)) return windowElement;
            }

            return null;
        }

        private void LoadInterface(InterfaceDescriptor descriptor)
        {
            InterfaceDescriptor = descriptor;

            _macroManager.LoadMacroSets(InterfaceDescriptor.MacroSetDescriptors);
            _macroManager.SetActiveMacroSet(InterfaceDescriptor.MacroSetDescriptors[0]);

            foreach (WindowDescriptor windowDescriptor in InterfaceDescriptor.WindowDescriptors) {
                ControlWindow window = new ControlWindow(windowDescriptor);

                Windows.Add(windowDescriptor.Name, window);
                if (window.IsDefault) {
                    DefaultWindow = window;
                }
            }

            foreach (ControlWindow window in Windows.Values) {
                window.CreateChildControls(this);

                foreach (InterfaceControl control in window.ChildControls) {
                    if (control.IsDefault) {
                        switch (control) {
                            case ControlOutput controlOutput: DefaultOutput = controlOutput; break;
                            case ControlInfo controlInfo: DefaultInfo = controlInfo; break;
                            case ControlMap controlMap: DefaultMap = controlMap; break;
                        }
                    }
                }
            }

            DefaultWindow.UIElement.Name = "MainWindow";

            LayoutContainer.SetAnchorRight(DefaultWindow.UIElement, 1);
            LayoutContainer.SetAnchorBottom(DefaultWindow.UIElement, 1);

            _userInterfaceManager.StateRoot.AddChild(DefaultWindow.UIElement);
        }
    }

    interface IDreamInterfaceManager
    {
        string[] AvailableVerbs { get; }
        public InterfaceDescriptor InterfaceDescriptor { get; }

        public void LoadDMF(ResourcePath dmfPath);
        void Initialize();
        void FrameUpdate(FrameEventArgs frameEventArgs);
        InterfaceElement FindElementWithName(string name);
    }
}
