using System.Collections.Generic;
using Content.Client.Input;
using Content.Client.Interface.Controls;
using Content.Client.Resources;
using Content.Shared.Interface;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Interface {
    class DreamInterfaceManager : IDreamInterfaceManager {
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IDreamMacroManager _macroManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;

        public InterfaceDescriptor InterfaceDescriptor { get; private set; }

        public ControlWindow DefaultWindow;
        public ControlOutput DefaultOutput;
        public ControlInfo DefaultInfo;
        public ControlMap DefaultMap;

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
        }

        public void FrameUpdate(FrameEventArgs frameEventArgs)
        {
            DefaultMap.Viewport.Eye = _eyeManager.CurrentEye;
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

    interface IDreamInterfaceManager {
        public InterfaceDescriptor InterfaceDescriptor { get; }

        public void LoadDMF(ResourcePath dmfPath);
        void Initialize();
        void FrameUpdate(FrameEventArgs frameEventArgs);
    }
}
