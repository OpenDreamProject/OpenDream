using OpenDreamClient.Interface.Controls;
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
using System.Windows.Media.Imaging;

namespace OpenDreamClient.Interface {
    class DreamInterface {
        public Dictionary<string, ControlWindow> Windows = new();
        public Dictionary<string, BrowsePopup> PopupWindows = new();
        public InterfaceDescriptor InterfaceDescriptor { get; private set; } = null;

        public ControlWindow DefaultWindow;
        public ControlOutput DefaultOutput;
        public ControlInfo DefaultInfo;
        public ControlMap DefaultMap;

        private Window _defaultWindow;
        private MacroHandler _macroHandler;

        public DreamInterface(OpenDream openDream) {
            openDream.DisconnectedFromServer += OpenDream_DisconnectedFromServer;
            _macroHandler = new MacroHandler(openDream);
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

            _macroHandler.ClearMacroSets();
            foreach (MacroSetDescriptor macroSetDescriptor in interfaceDescriptor.MacroSetDescriptors) {
                _macroHandler.AddMacroSet(new MacroSet(macroSetDescriptor));
            }

            foreach (WindowDescriptor windowDescriptor in InterfaceDescriptor.WindowDescriptors) {
                ControlWindow window = new ControlWindow(windowDescriptor);

                Windows.Add(windowDescriptor.Name, window);
                if (window.IsDefault) {
                    DefaultWindow = window;
                }
            }

            foreach (ControlWindow window in Windows.Values) {
                window.CreateChildControls();

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

            _defaultWindow = CreateDefaultWindow();
            _defaultWindow.Show();
        }

        public InterfaceElement FindElementWithName(string name) {
            string[] split = name.Split(".");

            if (split.Length == 2) {
                string windowName = split[0];
                string elementName = split[1];
                ControlWindow window = null;

                if (Windows.ContainsKey(windowName)) {
                    window = Windows[windowName];
                } else if (PopupWindows.TryGetValue(windowName, out BrowsePopup popup)) {
                    window = popup.WindowElement;
                }

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

        public void SaveScreenshot(bool openDialog) {
            if (DefaultMap == null) return;

            BitmapSource screenshot = DefaultMap.CreateScreenshot();

            //TODO: Support automatically choosing a location if openDialog == false
            System.Windows.Forms.SaveFileDialog dialog = new() {
                Title = "Screenshot",
                FileName = "screenshot.png",
                DefaultExt = "png",
                Filter = "PNG|*.png",
                AddExtension = true,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                PngBitmapEncoder encoder = new PngBitmapEncoder();

                encoder.Frames.Add(BitmapFrame.Create(screenshot));
                using (FileStream file = File.OpenWrite(dialog.FileName)) {
                    encoder.Save(file);
                }
            }
        }

        #region Packet Handlers
        public void HandlePacketOutput(PacketOutput pOutput) {
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

        public void HandlePacketBrowse(PacketBrowse pBrowse) {
            if (pBrowse.HtmlSource == null && pBrowse.Window != null) { //Closing a popup
                if (PopupWindows.TryGetValue(pBrowse.Window, out BrowsePopup popup)) {
                    popup.Close();
                }
            } else if (pBrowse.HtmlSource != null) { //Outputting to a browser
                string htmlFileName;
                ControlBrowser outputBrowser;

                if (pBrowse.Window != null) {
                    htmlFileName = pBrowse.Window;
                    outputBrowser = FindElementWithName(pBrowse.Window) as ControlBrowser;

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
            bool canCancel = (pPrompt.Types & DMValueType.Null) == DMValueType.Null;

            if ((pPrompt.Types & DMValueType.Text) == DMValueType.Text) {
                prompt = new TextPrompt(pPrompt.PromptId, pPrompt.Title, pPrompt.Message, pPrompt.DefaultValue, canCancel);
            } else if ((pPrompt.Types & DMValueType.Num) == DMValueType.Num) {
                prompt = new NumberPrompt(pPrompt.PromptId, pPrompt.Title, pPrompt.Message, pPrompt.DefaultValue, canCancel);
            } else if ((pPrompt.Types & DMValueType.Message) == DMValueType.Message) {
                prompt = new MessagePrompt(pPrompt.PromptId, pPrompt.Title, pPrompt.Message, pPrompt.DefaultValue, canCancel);
            }

            if (prompt != null) {
                prompt.Owner = _defaultWindow;
                prompt.Show();
            }
        }

        public void HandlePacketAlert(PacketAlert pAlert) {
            AlertWindow alert = new AlertWindow(pAlert.PromptId, pAlert.Title, pAlert.Message, pAlert.Button1, pAlert.Button2, pAlert.Button3);

            alert.Owner = _defaultWindow;
            alert.Show();
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
                    if (Array.IndexOf(DMFParser.ValidAttributeValueTypes, attributeValue.Type) >= 0) {
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
            defaultWindow.KeyDown += _macroHandler.HandleKeyDown;
            defaultWindow.KeyUp += _macroHandler.HandleKeyUp;

            return defaultWindow;
        }

        private void OnDefaultWindowClosing(object sender, EventArgs e) {
            _defaultWindow = null;

            Program.OpenDream.DisconnectFromServer();
        }
    }
}
