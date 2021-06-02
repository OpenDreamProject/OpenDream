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
            }
        }

        private ElementDescriptorChild _elementDescriptor;

        public void UpdateVisuals() {
            this.Children.Remove(LeftElement);
            this.Children.Remove(RightElement);

            if (_elementDescriptor.Left != null) {
                LeftElement = Program.OpenDream.Interface.Windows[_elementDescriptor.Left];

                LeftElement.UpdateVisuals();
                this.Children.Add(LeftElement);
            }

            if (_elementDescriptor.Right != null) {
                RightElement = Program.OpenDream.Interface.Windows[_elementDescriptor.Right];

                RightElement.UpdateVisuals();
                this.Children.Add(RightElement);
            }

            UpdateGrid();
        }

        public void Shutdown() {
            LeftElement.Shutdown();
            RightElement.Shutdown();
        }

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
    }
}
