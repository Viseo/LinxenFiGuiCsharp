using System.Windows;


namespace Linxens.Gui
{
    /// <summary>
    /// Logique d'interaction pour PasswordConfirm.xaml
    /// </summary>
    public partial class PasswordConfirm : Window
    {
        public PasswordConfirm()
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
        

        public string PasswordResponse
        {
            get { return pwdResponse.Password; }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
