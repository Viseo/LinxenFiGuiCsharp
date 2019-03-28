using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;


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

        private void PwdResponse_OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key != System.Windows.Input.Key.Enter) return;

            // your event handler here
            e.Handled = true;
            this.btOk.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        }
    }
}
