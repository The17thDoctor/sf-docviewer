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

namespace Starfall_Documentation_Viewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    ///

    public partial class MainWindow : Window
    {
        readonly DocumentationCreator creator = new();
        public MainWindow()
        {
            InitializeComponent();
            if (!creator.FetchDocumentation()) EnableWarning();
            creator.PopulateTree(MainTreeView);        
        }

        public void EnableWarning()
        {
            Thickness Margin = MainTreeView.Margin;
            Margin.Bottom = 30;
            MainTreeView.Margin = Margin;
            WarningLabel.Visibility = Visibility.Visible;
        }

        private void TVISelected(object sender, RoutedEventArgs e)
        {
            TreeViewItem TVI = (TreeViewItem)sender;
            creator.CreatePage((string)TVI.Tag, DocumentationPage);
            e.Handled = true;
        }
    }
}
