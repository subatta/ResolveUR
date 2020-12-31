namespace ResolveUR.VSIXPackage
{
    using System.Windows;

    /// <summary>
    ///     Interaction logic for RemoveConfirmDialog.xaml
    /// </summary>
    public partial class RemoveConfirmDialog
    {
        public RemoveConfirmDialog()
        {
            InitializeComponent();
        }

        public bool IsRemoveConfirm { get; set; }

        void BtnYes_Click(object sender, RoutedEventArgs e)
        {
            IsRemoveConfirm = true;
            Close();
        }

        void BtnNo_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}