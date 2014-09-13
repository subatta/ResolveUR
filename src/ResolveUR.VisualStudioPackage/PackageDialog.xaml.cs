using System.Windows;

namespace ResolveURVisualStudioPackage
{
    /// <summary>
    ///     Interaction logic for PackageDialog.xaml
    /// </summary>
    public partial class PackageDialog
    {
        public PackageDialog()
        {
            InitializeComponent();
        }

        public bool IsResolvePackage { get; set; }

        void btnYes_Click(object sender, RoutedEventArgs e)
        {
            IsResolvePackage = true;
            Close();
        }

        void btnNo_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}