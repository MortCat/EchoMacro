using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EchoMacro.View
{
    public partial class FileTreeView_UserControl : UserControl
    {
        public FileTreeView_UserControl()
        {
            InitializeComponent();
        }
        public void Show(Point position)
        {
            TreePopup.HorizontalOffset = position.X - 3;
            TreePopup.VerticalOffset = position.Y - 3;
            TreePopup.IsOpen = true;
        }
        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItem item)
            {
                MessageBox.Show($"You selected: {item.Header}");
                TreePopup.IsOpen = false;
            }
        }
    }
}
