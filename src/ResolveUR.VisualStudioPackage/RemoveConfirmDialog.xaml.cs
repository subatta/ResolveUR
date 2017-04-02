namespace ResolveURVisualStudioPackage
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

        void btnYes_Click(object sender, RoutedEventArgs e)
        {
            IsRemoveConfirm = true;
            Close();
        }

        void btnNo_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}