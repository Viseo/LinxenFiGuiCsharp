using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Linxens.Core.Logger;
using Linxens.Core.Model;
using Linxens.Core.Service;
using Application = System.Windows.Application;
using DataGridCell = System.Windows.Controls.DataGridCell;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using TextBox = System.Windows.Controls.TextBox;

namespace Linxens.Gui
{
    /// <summary>
    ///     MainWindow
    /// </summary>
    public partial class RepetitiveGUI : Window
    {
        private readonly ILogger _qadLogger;
        private readonly ILogger _technicalLogger;

        private readonly Regex Reg1 = new Regex("^[0-9]*$+");
        private readonly Regex Reg2 = new Regex("^[a-zA-Z0-9-]*$");

        private bool fileInfoReadOnly = true;

        public RepetitiveGUI()
        {
            this.InitializeComponent();
            this.Title = "Fi Auto Data Entry v" + this.getRunningVersion();
            TechnicalLogger.logUi = this.AppendTechnicalLogs;
            QadLogger.logUi = this.AppendQadLogs;

            this._technicalLogger = TechnicalLogger.Instance;
            this._qadLogger = QadLogger.Instance;

            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;


            this._technicalLogger.LogInfo("APPLICATION START", "");
            this._qadLogger.LogInfo("APPLICATION START", "");


            this.DataFileService = new DataFileService();
            this.gr_result.ItemsSource = this.DataFileService.FilesToProcess;

            //this.ChangeUiState(false);
            this.SelectDatagridRowIfExist();
        }

        public DataFileService DataFileService { get; set; }

        private void DataGridRow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            this.ChangeUiState(false);

            DataGridRow sdr = (DataGridRow) sender;
            string file = sdr.DataContext.ToString();
            DataFile datafile = this.DataFileService.ReadFile(file);

            if (datafile == null)
            {
                this.gr_result.IsEnabled = true;
                this._technicalLogger.LogWarning("Read File", "This File is not read correctly. This is due either to the format of the file which is not of type txt or to the contents of the file which does not respect the format of files FI Station");
                return;
            }

            this.DataFileService.CurrentFile = datafile;

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
            this.gr_scraps.Items.Refresh();
            this.gr_scraps.UpdateLayout();

            this.Statut.Background = Brushes.Green;
            this.Statut.Text = "READY";
            this.ChangeUiState(true);
            this._technicalLogger.LogInfo("Status", "File selected READY for transmission");
        }

        private void Submit_OnClick(object sender, RoutedEventArgs e)
        {
            this.SendData();
        }

        private void SendData()
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                this.ChangeUiState(false);
                this.Statut.Background = Brushes.Yellow;
                this.Statut.Text = "SENDING";

                AppSettingsReader config = new AppSettingsReader();
                string Domain = config.GetValue("Domain", typeof(string)) as string;
                QadService qadService = new QadService(Domain);

                int Attempt = int.Parse(config.GetValue("AutoRetrySendOnError", typeof(string)) as string);

                Thread sendThread = new Thread(() =>
                {
                    bool res = false;
                    string error = "";
                    this.DataFileService.WriteFile();
                    for (int i = 1; i <= Attempt; i++)
                    {
                        this._qadLogger.LogInfo("Send file", string.Format("Attempt {0}/{1}", i, Attempt));
                        res = qadService.Send(this.DataFileService.CurrentFile, out error);
                        if (res) break;
                        this._qadLogger.LogInfo("Send file", string.Format("Sending attempt {0} has failed", i));
                    }

                    if (!res)
                        try
                        {
                            string host = config.GetValue("EmailNotificationServer", typeof(string)) as string;
                            string port = config.GetValue("EmailNotificationPort", typeof(string)) as string;
                            string from = config.GetValue("EmailNotificationFrom", typeof(string)) as string;
                            string to = config.GetValue("EmailNotificationTo", typeof(string)) as string;
                            MailService mailService = new MailService(host, int.Parse(port));
                            mailService.Send(from, to, "[FiAutoDataEntry] Error on attempt to send data", "Errors => \n\n" + error);
                        }
                        catch (Exception e)
                        {
                            string innerMessage = "";
                            if (e.InnerException != null) innerMessage = e.InnerException.Message;
                            this._technicalLogger.LogError("Send alert mail", e.Message + " " + innerMessage);
                        }

                    this.ChangeUiState(true);
                    this.onSendFinished(res);
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
                    this.DataFileService.successFile();
                    MessageBox.Show("Sending data file Success ! ", "", MessageBoxButton.OK);
                    Application.Current.Shutdown();
                }
                else
                {
                    this.Statut.Background = Brushes.Red;
                    this.Statut.Text = "ERROR";
                    this.DataFileService.ErrorFile();
                }
            }));
        }

        private void ChangeUiState(bool state)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                if (this.fileInfoReadOnly)
                    this.ChangeFileInfoUIState(false);
                else
                    this.ChangeFileInfoUIState(state);
                this.gr_result.IsEnabled = state;
                this.btSend.IsEnabled = state;
                this.btBrowse.IsEnabled = state;
            }));
        }

        private void ChangeFileInfoUIState(bool state)
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

                this.gr_scraps.IsEnabled = state;
                this.btRemove.IsEnabled = state;
                this.btAdd.IsEnabled = state;
            }));
        }

        private void RemoveScrap(object sender, RoutedEventArgs e)
        {
            int i = 0;
            List<Quality> ListScrap = this.DataFileService.CurrentFile.Scrap;
            Quality qtySelected = this.gr_scraps.SelectedItem as Quality;
            if (this.gr_result.SelectedItem == null)
            {
                MessageBox.Show("Select a file!");
                this._technicalLogger.LogInfo("Select File", "You don't have selected a file on the file list. This action can only be performed if you have selected a file in the file list and a Scrap belonging to this file");
            }
            else if (this.gr_scraps.SelectedItem == null)
            {
                MessageBox.Show("select a scrap to delete it!");
                this._technicalLogger.LogInfo("Select Scrap", "You don't have selected a scrap. The removal of a Scrap follows the selection of it");
            }
            else
            {
                MessageBoxResult messageBoxResult = MessageBox.Show("Are you sure?", "Delete Confirmation", MessageBoxButton.YesNo);
                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    int index = ListScrap.IndexOf(qtySelected);
                    ListScrap.RemoveAt(index);
                    this._technicalLogger.LogInfo("Delete Scrap", string.Format("Qty:  {0} Rsn Code:  {1} is deleted successfully", qtySelected.Qty, qtySelected.RsnCode));
                    this.tb_qty.Text = this.DataFileService.CurrentFile.Qty;
                    this.gr_scraps.ItemsSource = this.DataFileService.CurrentFile.Scrap.ToArray();
                }
            }
        }

        private void AddScrap(object sender, RoutedEventArgs e)
        {
            if (this.gr_result.SelectedItem == null)
            {
                MessageBox.Show("You can not add scrap to a non-existent file. Please select a file in the file list!");
                this._technicalLogger.LogInfo("Add new Scap", "You haven't select a file in the file list. The addition of a scrap follows the selection of a file in the list of files");
            }
            else
            {
                List<Quality> c = this.DataFileService.CurrentFile.Scrap;
                c.Add(new Quality
                {
                    Qty = "",
                    RsnCode = ""
                });
                this.DataFileService.CurrentFile.Scrap = c;
                this.gr_scraps.CurrentItem = c;
                this._technicalLogger.LogInfo("Add new Scrap", "You have add line for a new scrap on the file selected.");
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
                TextBox TextBx = e.OriginalSource as TextBox;
                string val = TextBx.Text;
                Quality scrap = this.DataFileService.CurrentFile.Scrap.FirstOrDefault(s => s.Qty == "");
                if (scrap == null)
                {
                    IEnumerable collectionFiles = this.gr_scraps.Items.SourceCollection;
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

        private void SelectDatagridRowIfExist()
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                if (this.gr_result.Items.Count > 0)
                {
                    DataGridRow row = (DataGridRow) this.gr_result.ItemContainerGenerator.ContainerFromIndex(0);
                    object item = this.gr_result.Items[0];
                    this.gr_result.SelectedItem = item;
                    this.gr_result.SelectedValue = item;
                    this.gr_result.SelectedIndex = 0;
                    this.gr_result.CurrentItem = item;
                    this.gr_result.SelectedItems.Add(item);

                    DataGridRow gridRow = new DataGridRow();
                    gridRow.IsSelected = true;
                    gridRow.Item = item;
                    gridRow.DataContext = item;

                    MouseButtonEventArgs args = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) {RoutedEvent = MouseLeftButtonDownEvent};
                    this.DataGridRow_MouseDoubleClick(gridRow, args);
                }

                this.ChangeUiState(true);
            }), DispatcherPriority.ContextIdle, null);
        }

        private void Tb_splice_KeyUp(object sender, KeyEventArgs e)
        {
            if (this.Reg1.IsMatch(this.tb_splice.Text))
                try
                {
                    if (this.tb_splice.Text == "")
                        this.DataFileService.CurrentFile.Splices = null;

                    else
                        this.DataFileService.CurrentFile.Splices = int.Parse(this.tb_splice.Text);
                    this._technicalLogger.LogInfo("Change value", string.Format("You have changed the value of Splice to {0}", this.DataFileService.CurrentFile.Splices));
                }
                catch (InvalidCastException x)
                {
                    throw x;
                }
            else
                this.tb_splice.Text = this.DataFileService.CurrentFile.Splices.ToString();
        }

        private void Tb_defect_KeyUp(object sender, KeyEventArgs e)
        {
            if (this.Reg1.IsMatch(this.tb_defect.Text))
                try
                {
                    if (this.tb_defect.Text == "")
                        this.DataFileService.CurrentFile.Defect = null;

                    else
                        this.DataFileService.CurrentFile.Defect = int.Parse(this.tb_defect.Text);
                    this._technicalLogger.LogInfo("Change value", string.Format("You have changed the value of defect to {0}", this.DataFileService.CurrentFile.Defect));
                }
                catch (InvalidCastException x)
                {
                    throw x;
                }
            else
                this.tb_defect.Text = this.DataFileService.CurrentFile.Defect.ToString();
        }

        private void Tb_lbl_KeyUp(object sender, KeyEventArgs e)
        {
            if (this.Reg2.IsMatch(this.tb_lbl.Text))
            {
                this.DataFileService.CurrentFile.LBL = this.tb_lbl.Text;
                this._technicalLogger.LogInfo("Change value", string.Format("You have changed the value of LBL to {0}", this.DataFileService.CurrentFile.LBL));
            }
            else
            {
                this.tb_lbl.Text = this.DataFileService.CurrentFile.LBL;
            }
        }

        private void Tb_trtype_KeyUp(object sender, KeyEventArgs e)
        {
            if (this.Reg2.IsMatch(this.tb_trtype.Text))
            {
                this.DataFileService.CurrentFile.TrType = this.tb_trtype.Text;
                this._technicalLogger.LogInfo("Change value", string.Format("You have changed the value of TR-Type to {0}", this.DataFileService.CurrentFile.TrType));
            }
            else
            {
                this.tb_trtype.Text = this.DataFileService.CurrentFile.TrType;
            }
        }

        private void Tb_pn_KeyUp(object sender, KeyEventArgs e)
        {
            if (this.Reg2.IsMatch(this.tb_pn.Text))
            {
                this.DataFileService.CurrentFile.PN = this.tb_pn.Text;
                this._technicalLogger.LogInfo("Change value", string.Format("You have changed the value of PN to {0}", this.DataFileService.CurrentFile.PN));
            }
            else
            {
                this.tb_pn.Text = this.DataFileService.CurrentFile.PN;
            }
        }

        private void Tb_emp_KeyUp(object sender, KeyEventArgs e)
        {
            if (this.Reg2.IsMatch(this.tb_emp.Text))
            {
                this.DataFileService.CurrentFile.Emp = this.tb_emp.Text;
                this._technicalLogger.LogInfo("Change value", string.Format("You have changed the value of Emp to {0}", this.DataFileService.CurrentFile.Emp));
            }
            else
            {
                this.tb_emp.Text = this.DataFileService.CurrentFile.Emp;
            }
        }

        private void Tb_site_KeyUp(object sender, KeyEventArgs e)
        {
            if (this.Reg1.IsMatch(this.tb_site.Text))
            {
                this.DataFileService.CurrentFile.Site = this.tb_site.Text;
                this._technicalLogger.LogInfo("Change value", string.Format("You have changed the value of Site to {0}", this.DataFileService.CurrentFile.Site));
            }

            else
            {
                this.tb_site.Text = this.DataFileService.CurrentFile.Site;
            }
        }

        private void Tb_line_KeyUp(object sender, KeyEventArgs e)
        {
            if (this.Reg2.IsMatch(this.tb_line.Text))
            {
                this.DataFileService.CurrentFile.Line = this.tb_line.Text;
                this._technicalLogger.LogInfo("Change value", string.Format("You have changed the value of Line to {0}", this.DataFileService.CurrentFile.Line));
            }
            else
            {
                this.tb_line.Text = this.DataFileService.CurrentFile.Line;
            }
        }

        private void Tb_op_KeyUp(object sender, KeyEventArgs e)
        {
            if (this.Reg1.IsMatch(this.tb_op.Text))
                try
                {
                    if (this.tb_op.Text == "")
                        this.DataFileService.CurrentFile.OP = null;

                    else
                        this.DataFileService.CurrentFile.OP = int.Parse(this.tb_op.Text);
                    this._technicalLogger.LogInfo("Change value", string.Format("You have changed the value of OP to {0}", this.DataFileService.CurrentFile.OP));
                }
                catch (InvalidCastException x)
                {
                    throw x;
                }
            else
                this.tb_op.Text = this.DataFileService.CurrentFile.OP.ToString();
        }

        private void Tb_wc_KeyUp(object sender, KeyEventArgs e)
        {
            if (this.Reg2.IsMatch(this.tb_wc.Text))
            {
                this.DataFileService.CurrentFile.WC = this.tb_wc.Text;
                this._technicalLogger.LogInfo("Change value", string.Format("You have changed the value of WC to {0}", this.DataFileService.CurrentFile.WC));
            }
            else
            {
                this.tb_wc.Text = this.DataFileService.CurrentFile.WC;
            }
        }

        private void Tb_mhc_KeyUp(object sender, KeyEventArgs e)
        {
            if (this.Reg2.IsMatch(this.tb_mhc.Text))
            {
                this.DataFileService.CurrentFile.MCH = this.tb_mhc.Text;
                this._technicalLogger.LogInfo("Change value", string.Format("You have changed the value of MCH to {0}", this.DataFileService.CurrentFile.MCH));
            }
            else
            {
                this.tb_mhc.Text = this.DataFileService.CurrentFile.MCH;
            }
        }

        private void Tb_numbofconfparts_KeyUp(object sender, KeyEventArgs e)
        {
            if (this.Reg1.IsMatch(this.tb_numbofconfparts.Text))
            {
                this.DataFileService.CurrentFile.NumbOfConfParts = this.tb_numbofconfparts.Text;
                this._technicalLogger.LogInfo("Change value", string.Format("You have changed the value of Number of conform parts to {0}", this.DataFileService.CurrentFile.NumbOfConfParts));
            }
            else
            {
                this.tb_numbofconfparts.Text = this.DataFileService.CurrentFile.NumbOfConfParts;
            }
        }

        private void Tb_printer_KeyUp(object sender, KeyEventArgs e)
        {
            if (this.Reg2.IsMatch(this.tb_printer.Text))
            {
                this.DataFileService.CurrentFile.Printer = this.tb_printer.Text;
                this._technicalLogger.LogInfo("Change value", string.Format("You have changed the value of Printer to {0}", this.DataFileService.CurrentFile.Printer));
            }
            else
            {
                this.tb_printer.Text = this.DataFileService.CurrentFile.Printer;
            }
        }

        private void Tb_tapeN_KeyUp(object sender, KeyEventArgs e)
        {
            if (this.Reg2.IsMatch(this.tb_tapeN.Text))
            {
                this.DataFileService.CurrentFile.TapeN = this.tb_tapeN.Text;
                this._technicalLogger.LogInfo("Change value", string.Format("You have changed the value of Tape# to {0}", this.DataFileService.CurrentFile.TapeN));
            }
            else
            {
                this.tb_tapeN.Text = this.DataFileService.CurrentFile.TapeN;
            }
        }

        private void AddFileWithBrowse(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "(*.txt)|*.txt";

            try
            {
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string newFile = this.DataFileService.MoveToTODODirectory(dialog.FileName);

                    bool isOk = this.DataFileService.VerifFile(newFile);
                    if (!isOk)
                    {
                        this.DataFileService.DeleteFromTodoDirectory(newFile);
                        MessageBox.Show("The selected file is not a FI Station file", "Loading error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    this.DataFileService.LoadFileToProcess();
                    this.gr_result.ItemsSource = this.DataFileService.FilesToProcess;
                }
            }
            catch (Exception ex)
            {
                this._technicalLogger.LogError("Import File", "This File is not a valid FI Station");
                ex.ToString();
            }
        }

        private void EditPassWord(object sender, RoutedEventArgs e)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                AppSettingsReader config = new AppSettingsReader();
                string Password = config.GetValue("EditPassword", typeof(string)) as string;

                PasswordConfirm dialog = new PasswordConfirm();
                if (dialog.ShowDialog() == true)
                {
                    string pwd = dialog.PasswordResponse;
                    if (Password == pwd)
                    {
                        this.fileInfoReadOnly = false;
                        this.ChangeUiState(true);
                        this.edit.IsEnabled = false;
                        this._technicalLogger.LogInfo("Enable edit", "File edit enable by user");
                    }
                    else
                    {
                        this.ChangeUiState(false);
                        this.gr_result.IsEnabled = true;
                        this.btSend.IsEnabled = true;
                        MessageBox.Show("This password is not valid", "Password error", MessageBoxButton.OK, MessageBoxImage.Error);
                        this._technicalLogger.LogError("Enable edit", "Wrong password, can not enable file edit");
                    }
                }
            }), DispatcherPriority.ContextIdle, null);
        }

        private Version getRunningVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version;
        }
    }
}