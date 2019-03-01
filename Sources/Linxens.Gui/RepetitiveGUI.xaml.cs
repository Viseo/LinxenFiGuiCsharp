using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Linxens.Core.Service;

namespace Linxens.Gui
{
    /// <summary>
    ///     Logique d'interaction pour Page1.xaml
    /// </summary>
    public partial class RepetitiveGUI : Window
    {
        public RepetitiveGUI()
        {
            this.InitializeComponent();


            this.DataFileService = new DataFileService();

            this.gr_result.ItemsSource = this.DataFileService.FilesToProcess;
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
            DataGridRow sdr = (DataGridRow) sender;
            string file = sdr.DataContext.ToString();


            this.DataFileService.ReadFile(file);

            // var result2 = service.ReadFileScrap(file);
            this.tb_site.Text = this.DataFileService.CurrentFile.Site;
            this.tb_emp.Text = this.DataFileService.CurrentFile.Emp;
            this.tb_trtype.Text = this.DataFileService.CurrentFile.TrType;
            this.tb_line.Text = this.DataFileService.CurrentFile.Line;
            this.tb_pn.Text = this.DataFileService.CurrentFile.PN;
            this.tb_op.Text = this.DataFileService.CurrentFile.OP.ToString();
            this.tb_wc.Text = this.DataFileService.CurrentFile.WC;
            this.tb_mhc.Text = this.DataFileService.CurrentFile.MCH;
            this.tb_lbl.Text = this.DataFileService.CurrentFile.LBL;

            //tb_tape.Text = DataFileService.CurrentFile.Tape;
            this.tb_qty.Text = this.DataFileService.CurrentFile.Qty;

            this.tb_defect.Text = this.DataFileService.CurrentFile.Defect.ToString();
            this.tb_splice.Text = this.DataFileService.CurrentFile.Splices.ToString();
            this.tb_date.Text = this.DataFileService.CurrentFile.DateTapes;
            this.tb_printer.Text = this.DataFileService.CurrentFile.Printer;
            this.tb_numbofconfparts.Text = this.DataFileService.CurrentFile.NumbOfConfParts;


            this.gr_scraps.ItemsSource = this.DataFileService.CurrentFile.Scrap.ToArray();
            this.gr_scraps.Columns.RemoveAt(0);
            //DataGridColumn column = gr_scraps.Columns[0];
        }
    }
}