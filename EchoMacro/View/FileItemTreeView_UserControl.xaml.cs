using EchoMacro.Service;
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
        public event Action OnLoadRecord;
        public event Action OnSaveAsRecord;
        public event Action OnCloseApp;
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

        private void SaveRecord_Click(object sender, RoutedEventArgs e)
        {
            //FileManager.SaveRecord();
        }

        /// <summary>
        /// 點擊「Close」- 觸發事件
        /// </summary>
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            OnCloseApp?.Invoke(); // 交給 MainWindow 處理關閉
        }
    }
}
