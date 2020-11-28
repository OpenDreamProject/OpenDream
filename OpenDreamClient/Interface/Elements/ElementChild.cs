using OpenDreamShared.Interface;
using System.Windows;
using System.Windows.Controls;

namespace OpenDreamClient.Interface.Elements {
    class ElementChild : Grid, IElement {
        public ElementWindow LeftElement, RightElement;
        public ElementDescriptor ElementDescriptor {
            get => _elementDescriptor;
            set {
                _elementDescriptor = (ElementDescriptorChild)value;
                UpdateVisuals();
            }
        }

        private ElementDescriptorChild _elementDescriptor;

        private void UpdateGrid() {
            this.ColumnDefinitions.Clear();
            this.RowDefinitions.Clear();

            GridSplitter splitter = new GridSplitter();
            splitter.HorizontalAlignment = HorizontalAlignment.Stretch;
            splitter.VerticalAlignment = VerticalAlignment.Stretch;
            this.Children.Add(splitter);

            if (_elementDescriptor.IsVert) {
                ColumnDefinition leftColumnDef = new ColumnDefinition();
                leftColumnDef.Width = new GridLength(1, GridUnitType.Star);
                this.ColumnDefinitions.Add(leftColumnDef);

                ColumnDefinition splitterColumnDef = new ColumnDefinition();
                splitterColumnDef.Width = new GridLength(5);
                this.ColumnDefinitions.Add(splitterColumnDef);

                ColumnDefinition rightColumnDef = new ColumnDefinition();
                rightColumnDef.Width = new GridLength(1, GridUnitType.Star);
                this.ColumnDefinitions.Add(rightColumnDef);

                Grid.SetColumn(LeftElement, 0);
                Grid.SetColumn(splitter, 1);
                Grid.SetColumn(RightElement, 2);
            } else {
                RowDefinition leftRowDef = new RowDefinition();
                leftRowDef.Height = new GridLength(1, GridUnitType.Star);
                this.RowDefinitions.Add(leftRowDef);

                RowDefinition splitterRowDef = new RowDefinition();
                splitterRowDef.Height = new GridLength(5);
                this.RowDefinitions.Add(splitterRowDef);

                RowDefinition rightRowDef = new RowDefinition();
                rightRowDef.Height = new GridLength(1, GridUnitType.Star);
                this.RowDefinitions.Add(rightRowDef);

                Grid.SetRow(LeftElement, 0);
                Grid.SetRow(splitter, 1);
                Grid.SetRow(RightElement, 2);
            }
        }

        private void UpdateVisuals() {
            this.Children.Remove(LeftElement);
            this.Children.Remove(RightElement);

            if (_elementDescriptor.Left != null) {
                InterfaceWindowDescriptor windowDescriptor = Program.OpenDream.GameWindow.InterfaceDescriptor.GetWindowDescriptorFromName(_elementDescriptor.Left);
                LeftElement = InterfaceHelpers.CreateWindowFromDescriptor(windowDescriptor);

                this.Children.Add(LeftElement);
            }

            if (_elementDescriptor.Right != null) {
                InterfaceWindowDescriptor windowDescriptor = Program.OpenDream.GameWindow.InterfaceDescriptor.GetWindowDescriptorFromName(_elementDescriptor.Right);
                RightElement = InterfaceHelpers.CreateWindowFromDescriptor(windowDescriptor);

                this.Children.Add(RightElement);
            }

            UpdateGrid();
        }
    }
}
