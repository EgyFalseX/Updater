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
            SqlCommand cmd = new SqlCommand(commandString, con);
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
        public enum UpdaterArgsEnum
        {
            Upload = 1,
            Download = 2
        }
    }
}
