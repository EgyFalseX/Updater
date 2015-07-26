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
        //public static Queue<string> FilesList { get; set; }

        public MainWindow()
        {
            //FilesList = new Queue<string>();

            InitializeComponent();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!ReadArgs())
                System.Diagnostics.Process.GetCurrentProcess().Kill();
            
        }
        private bool ReadArgs()
        {
            // Args:
            // 1- Temp String
            // 2- Opcode (int)
            // 3- Connection String (string)
            // 4- Files Split With | (string)
            try
            {
                if (Environment.GetCommandLineArgs().Length <= 1)
                {
                    MessageBox.Show("No Data Found ...", "Shutdown", MessageBoxButton.OK, MessageBoxImage.Error);
                    System.Diagnostics.Process.GetCurrentProcess().Kill();
                }
                //string arg = FXFW.EncDec.Decrypt(Environment.GetCommandLineArgs()[1], Core.DecryptPassword);
                
                string arg = Environment.GetCommandLineArgs()[1];

                int Opcode = Convert.ToInt32(arg.Split(Core.SplitChar)[0]);// Extract Opcode
                
                string command = arg.Substring(2, arg.Length - 2);
                
                switch (Opcode)
                {
                    case (int)Core.UpdaterArgsEnum.Download:// 2
                        Queue<string> FilesList = (Queue<string>)Core.ReadDownloadArgs(command);
                        pbEst.Maximum = FilesList.Count; pbEst.Value = 0;//Update UI
                        GetDownloadData(FilesList);
                        break;
                    case (int)Core.UpdaterArgsEnum.Upload:// 1
                        Queue<KeyValuePair<string, int>> UploadData = Core.ReadUploadArgs(command);
                        pbEst.Maximum = UploadData.Count; pbEst.Value = 0;//Update UI
                        SetUploadData(UploadData);
                        break;
                    default:
                        System.Diagnostics.Process.GetCurrentProcess().Kill();
                        break;
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "error", MessageBoxButton.OK, MessageBoxImage.Information);
                App.Current.Dispatcher.InvokeShutdown();
            }
            return false;
        }
        private void GetDownloadData(Queue<string> Data)
        {
            Queue<string> FilesList = Data;

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
                GetDownloadData(FilesList);// pool Next File
                
            });
        }
        private void SetUploadData(Queue<KeyValuePair<string, int>> Data)
        {
            if (Data.Count == 0)// All Files Uploaded, Application Will Shutdown
            {
                MessageBox.Show("All Files Uploaded ... Application Will Shutdown", "Operation Ended", MessageBoxButton.OK, MessageBoxImage.Information);
                App.Current.Dispatcher.InvokeShutdown();
                return;
            }
            System.Threading.ThreadPool.QueueUserWorkItem((o) =>
            {
                KeyValuePair<string, int> value = Data.Dequeue();
                Dispatcher.Invoke(new Action(() => { lblFileName.Content = System.IO.Path.GetFileName(value.Key); }));

                if (Core.SetFileData(System.IO.Path.GetFileName(value.Key), value.Value, System.IO.File.ReadAllBytes(value.Key)))
                {
                    Dispatcher.Invoke(new Action(() => { pbEst.Value = Convert.ToInt32(pbEst.Value) + 1; }));
                    SetUploadData(Data);// pool Next File
                }
                else
                {
                    MessageBox.Show(String.Format("{0}{1}Failed to save file to Database", value.Key, Environment.NewLine), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    System.Diagnostics.Process.GetCurrentProcess().Kill();
                }
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
