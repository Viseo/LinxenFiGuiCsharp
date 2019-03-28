using System.Diagnostics;
using System.Linq;
using System.Windows;

namespace Linxens.Gui
{
    /// <summary>
    ///     Logique d'interaction pour App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            Process proc = Process.GetCurrentProcess();
            int count = Process.GetProcesses().Where(p =>
                p.ProcessName == proc.ProcessName).Count();
            if (count > 1)
            {
                MessageBox.Show("An instance of Fi Auto Data Entry is already started...");
                App.Current.Shutdown();
            }
            base.OnStartup(e);
        }
    }
}