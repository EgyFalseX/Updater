using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Updater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static Queue<string> FilesList { get; set; }
        public MainWindow()
        {
            FilesList = new Queue<string>();

            InitializeComponent();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ReadArgs();
            //Update UI
            pbEst.Maximum = FilesList.Count;
            pbEst.Value = 0;
            //Get Data From Database
            GetUpdateData();
        }
        private void ReadArgs()
        {
            try
            {
                if (Environment.GetCommandLineArgs().Length <= 1)
                {
                    MessageBox.Show("No Data Found ...", "Shutdown", MessageBoxButton.OK, MessageBoxImage.Error);
                    App.Current.Dispatcher.InvokeShutdown();
                }
                string arg = Environment.GetCommandLineArgs()[1];

                string[] Data = FXFW.EncDec.Decrypt(arg, "FalseX").Split(Convert.ToChar("|"));
                Properties.Settings.Default["DatabaseConnectionString"] = Data[0];
                string ApplicationDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                for (int i = 1; i < Data.Length; i++)
                    FilesList.Enqueue(String.Format(@"{0}\{1}", ApplicationDir, Data[i]));   
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "error", MessageBoxButton.OK, MessageBoxImage.Information);
                App.Current.Dispatcher.InvokeShutdown();
            }
            
        }
        private void GetUpdateData()
        {
            if (FilesList.Count == 0)// All Files Downloaded, Application Will Shutdown
            {
                MessageBox.Show("All Files Downloaded ... Application Will Shutdown", "Operation Ended", MessageBoxButton.OK, MessageBoxImage.Information);
                App.Current.Dispatcher.InvokeShutdown();
                return;
            }

            System.Threading.ThreadPool.QueueUserWorkItem((o) =>
            {
                string FileName = FilesList.Dequeue();
                Dispatcher.Invoke(new Action(() => { lblFileName.Content = System.IO.Path.GetFileName(FileName); }));
                
                byte[] buffer = (byte[])Core.GetFileData(FileName);

                if (System.IO.File.Exists(FileName))
                    System.IO.File.Delete(FileName);
                System.IO.FileStream fs = new System.IO.FileStream(FileName, System.IO.FileMode.CreateNew);
                fs.Write(buffer,0, buffer.Length);
                fs.Close();

                Dispatcher.Invoke(new Action(() => { pbEst.Value = Convert.ToInt32(pbEst.Value) + 1; }));
                GetUpdateData();
                
            });
        }
        private void Test()
        {
            string ApplicationDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            List<string> filePaths = new List<string>();
            filePaths.Add(ApplicationDir + @"\DevExpress.BonusSkins.v14.1.dll");
            filePaths.Add(ApplicationDir + @"\DevExpress.Dashboard.v14.1.Core.dll");
            filePaths.Add(ApplicationDir + @"\DevExpress.RichEdit.v14.1.Core.dll");
            filePaths.Add(ApplicationDir + @"\DevExpress.Utils.v14.1.dll");

            Datasources.dsDataTableAdapters.QueriesTableAdapter adp = new Datasources.dsDataTableAdapters.QueriesTableAdapter();
            Core.SetAllCommandTimeouts(adp, 0);

            foreach (string item in filePaths)
            {
                string FileName = System.IO.Path.GetFileName(item);

                byte[] buffer = System.IO.File.ReadAllBytes(item);
                
                adp.AddFileData(FileName, 1000, buffer);
            }
            
            
            
            
        }

    }
}
