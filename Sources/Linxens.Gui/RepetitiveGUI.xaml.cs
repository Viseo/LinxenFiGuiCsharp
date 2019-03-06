using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Linxens.Core.Service;
using System.Linq;
using System.Collections.ObjectModel;
using Linxens.Core.Model;
using System.Collections.Generic;

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
           // remove.IsEnabled = false;
        }

        public DataFileService DataFileService { get; set; }


        private void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            //using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            //{
            //System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            //    if (result == System.Windows.Forms.DialogResult.OK)
            //    {

            //        CheckDirectoryStrucuture(dialog.SelectedPath, true);
            //       // tb_todo.Text = Path.Combine(dialog.SelectedPath, TODO);
            //        ReadTodoDirectory(Path.Combine(dialog.SelectedPath, TODO));
            //    }
            //}
        }

        private void DataGridRow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            this.Statut.Background = System.Windows.Media.Brushes.Green;
            Statut.Text = "READY";
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
            this.tb_date.Text = this.DataFileService.CurrentFile.DateTapes;
            this.tb_printer.Text = this.DataFileService.CurrentFile.Printer;
            this.tb_numbofconfparts.Text = this.DataFileService.CurrentFile.NumbOfConfParts;
          
            this.gr_scraps.Columns.RemoveAt(0);
            this.gr_scraps.ItemsSource = this.DataFileService.CurrentFile.Scrap.ToArray();
            
        }
        
        private void RemoveScrap(object sender, RoutedEventArgs e)
        {
            int i = 0;
            var test = DataFileService.CurrentFile.Scrap;
            int SelectedItems = this.gr_scraps.SelectedItems.Count;

            if (gr_result.SelectedItem == null)
                MessageBox.Show("Select a file!");

            else if (gr_scraps.SelectedItem == null)
                MessageBox.Show("select a scrap to delete it!");

            else
            {
                MessageBoxResult messageBoxResult = MessageBox.Show("Are you sure?", "Delete Confirmation", MessageBoxButton.YesNo);
                List<Quality> items = this.gr_scraps.SelectedItems.Cast<Quality>().ToList();
                if (test != null)
                {
                    while (i < SelectedItems)
                    {
                        Quality itm = this.gr_scraps.SelectedItems[i] as Quality;
                        if (itm != null)
                        {
                            if (messageBoxResult == MessageBoxResult.Yes)
                                test.Remove(itm);
                            i++;
                        }
                    }
                    this.gr_scraps.ItemsSource = this.DataFileService.CurrentFile.Scrap.ToArray();
                }
            }    
        }
        
        private void AddScrap(object sender, RoutedEventArgs e)
        {
            if (gr_result.SelectedItem == null)
            {
                MessageBox.Show("You can not add scrap to a non-existent file. Please select a file!");
            }
            else
            {
                var c = DataFileService.CurrentFile.Scrap;
                c.Add(new Quality
                {
                    Qty = "",
                    RsnCode = "",
                    Tape = ""
                });
                DataFileService.CurrentFile.Scrap = c;
                gr_scraps.CurrentItem = c;
                this.gr_scraps.ItemsSource = this.DataFileService.CurrentFile.Scrap.ToArray();
            }  
        }
    }
}