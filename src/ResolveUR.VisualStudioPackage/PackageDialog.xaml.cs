namespace ResolveURVisualStudioPackage
{
    using System.Windows;

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

        private void btnYes_Click(
            object sender,
            RoutedEventArgs e)
        {
            IsResolvePackage = true;
            Close();
        }

        private void btnNo_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }
    }
}