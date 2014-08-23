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
using System.Windows.Shapes;
using Microsoft.VisualStudio.PlatformUI;

namespace ResolveURVisualStudioPackage
{
    /// <summary>
    /// Interaction logic for PackageDialog.xaml
    /// </summary>
    public partial class PackageDialog: DialogWindow
    {
        public PackageDialog()
        {
            InitializeComponent();
        }

        public bool IsResolvePackage
        {
            get;
            set;
        }

        private void btnYes_Click(object sender, RoutedEventArgs e)
        {
            IsResolvePackage = true;    
            this.Close();
        }

        private void btnNo_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

    }
}
