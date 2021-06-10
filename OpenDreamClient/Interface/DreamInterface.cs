using OpenDreamClient.Interface.Elements;
using OpenDreamClient.Interface.Prompts;
using OpenDreamShared.Compiler;
using OpenDreamShared.Compiler.DMF;
using OpenDreamShared.Dream.Procs;
using OpenDreamShared.Interface;
using OpenDreamShared.Net.Packets;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Web;
using System.Windows;
using System.Windows.Input;

namespace OpenDreamClient.Interface {
    class DreamInterface {
        public Dictionary<string, ElementWindow> Windows = new();
        public Dictionary<string, BrowsePopup> PopupWindows = new();
        public InterfaceDescriptor InterfaceDescriptor { get; private set; } = null;

        public ElementWindow DefaultWindow;
        public ElementOutput DefaultOutput;
        public ElementInfo DefaultInfo;

        private Window _defaultWindow;

        public DreamInterface(OpenDream openDream) {
            openDream.DisconnectedFromServer += OpenDream_DisconnectedFromServer;
        }

        private void OpenDream_DisconnectedFromServer() {
            DefaultWindow.Shutdown();
            _defaultWindow?.Close();
            _defaultWindow = null;
        }

        public void LoadInterfaceFromSource(string source) {
            DMFLexer dmfLexer = new DMFLexer("interface.dmf", source);
            DMFParser dmfParser = new DMFParser(dmfLexer);
            InterfaceDescriptor interfaceDescriptor = dmfParser.Interface();

            if (dmfParser.Warnings.Count > 0) {
                Console.WriteLine("Warnings while parsing interface data");

                foreach (CompilerWarning warning in dmfParser.Warnings) {
                    Console.WriteLine(warning);
                }
            }

            if (dmfParser.Errors.Count > 0) {
                foreach (CompilerError error in dmfParser.Errors) {
                    Console.WriteLine(error);
                }

                throw new Exception("Errors while parsing interface data");
            }

            LoadInterface(interfaceDescriptor);
        }

        public void LoadInterface(InterfaceDescriptor interfaceDescriptor) {
            InterfaceDescriptor = interfaceDescriptor;

            foreach (WindowDescriptor windowDescriptor in InterfaceDescriptor.WindowDescriptors) {
                ElementWindow window = new ElementWindow(windowDescriptor);

                Windows.Add(windowDescriptor.Name, window);
                if (window.IsDefault) {
                    DefaultWindow = window;
                }
            }

            foreach (ElementWindow window in Windows.Values) {
                window.CreateChildElements();

                foreach (InterfaceElement element in window.ChildElements) {
                    if (element.IsDefault) {
                        if (element is ElementOutput elementOutput) DefaultOutput = elementOutput;
                        else if (element is ElementInfo elementInfo) DefaultInfo = elementInfo;
                    }
                }
            }

            _defaultWindow = CreateDefaultWindow();
            _defaultWindow.Show();
        }

        public InterfaceElement FindElementWithName(string name) {
            string[] split = name.Split(".");

            if (split.Length == 2) {
                string windowName = split[0];
                string elementName = split[1];
                ElementWindow window = null;

                if (Windows.ContainsKey(windowName)) {
                    window = Windows[windowName];
                } else if (PopupWindows.TryGetValue(windowName, out BrowsePopup popup)) {
                    window = popup.WindowElement;
                }

                if (window != null) {
                    foreach (InterfaceElement element in window.ChildElements) {
                        if (element.Name == elementName) return element;
                    }
                }
            } else {
                string elementName = split[0];

                foreach (ElementWindow window in Windows.Values) {
                    foreach (InterfaceElement element in window.ChildElements) {
                        if (element.Name == elementName) return element;
                    }
                }

                if (Windows.TryGetValue(elementName, out ElementWindow windowElement)) return windowElement;
            }

            return null;
        }

        #region Packet Handlers

        public void HandlePacketOutput(PacketOutput pOutput) {
            InterfaceElement interfaceElement;
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
                if (PopupWindows.TryGetValue(pBrowse.Window, out BrowsePopup popup)) {
                    popup.Close();
                }
            } else if (pBrowse.HtmlSource != null) { //Outputting to a browser
                string htmlFileName;
                ElementBrowser outputBrowser;

                if (pBrowse.Window != null) {
                    htmlFileName = pBrowse.Window;
                    outputBrowser = FindElementWithName(pBrowse.Window) as ElementBrowser;

                    if (outputBrowser == null) {
                        BrowsePopup popup;

                        if (!PopupWindows.TryGetValue(pBrowse.Window, out popup)) {
                            popup = new BrowsePopup(pBrowse.Window, pBrowse.Size, _defaultWindow);
                            popup.Closed += (object sender, EventArgs e) => {
                                PopupWindows.Remove(pBrowse.Window);
                            };

                            PopupWindows.Add(pBrowse.Window, popup);
                        }

                        outputBrowser = popup.Browser;
                        popup.Open();
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

        public void HandlePacketPrompt(PacketPrompt pPrompt) {
            PromptWindow prompt = null;
            int promptTypeBitflag = (int)pPrompt.Types;

            if ((promptTypeBitflag & (int)DMValueType.Text) != 0) {
                prompt = new TextPrompt(pPrompt.PromptId, pPrompt.Title, pPrompt.Message, pPrompt.DefaultValue);
            } else if ((promptTypeBitflag & (int)DMValueType.Num) != 0) {
                prompt = new NumberPrompt(pPrompt.PromptId, pPrompt.Title, pPrompt.Message, pPrompt.DefaultValue);
            } else if ((promptTypeBitflag & (int)DMValueType.Message) != 0) {
                prompt = new MessagePrompt(pPrompt.PromptId, pPrompt.Title, pPrompt.Message, pPrompt.DefaultValue);
            }

            if (prompt != null) {
                prompt.Owner = _defaultWindow;
                prompt.Show();
            }
        }

        public void HandlePacketUpdateStatPanels(PacketUpdateStatPanels pUpdateStatPanels) {
            DefaultInfo?.UpdateStatPanels(pUpdateStatPanels);
        }

        public void HandlePacketSelectStatPanel(PacketSelectStatPanel pSelectStatPanel) {
            DefaultInfo?.SelectStatPanel(pSelectStatPanel.StatPanel);
        }

        public void HandlePacketWinSet(PacketWinSet pWinSet) {
            if (String.IsNullOrEmpty(pWinSet.ControlId)) throw new NotImplementedException("ControlId is null or empty");

            InterfaceElement element = FindElementWithName(pWinSet.ControlId);
            if (element == null) throw new Exception("Invalid element \"" + pWinSet.ControlId + "\"");

            //params2list
            string winsetParams = pWinSet.Params.Replace(";", "&");
            NameValueCollection query = HttpUtility.ParseQueryString(winsetParams);

            foreach (string attribute in query.AllKeys) {
                if (DMFLexer.ValidAttributes.Contains(attribute)) {
                    string value = query.GetValues(attribute)[^1];

                    Token attributeValue = new DMFLexer(null, value).GetNextToken();
                    if (DMFParser.ValidAttributeValueTypes.Contains(attributeValue.Type)) {
                        element.SetAttribute(attribute, attributeValue.Value);
                    } else {
                        throw new Exception("Invalid attribute value (" + attributeValue.Text + ")");
                    }
                } else {
                    throw new Exception("Invalid attribute \"" + attribute + "\"");
                }
            }
        }
        #endregion Packet Handlers

        private Window CreateDefaultWindow() {
            Window defaultWindow = DefaultWindow.CreateWindow();

            defaultWindow.Closing += OnDefaultWindowClosing;
            defaultWindow.KeyDown += OnDefaultWindowKeyDown;
            defaultWindow.KeyUp += OnDefaultWindowKeyUp;

            return defaultWindow;
        }

        private void OnDefaultWindowClosing(object sender, EventArgs e) {
            _defaultWindow = null;

            Program.OpenDream.DisconnectFromServer();
        }

        private void OnDefaultWindowKeyDown(object sender, KeyEventArgs e) {
            int keyCode = KeyToKeyCode(e.Key);

            if (keyCode != -1) {
                e.Handled = true;

                Program.OpenDream.Connection.SendPacket(new PacketKeyboardInput(new int[1] { keyCode }, new int[0] { }));
            }
        }

        private void OnDefaultWindowKeyUp(object sender, KeyEventArgs e) {
            int keyCode = KeyToKeyCode(e.Key);

            if (keyCode != -1) {
                e.Handled = true;

                Program.OpenDream.Connection.SendPacket(new PacketKeyboardInput(new int[0] { }, new int[1] { keyCode }));
            }
        }

        private static int KeyToKeyCode(Key key) {
            int keyCode = key switch {
                Key.W => 87,
                Key.A => 65,
                Key.S => 83,
                Key.D => 68,
                Key.Up => 38,
                Key.Down => 40,
                Key.Left => 37,
                Key.Right => 39,
                Key.T => 84,
                _ => -1
            };

            return keyCode;
        }
    }
}
