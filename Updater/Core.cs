using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;

namespace Updater
{
    public static class Core
    {

        public static Char SplitChar = Convert.ToChar("|");
        public static string DecryptPassword = "FalseX";
        public static void SetAllCommandTimeouts(object adapter, int timeout)
        {
            var commands = adapter.GetType().InvokeMember("CommandCollection", System.Reflection.BindingFlags.GetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                    null, adapter, new object[0]);
            var sqlCommand = (IDbCommand[])commands;
            foreach (var cmd in sqlCommand)
            {
                cmd.CommandTimeout = timeout;
            }
        }
        public static string ZZReadMappedMemoryData()
        {
            string Data = string.Empty;
            try
            {
                using (System.IO.MemoryMappedFiles.MemoryMappedFile mmf = System.IO.MemoryMappedFiles.MemoryMappedFile.OpenExisting("NICSQLToolsUpdateMap"))
                {
                    System.Threading.Mutex mutex = System.Threading.Mutex.OpenExisting("NICSQLToolsUpdateMapMutex");
                    using (System.IO.MemoryMappedFiles.MemoryMappedViewStream stream = mmf.CreateViewStream())
                    {
                        System.IO.BinaryReader reader = new System.IO.BinaryReader(stream);
                        Data = reader.ReadString();
                    }
                    mutex.WaitOne();
                }
                if (Data == string.Empty)
                    System.Diagnostics.Process.GetCurrentProcess().Kill();
                Properties.Settings.Default["DatabaseConnectionString"] = Data.Split(Convert.ToChar("||"))[0];
            }
            catch (System.IO.FileNotFoundException)
            {
                Console.WriteLine("Memory-mapped file does not exist. Run Process A first.");
            }
            return Data.Split(Convert.ToChar("||"))[1];
        }
        public static byte[] GetFileData(string FilePath)
        {
            byte[] output = null;

            string FileName = System.IO.Path.GetFileName(FilePath);
            SqlConnection con = new SqlConnection(Properties.Settings.Default.DatabaseConnectionString);
            string commandString = String.Format("SELECT FileData FROM dbo.AppDependenceFile WHERE FileName = '{0}'", FileName);
            SqlCommand cmd = new SqlCommand(commandString, con) { CommandTimeout = 0 };
            cmd.CommandTimeout = 0;
            try
            {
                con.Open();
                object obj = cmd.ExecuteScalar();
                if (obj != null)
                    output = (byte[])obj;
            }
            catch (SqlException ex)
            {
                throw ex;
            }
            con.Close();

            return output;
        }
        public static bool SetFileData(string FileName, Int64 FileVersion, byte[] FileData)
        {
            bool output = false;

            //string FileName = System.IO.Path.GetFileName(FilePath);
            SqlConnection con = new SqlConnection(Properties.Settings.Default.DatabaseConnectionString);
            SqlCommand cmd = new SqlCommand(@"IF EXISTS(SELECT * FROM dbo.AppDependenceFile WHERE [FileName] = @FileName)
            UPDATE dbo.AppDependenceFile SET FileVersion = @FileVersion, FileData = @FileData WHERE [FileName] = @FileName
            ELSE
            INSERT INTO dbo.AppDependenceFile ([FileName], FileVersion, FileData) VALUES(@FileName, @FileVersion, @FileData)", con) { CommandTimeout = 0 };
            SqlParameter paramFileName = new SqlParameter("@FileName", SqlDbType.NVarChar);
            SqlParameter paramFileVersion = new SqlParameter("@FileVersion", SqlDbType.BigInt);
            SqlParameter paramFileData = new SqlParameter("@FileData", SqlDbType.Image);
            cmd.Parameters.AddRange(new SqlParameter[] { paramFileName, paramFileVersion, paramFileData });
            try
            {
                paramFileName.Value = FileName;
                paramFileVersion.Value = FileVersion;
                paramFileData.Value = FileData;
                con.Open();
                cmd.ExecuteNonQuery();
                output = true;
            }
            catch (SqlException ex)
            {
                throw ex;
            }
            con.Close();

            return output;
        }

        public enum UpdaterArgsEnum
        {
            Upload = 1,
            Download = 2
        }
        public static Queue<string> ReadDownloadArgs(string arg)
        {
            Queue<string> output = new Queue<string>();
            string[] Data = arg.Split(SplitChar);
            Properties.Settings.Default["DatabaseConnectionString"] = Data[0];
            string ApplicationDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            for (int i = 1; i < Data.Length; i++)
                output.Enqueue(String.Format(@"{0}\{1}", ApplicationDir, Data[i]));

            return output;
        }
        public static Queue<KeyValuePair<string, Int64>> ReadUploadArgs(string arg)
        {
            Queue<KeyValuePair<string, Int64>> output = new Queue<KeyValuePair<string, Int64>>();
            string[] Data = arg.Split(SplitChar);
            Properties.Settings.Default["DatabaseConnectionString"] = Data[0];
            string ApplicationDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            for (int i = 1; i < Data.Length; i = i + 2)
                output.Enqueue(new KeyValuePair<string, Int64>(String.Format(@"{0}\{1}", ApplicationDir, Data[i]), Convert.ToInt64(Data[i + 1])));
            return output;

        }

    }
}
