using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ClarionDatConnector;
using Microsoft.Win32;

namespace ClarionDatabaseViewer_gui
{
    /// <summary>
    /// Sample Usage Application for the ClarionDatConnector library. It creates a gui with all the data in an excel-like view
    /// </summary>
    public partial class MainWindow : Window
    {
        public ClarionFileData clarionFile { get; set; }    
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DataTable dt = clarionFile.ClarionData;
            StringBuilder sb = new StringBuilder();

            IEnumerable<string> columnNames = dt.Columns.Cast<DataColumn>().
                                              Select(column => column.ColumnName);
            sb.AppendLine(string.Join(";", columnNames));

            foreach (DataRow row in dt.Rows)
            {
                IEnumerable<string> fields = row.ItemArray.Select(field => field.ToString());
                sb.AppendLine(string.Join(";", fields));
            }

            File.WriteAllText("c:\\temp\\test.csv", sb.ToString());
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
                this.tb_PLIK.Text = openFileDialog.FileName;
        }

        private void bt_Zaladuj_Click(object sender, RoutedEventArgs e)
        {
            clarionFile = new ClarionFileData(this.tb_PLIK.Text);
            var k = clarionFile.GetData();
            //var i = clarionFile.ClarionData.DefaultView;
            this.DataContext = clarionFile.ClarionData;

        }
    }
}
