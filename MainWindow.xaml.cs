using PDFPages.ViewModels;
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

namespace PDFPages
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private void dgFiles_Sorting(object sender, DataGridSortingEventArgs e)
        {
            (DataContext as MainVM)?.SortFiles();
            e.Handled = true;
        }
        private void dgOutputFiles_Sorting(object sender, DataGridSortingEventArgs e)
        {
            (DataContext as MainVM)?.SortOutputFiles();
            e.Handled = true;
        }
    }
}
