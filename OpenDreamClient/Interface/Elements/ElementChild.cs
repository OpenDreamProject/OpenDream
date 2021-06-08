using OpenDreamShared.Interface;
using System.Windows;
using System.Windows.Controls;

namespace OpenDreamClient.Interface.Elements {
    class ElementChild : InterfaceElement {
        private Grid _grid;
        private ElementWindow _leftElement, _rightElement;

        public ElementChild(ElementDescriptor elementDescriptor, ElementWindow window) : base(elementDescriptor, window) { }

        protected override FrameworkElement CreateUIElement() {
            _grid = new Grid();

            return _grid;
        }

        public override void UpdateElementDescriptor() {
            base.UpdateElementDescriptor();

            ElementDescriptorChild elementDescriptor = (ElementDescriptorChild)_elementDescriptor;

            _grid.Children.Remove(_leftElement?.UIElement);
            _grid.Children.Remove(_rightElement?.UIElement);

            if (elementDescriptor.Left != null) {
                _leftElement = Program.OpenDream.Interface.Windows[elementDescriptor.Left];

                _grid.Children.Add(_leftElement.UIElement);
            }

            if (elementDescriptor.Right != null) {
                _rightElement = Program.OpenDream.Interface.Windows[elementDescriptor.Right];

                _grid.Children.Add(_rightElement.UIElement);
            }

            UpdateGrid(elementDescriptor.IsVert);
        }

        public override void Shutdown() {
            _leftElement.Shutdown();
            _rightElement.Shutdown();
        }

        private void UpdateGrid(bool isVert) {
            _grid.ColumnDefinitions.Clear();
            _grid.RowDefinitions.Clear();

            GridSplitter splitter = new GridSplitter() {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            _grid.Children.Add(splitter);

            if (isVert) {
                ColumnDefinition leftColumnDef = new ColumnDefinition();
                leftColumnDef.Width = new GridLength(1, GridUnitType.Star);
                _grid.ColumnDefinitions.Add(leftColumnDef);

                ColumnDefinition splitterColumnDef = new ColumnDefinition();
                splitterColumnDef.Width = new GridLength(5);
                _grid.ColumnDefinitions.Add(splitterColumnDef);

                ColumnDefinition rightColumnDef = new ColumnDefinition();
                rightColumnDef.Width = new GridLength(1, GridUnitType.Star);
                _grid.ColumnDefinitions.Add(rightColumnDef);

                Grid.SetColumn(_leftElement.UIElement, 0);
                Grid.SetColumn(splitter, 1);
                Grid.SetColumn(_rightElement.UIElement, 2);
            } else {
                RowDefinition leftRowDef = new RowDefinition();
                leftRowDef.Height = new GridLength(1, GridUnitType.Star);
                _grid.RowDefinitions.Add(leftRowDef);

                RowDefinition splitterRowDef = new RowDefinition();
                splitterRowDef.Height = new GridLength(5);
                _grid.RowDefinitions.Add(splitterRowDef);

                RowDefinition rightRowDef = new RowDefinition();
                rightRowDef.Height = new GridLength(1, GridUnitType.Star);
                _grid.RowDefinitions.Add(rightRowDef);

                Grid.SetRow(_leftElement.UIElement, 0);
                Grid.SetRow(splitter, 1);
                Grid.SetRow(_rightElement.UIElement, 2);
            }
        }
    }
}
