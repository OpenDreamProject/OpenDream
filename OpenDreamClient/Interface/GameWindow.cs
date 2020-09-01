using OpenDreamClient.Interface.Elements;
using OpenDreamShared.Interface;
using OpenDreamShared.Net.Packets;
using System;
using System.Windows.Input;
using System.Windows.Threading;

namespace OpenDreamClient.Interface {
    class GameWindow : System.Windows.Window {
        public InterfaceWindowDescriptor WindowDescriptor = null;
        public ElementOutput DefaultOutput = null;
        public InterfaceDescriptor InterfaceDescriptor { get; private set; } = null;

        private DispatcherTimer _updateTimer = new DispatcherTimer();

        public GameWindow() {
            this.Loaded += OnLoaded;
            this.Closed += OnClosed;
            this.KeyDown += OnKeyDown;
            this.KeyUp += OnKeyUp;

            _updateTimer.Interval = TimeSpan.FromMilliseconds(1000 / 20);
            _updateTimer.Tick += UpdateTimerTick;
        }

        public void SetInterface(InterfaceDescriptor interfaceDescriptor) {
            InterfaceDescriptor = interfaceDescriptor;

            ElementWindow defaultWindow = InterfaceHelpers.CreateWindowFromDescriptor(InterfaceDescriptor.DefaultWindowDescriptor);
            this.Width = defaultWindow.ElementDescriptor.Size.Width;
            this.Height = defaultWindow.ElementDescriptor.Size.Height;
            this.Content = defaultWindow;
            defaultWindow.Focus();
        }
        
        public void HandlePacketInterfaceData(PacketInterfaceData pInterfaceData) {
            SetInterface(pInterfaceData.InterfaceDescriptor);
        }

        private void OnLoaded(object sender, EventArgs e) {
            _updateTimer.Start();
        }

        private void OnClosed(object sender, EventArgs e) {
            _updateTimer.Stop();
            Program.OpenDream.DisconnectFromServer();
        }

        private void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
            int keyCode = KeyToKeyCode(e.Key);

            if (keyCode != -1) {
                Program.OpenDream.Connection.SendPacket(new PacketKeyboardInput(new int[1] { keyCode }, new int[0] { }));
            }
        }

        private void OnKeyUp(object sender, System.Windows.Input.KeyEventArgs e) {
            int keyCode = KeyToKeyCode(e.Key);

            if (keyCode != -1) {
                Program.OpenDream.Connection.SendPacket(new PacketKeyboardInput(new int[0] { }, new int[1] { keyCode }));
            }
        }

        private void UpdateTimerTick(object sender, EventArgs e) {
            if (Program.OpenDream.Connection.Connected) {
                Program.OpenDream.Connection.ProcessPackets();
            }
        }

        private int KeyToKeyCode(Key key) {
            int keyCode = -1;

            switch (key) {
                case Key.W: keyCode = 87; break;
                case Key.A: keyCode = 65; break;
                case Key.S: keyCode = 83; break;
                case Key.D: keyCode = 68; break;
                case Key.Up: keyCode = 38; break;
                case Key.Down: keyCode = 40; break;
                case Key.Left: keyCode = 37; break;
                case Key.Right: keyCode = 39; break;
            }

            return keyCode;
        }
    }
}