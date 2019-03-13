using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Linxens.Core.Service;
using System.Linq;
using System.Collections.ObjectModel;
using Linxens.Core.Model;
using System.Collections.Generic;
using System;
using System.Windows.Data;
using System.Globalization;

namespace Linxens.Gui
{
    /// <summary>
    ///     Logique d'interaction pour Page1.xaml
    /// </summary>
    public partial class RepetitiveGUI : Window
    {
        public RepetitiveGUI()
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.InitializeComponent();

            this.DataFileService = new DataFileService();
            this.gr_result.ItemsSource = this.DataFileService.FilesToProcess;
        }

        public DataFileService DataFileService { get; set; }

        
        private void DataGridRow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            this.Statut.Background = System.Windows.Media.Brushes.Green;
            Statut.Text = "READY";
            this.DataFileService._technicalLogger.LogInfo("Status", "The status is going to ready. File selected is ready for submission");
            ;
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


            //_test = DataFileService.CurrentFile.Scrap;
            gr_scraps.ItemsSource = DataFileService.CurrentFile.Scrap.ToArray();
            //gr_scraps.Items.Refresh();
            //this.gr_scraps.Columns.RemoveAt(0);
            //this.gr_scraps.ItemsSource = this.DataFileService.CurrentFile.Scrap.ToArray();



        }
        
        private void RemoveScrap(object sender, RoutedEventArgs e)
        {
            int i = 0;
            var test = DataFileService.CurrentFile.Scrap;
            int ItemsSelected = this.gr_scraps.SelectedItems.Count;
            if (gr_result.SelectedItem == null){
                MessageBox.Show("Select a file!");
                DataFileService._technicalLogger.LogWarning("Select File", "You dont have selected a file");
            }
            else if (gr_scraps.SelectedItem == null)
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
                        if (this.gr_scraps.SelectedItems[i] is Quality itm)
                        {
                            if (messageBoxResult == MessageBoxResult.Yes)
                                test.Remove(itm);
                            i++;
                            DataFileService._technicalLogger.LogInfo("Delete Scrap", $"Line Scrap number {i} is deleted successfully");
                        }
                    }
                    tb_qty.Text = DataFileService.CurrentFile.Qty;
                    this.gr_scraps.ItemsSource = this.DataFileService.CurrentFile.Scrap.ToArray();
                }
            }
        }
        
        private void AddScrap(object sender, RoutedEventArgs e)
        {
            if (gr_result.SelectedItem == null)
            {
                MessageBox.Show("You can not add scrap to a non-existent file. Please select a file!");
                this.DataFileService._technicalLogger.LogWarning("Add line for Scap", "You haven't select a file in the file directory for add it a scrap");
            }
            else
            {
                int i = DataFileService.CurrentFile.Scrap.Count;
                var c = DataFileService.CurrentFile.Scrap;
                c.Add(new Quality
                {
                    Qty = "",//DataFileService.CurrentFile.Scrap[i - 1].Qty,
                    RsnCode ="",
                    Tape = DataFileService.CurrentFile.Scrap[i-1].Tape
                    
                });
                i++;
                DataFileService.CurrentFile.Scrap = c;
                gr_scraps.CurrentItem = c;
                DataFileService._technicalLogger.LogInfo("Add Line for Scrap", "You have add line for a new scrap");
                this.gr_scraps.ItemsSource = this.DataFileService.CurrentFile.Scrap.ToArray();
                tb_qty.Text = DataFileService.CurrentFile.Qty;
            }
        }

        private static object GetCellValue(DataGridCellInfo cell)
        {
            var boundItem = cell.Item;
            var binding = new Binding();
            if (cell.Column is DataGridTextColumn)
            {
                binding = ((DataGridTextColumn)cell.Column).Binding as Binding;
            }
            else if (cell.Column is DataGridCheckBoxColumn)
            {
                binding = ((DataGridCheckBoxColumn)cell.Column).Binding as Binding;
            }
            else if (cell.Column is DataGridComboBoxColumn)
            {
                binding = ((DataGridComboBoxColumn)cell.Column).SelectedValueBinding as Binding;

                if (binding == null)
                {
                    binding = ((DataGridComboBoxColumn)cell.Column).SelectedItemBinding as Binding;
                }
            }

            if (binding != null)
            {
                var propertyName = binding.Path.Path;
                var propInfo = boundItem.GetType().GetProperty(propertyName);
                return propInfo.GetValue(boundItem, new object[] { });
            }

            return null;
        }
        private void Gr_scraps_LostFocus(object sender, RoutedEventArgs e)
        {
            //tb_qty.Text = DataFileService.CurrentFile.Qty;
            var HoldValueCell = DataFileService.CurrentFile.InitialQty;
            var cell = (e.OriginalSource as DataGridCell);

            if (cell == null)
            {
                var test = (e.OriginalSource as TextBox);
                var val = test.Text;
                var scrap = DataFileService.CurrentFile.Scrap.FirstOrDefault(s => s.Qty == "");
                if (scrap == null)
                {
                    var test2 = gr_scraps.Items.SourceCollection;
                    return;
                }

                scrap.Qty = val;
                tb_qty.Text = DataFileService.CurrentFile.Qty;
            }
            else if (cell != null && (string)cell.Column.Header == "Qty")
            {
                var tbvalue = DataFileService.CurrentFile.Qty;
                var txtHoldvalue = HoldValueCell.ToString(CultureInfo.InvariantCulture);
                if (tbvalue != txtHoldvalue)
                {
                    tb_qty.Text = tbvalue;
                }
                else
                {
                    tb_qty.Text = txtHoldvalue;
                }
                //var test = cell.Content.ToString();
                //string strValue = GetCellValue(new DataGridCellInfo(cell)).ToString();
                //var cellValue = float.Parse(strValue, CultureInfo.InvariantCulture);
                //var textBoxValue = float.Parse(tb_qty.Text, CultureInfo.InvariantCulture);
                //var finalValue = textBoxValue + cellValue;
                // tb_qty.Text = finalValue.ToString(CultureInfo.InvariantCulture);

            }

        }

        
    }
}