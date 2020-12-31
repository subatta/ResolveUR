namespace ResolveUR.VSIXPackage
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

        void BtnYes_Click(object sender, RoutedEventArgs e)
        {
            IsResolvePackage = true;
            Close();
        }

        void BtnNo_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}