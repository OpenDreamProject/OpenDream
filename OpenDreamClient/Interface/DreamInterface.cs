using OpenDreamClient.Interface.Elements;
using OpenDreamShared.Interface;
using OpenDreamShared.Net.Packets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace OpenDreamClient.Interface {
    class DreamInterface {
        public Dictionary<string, ElementWindow> Windows = new Dictionary<string, ElementWindow>();
        public Dictionary<string, Window> PopupWindows = new Dictionary<string, Window>();
        public InterfaceDescriptor InterfaceDescriptor { get; private set; } = null;

        public ElementWindow DefaultWindow;
        public ElementOutput DefaultOutput;

        private Window _defaultWindow;

        public void LoadInterfaceDescriptor(InterfaceDescriptor interfaceDescriptor) {
            InterfaceDescriptor = interfaceDescriptor;

            foreach (WindowDescriptor windowDescriptor in InterfaceDescriptor.WindowDescriptors) {
                ElementWindow window = InterfaceHelpers.CreateWindowFromDescriptor(windowDescriptor);

                Windows.Add(windowDescriptor.Name, window);
                if (window.ElementDescriptor.IsDefault) {
                    DefaultWindow = window;
                }

                foreach (IElement element in window.ChildElements) {
                    if (element.ElementDescriptor.IsDefault) {
                        if (element is ElementOutput) DefaultOutput = (ElementOutput)element;
                    }
                }
            }
        }

        public IElement FindElementWithName(string elementName) {
            foreach (ElementWindow window in Windows.Values) {
                foreach (IElement element in window.ChildElements) {
                    if (element.ElementDescriptor.Name == elementName) return element;
                }
            }

            return null;
        }

        public Window CreateWindow(ElementWindow windowElement) {
            Window window = new Window();

            window.Width = windowElement.ElementDescriptor.Size.Value.Width;
            window.Height = windowElement.ElementDescriptor.Size.Value.Height;
            window.Content = windowElement;

            windowElement.UpdateVisuals();
            return window;
        }

        public Window CreatePopupWindow(string windowName, System.Drawing.Size windowSize) {
            WindowDescriptor popupWindowDescriptor = new WindowDescriptor(windowName, new() {
                new ElementDescriptorMain(windowName + "Main") {
                    Size = new System.Drawing.Size(480, 480)
                },
                new ElementDescriptorBrowser(windowName + "Browser") {
                    Size = new System.Drawing.Size(480, 480),
                    Anchor1 = new System.Drawing.Point(0, 0),
                    Anchor2 = new System.Drawing.Point(100, 100)
                }
            });

            Window popup = CreateWindow(InterfaceHelpers.CreateWindowFromDescriptor(popupWindowDescriptor));
            popup.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            popup.Owner = _defaultWindow;
            popup.Width = windowSize.Width;
            popup.Height = windowSize.Height;
            popup.Closed += (object sender, EventArgs e) => {
                PopupWindows.Remove(windowName);
            };

            PopupWindows.Add(windowName, popup);
            return popup;
        }

        public void HandlePacketInterfaceData(PacketInterfaceData pInterfaceData) {
            LoadInterfaceDescriptor(pInterfaceData.InterfaceDescriptor);

            _defaultWindow = CreateDefaultWindow();
        }

        public void HandlePacketOutput(PacketOutput pOutput) {
            IElement interfaceElement;
            string data = null;

            if (pOutput.Control != null) {
                string[] split = pOutput.Control.Split(":");

                interfaceElement = FindElementWithName(split[0]);
                if (split.Length > 1) data = split[1];
            } else {
                interfaceElement = DefaultOutput;
            }

            if (interfaceElement != null) interfaceElement.Output(pOutput.Value, data);
        }

        public void HandlePacketBrowse(PacketBrowse pBrowse) {
            if (pBrowse.HtmlSource == null && pBrowse.Window != null) { //Closing a popup
                if (PopupWindows.TryGetValue(pBrowse.Window, out Window popup)) {
                    popup.Close();
                }
            } else if (pBrowse.HtmlSource != null) { //Outputting to a browser
                string htmlFileName;
                ElementBrowser outputBrowser;

                if (pBrowse.Window != null) {
                    htmlFileName = pBrowse.Window;
                    outputBrowser = FindElementWithName(pBrowse.Window) as ElementBrowser;

                    if (outputBrowser == null) {
                        Window popup;

                        if (!PopupWindows.TryGetValue(pBrowse.Window, out popup)) popup = CreatePopupWindow(pBrowse.Window, pBrowse.Size);

                        ElementWindow windowElement = (ElementWindow)popup.Content;
                        outputBrowser = (ElementBrowser)windowElement.ChildElements[0];
                        popup.Show();
                        popup.Focus();
                    }
                } else {
                    //TODO: Find embedded browser panel
                    htmlFileName = null;
                    outputBrowser = null;
                }

                FileInfo cacheFile = Program.OpenDream.ResourceManager.CreateCacheFile(htmlFileName + ".html", pBrowse.HtmlSource);
                if (outputBrowser != null) outputBrowser.SetFileSource(cacheFile.FullName);
            }
        }

        private Window CreateDefaultWindow() {
            Window defaultWindow = CreateWindow(DefaultWindow);

            defaultWindow.Closed += OnDefaultWindowClosed;
            defaultWindow.KeyDown += OnDefaultWindowKeyDown;
            defaultWindow.KeyUp += OnDefaultWindowKeyUp;
            defaultWindow.Show();

            return defaultWindow;
        }

        private void OnDefaultWindowClosed(object sender, EventArgs e) {
            _defaultWindow = null;

            Program.OpenDream.DisconnectFromServer();
        }

        private void OnDefaultWindowKeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
            int keyCode = InterfaceHelpers.KeyToKeyCode(e.Key);

            if (keyCode != -1) {
                e.Handled = true;

                Program.OpenDream.Connection.SendPacket(new PacketKeyboardInput(new int[1] { keyCode }, new int[0] { }));
            }
        }

        private void OnDefaultWindowKeyUp(object sender, System.Windows.Input.KeyEventArgs e) {
            int keyCode = InterfaceHelpers.KeyToKeyCode(e.Key);

            if (keyCode != -1) {
                e.Handled = true;

                Program.OpenDream.Connection.SendPacket(new PacketKeyboardInput(new int[0] { }, new int[1] { keyCode }));
            }
        }
    }
}
