using System.Windows;
using System.Windows.Controls;

namespace EchoMacro.View
{
    public partial class FileTreeView_UserControl : UserControl
    {
        public event Action? OnLoadRecord;
        public event Action? OnSaveAsRecord;
        public event Action? OnSaveRecord;
        public event Action? OnMinimizeApp;
        public event Action? OnCloseApp;
        public FileTreeView_UserControl()
        {
            InitializeComponent();
        }
        public void Show(Point position)
        {
            if (this.ContextMenu != null)
            {
                this.ContextMenu.PlacementTarget = this;
                this.ContextMenu.IsOpen = true;
            }
        }
        private void LoadRecord_Click(object sender, RoutedEventArgs e) => OnLoadRecord?.Invoke();
        private void SaveAsRecord_Click(object sender, RoutedEventArgs e) => OnSaveAsRecord?.Invoke();
        private void SaveRecord_Click(object sender, RoutedEventArgs e) => OnSaveRecord?.Invoke();
        private void Minimize_Click(object sender, RoutedEventArgs e) => OnMinimizeApp?.Invoke();
        private void Close_Click(object sender, RoutedEventArgs e) => OnCloseApp?.Invoke();
    }
}
