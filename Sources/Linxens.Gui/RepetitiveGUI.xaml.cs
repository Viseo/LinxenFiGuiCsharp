using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Linxens.Core.Logger;
using Linxens.Core.Model;
using Linxens.Core.Service;

namespace Linxens.Gui
{
    /// <summary>
    ///     MainWindow
    /// </summary>
    public partial class RepetitiveGUI : Window
    {
        private readonly ILogger _technicalLogger;
        private readonly ILogger _qadLogger;

        public DataFileService DataFileService { get; set; }

        public RepetitiveGUI()
        {
            TechnicalLogger.logUi = this.AppendTechnicalLogs;
            QadLogger.logUi = this.AppendQadLogs;

            this._technicalLogger = TechnicalLogger.Instance;
            this._qadLogger = QadLogger.Instance;

            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.InitializeComponent();

            this._technicalLogger.LogInfo("APPLICATION START", "");
            this._qadLogger.LogInfo("APPLICATION START", "");

            this.ChangeUiState(false);
            this.DataFileService = new DataFileService();
            this.gr_result.ItemsSource = this.DataFileService.FilesToProcess;

            if (this.gr_result.Items.Count > 0)
            {
                ChangeUiState(true);
                this.SelectDatagridRow(0);
            }
        }

        private void DataGridRow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            this.ChangeUiState(true);

            this.Statut.Background = Brushes.Green;
            this.Statut.Text = "READY";
            this.DataFileService._technicalLogger.LogInfo("Status", "File selected READY for transmission");
            DataGridRow sdr = (DataGridRow) sender;
            string file = sdr.DataContext.ToString();

            this.DataFileService.ReadFile(file);

            this.tb_site.Text = this.DataFileService.CurrentFile.Site;
            this.tb_emp.Text = this.DataFileService.CurrentFile.Emp;
            this.tb_trtype.Text = this.DataFileService.CurrentFile.TrType;
            this.tb_line.Text = this.DataFileService.CurrentFile.Line;
            this.tb_pn.Text = this.DataFileService.CurrentFile.PN;
            this.tb_op.Text = this.DataFileService.CurrentFile.OP.ToString();
            this.tb_wc.Text = this.DataFileService.CurrentFile.WC;
            this.tb_mhc.Text = this.DataFileService.CurrentFile.MCH;
            this.tb_lbl.Text = this.DataFileService.CurrentFile.LBL;

            this.tb_qty.Text = this.DataFileService.CurrentFile.Qty;
            this.tb_defect.Text = this.DataFileService.CurrentFile.Defect.ToString();
            this.tb_splice.Text = this.DataFileService.CurrentFile.Splices.ToString();
            this.tb_printer.Text = this.DataFileService.CurrentFile.Printer;
            this.tb_numbofconfparts.Text = this.DataFileService.CurrentFile.NumbOfConfParts;
            this.tb_tapeN.Text = this.DataFileService.CurrentFile.TapeN;

            this.gr_scraps.ItemsSource = this.DataFileService.CurrentFile.Scrap.ToArray();
            this.gr_scraps.UpdateLayout();
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            this.SendData();
        }

        public static void Invoke(Action action)
        {
            Dispatcher dispatchObject = Application.Current.Dispatcher;
            if (dispatchObject == null || dispatchObject.CheckAccess())
                action();
            else
                dispatchObject.Invoke(action);
        }

        private void SendData()
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                this.ChangeUiState(false);
                this.Statut.Background = Brushes.Yellow;
                this.Statut.Text = "SENDING";

                AppSettingsReader config = new AppSettingsReader();
                string User = config.GetValue("User", typeof(string)) as string;
                string Domain = config.GetValue("Domain", typeof(string)) as string;
                string Password = config.GetValue("Password", typeof(string)) as string;
                QadService qadService = new QadService(Password, User, Domain);

                Thread sendThread = new Thread(() =>
                {
                    bool res = qadService.Send(this.DataFileService.CurrentFile);
                    this.ChangeUiState(true); // Call UI Thread
                    this.onSendFinished(true);
                });

                sendThread.Start();
            }));
        }

        private void onSendFinished(bool state)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                if (state)
                {
                    this.Statut.Background = Brushes.Green;
                    this.Statut.Text = "DONE";
                    MessageBox.Show("Sending data file Success ! ", "", MessageBoxButton.OK);
                    Application.Current.Shutdown();
                }
                else
                {
                    this.Statut.Background = Brushes.Red;
                    this.Statut.Text = "ERROR";
                    MessageBoxResult response = MessageBox.Show("Sending data file FAILED ! Do you wan to retry sending data ?", "", MessageBoxButton.YesNo);

                    if (response == MessageBoxResult.Yes) this.SendData();
                }
            }));
        }

        private void ChangeUiState(bool state)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                this.tb_site.IsEnabled = state;
                this.tb_emp.IsEnabled = state;
                this.tb_trtype.IsEnabled = state;
                this.tb_line.IsEnabled = state;
                this.tb_pn.IsEnabled = state;
                this.tb_op.IsEnabled = state;
                this.tb_wc.IsEnabled = state;
                this.tb_mhc.IsEnabled = state;
                this.tb_lbl.IsEnabled = state;

                this.tb_defect.IsEnabled = state;
                this.tb_splice.IsEnabled = state;
                this.tb_printer.IsEnabled = state;
                this.tb_numbofconfparts.IsEnabled = state;
                this.tb_tapeN.IsEnabled = state;

                this.gr_result.IsEnabled = state;
                this.gr_scraps.IsEnabled = state;

                this.btRemove.IsEnabled = state;
                this.btAdd.IsEnabled = state;
                this.btSend.IsEnabled = state;
            }));
        }

        private void RemoveScrap(object sender, RoutedEventArgs e)
        {
            int i = 0;
            List<Quality> test = this.DataFileService.CurrentFile.Scrap;
            int ItemsSelected = this.gr_scraps.SelectedItems.Count;
            if (this.gr_result.SelectedItem == null)
            {
                MessageBox.Show("Select a file!");
                this.DataFileService._technicalLogger.LogWarning("Select File", "You dont have selected a file");
            }
            else if (this.gr_scraps.SelectedItem == null)
            {
                MessageBox.Show("select a scrap to delete it!");
                this.DataFileService._technicalLogger.LogWarning("Select Scrap", "You dont have selected a scrap");
            }
            else
            {
                MessageBoxResult messageBoxResult = MessageBox.Show("Are you sure?", "Delete Confirmation", MessageBoxButton.YesNo);
                if (test != null)
                {
                    while (i < ItemsSelected)
                    {
                        Quality itm = this.gr_scraps.SelectedItems[i] as Quality;
                        if (itm != null)
                        {
                            if (messageBoxResult == MessageBoxResult.Yes) test.Remove(itm);
                            i++;
                            this.DataFileService._technicalLogger.LogInfo("Delete Scrap", string.Format("Line Scrap number {0} is deleted successfully", i));
                        }
                    }

                    this.tb_qty.Text = this.DataFileService.CurrentFile.Qty;
                    this.gr_scraps.ItemsSource = this.DataFileService.CurrentFile.Scrap.ToArray();
                }
            }
        }

        private void AddScrap(object sender, RoutedEventArgs e)
        {
            if (this.gr_result.SelectedItem == null)
            {
                MessageBox.Show("You can not add scrap to a non-existent file. Please select a file!");
                this.DataFileService._technicalLogger.LogWarning("Add line for Scap", "You haven't select a file in the file directory for add it a scrap");
            }
            else
            {
                int i = this.DataFileService.CurrentFile.Scrap.Count;
                List<Quality> c = this.DataFileService.CurrentFile.Scrap;
                c.Add(new Quality
                {
                    Qty = "",
                    RsnCode = ""
                });
                i++;
                this.DataFileService.CurrentFile.Scrap = c;
                this.gr_scraps.CurrentItem = c;
                this.DataFileService._technicalLogger.LogInfo("Add Line for Scrap", "You have add line for a new scrap");
                this.gr_scraps.ItemsSource = this.DataFileService.CurrentFile.Scrap.ToArray();
                this.tb_qty.Text = this.DataFileService.CurrentFile.Qty;
            }
        }

        private void Gr_scraps_LostFocus(object sender, RoutedEventArgs e)
        {
            string HoldValueCell = this.DataFileService.CurrentFile.InitialQty;
            DataGridCell cell = e.OriginalSource as DataGridCell;

            if (cell == null)
            {
                TextBox test = e.OriginalSource as TextBox;
                string val = test.Text;
                Quality scrap = this.DataFileService.CurrentFile.Scrap.FirstOrDefault(s => s.Qty == "");
                if (scrap == null)
                {
                    IEnumerable test2 = this.gr_scraps.Items.SourceCollection;
                    return;
                }

                scrap.Qty = val;
                this.tb_qty.Text = this.DataFileService.CurrentFile.Qty;
            }
            else if (cell != null && (string) cell.Column.Header == "Qty")
            {
                string tbvalue = this.DataFileService.CurrentFile.Qty;
                string txtHoldvalue = HoldValueCell.ToString(CultureInfo.InvariantCulture);
                if (tbvalue != txtHoldvalue)
                    this.tb_qty.Text = tbvalue;
                else
                    this.tb_qty.Text = txtHoldvalue;
            }
        }

        private void AppendTechnicalLogs(string message)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                this.techLogs.Items.Add(message);
                this.techLogs.SelectedIndex = this.techLogs.Items.Count - 1;
                this.techLogs.ScrollIntoView(this.techLogs.SelectedItem);
            }));
        }

        private void AppendQadLogs(string message)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                this.qadLogs.Items.Add(message);
                this.qadLogs.SelectedIndex = this.qadLogs.Items.Count - 1;
                this.qadLogs.ScrollIntoView(this.qadLogs.SelectedItem);
            }));
        }

        private void SelectDatagridRow(int index)
        {
            object item = this.gr_result.Items[index];
            this.gr_result.SelectedItem = item;

            DataGridRow gridRow = new DataGridRow();
            gridRow.IsSelected = true;
            gridRow.Item = item;
            gridRow.DataContext = item;

            MouseButtonEventArgs args = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) {RoutedEvent = MouseDoubleClickEvent};
            this.DataGridRow_MouseDoubleClick(gridRow, args);
        }
    }
}