using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.IO;

namespace backup
{
    class DebugLogs
    {
        private object _lock = new object();
        public string _logFilePath = @"D:\Logs\Log.txt"; // Thay đổi đường dẫn tệp log
        public string keylogs = "";
        public DebugLogs()
        {
            Random random = new Random();

            // Tạo số nguyên ngẫu nhiên có 3 chữ số
            int randomNumber = random.Next(100, 1000);
            keylogs = DateTime.Now.ToShortTimeString();
            keylogs = keylogs.Replace(":", "_");
            keylogs = keylogs.Replace(" ", "_");
            keylogs += "_" + randomNumber.ToString();
        }

        public void writeDebugLog(string message)
        {
            lock (_lock)
            {
                using (StreamWriter sw = File.AppendText(_logFilePath))
                {
                    try
                    {
                        sw.WriteLine($"{DateTime.Now} - {message}");
                    }
                    catch (Exception ex)
                    {
                        sw.WriteLine($"{DateTime.Now} - {ex.Message}");
                    }
                }
            }

        }
        public void checkExistFolder(string savetofolderfile)
        {
            string path = "";
            string[] serverpathfilea = savetofolderfile.Split('\\');
            int len = serverpathfilea.Length;


            StringBuilder pathStrbd = new StringBuilder();
            pathStrbd.Append(serverpathfilea[0]);
            for (int i = 1; i < len; i++)
            {
                pathStrbd.Append(@"\" + serverpathfilea[i]);

                path = pathStrbd.ToString();
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }


        }
        public string setLogFilePath2()
        {
            try
            {
                //string logPath = "";// Properties.Settings.Default.logPath;
                string logPath = ConfigurationManager.AppSettings["logPath"];
                logPath = logPath.Trim();
                if (logPath == "")
                {
                    string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    logPath = currentDirectory[0] + ":\\locvvLogs";

                }
                DateTime dateTime = DateTime.Today;
                string yearmonthstr = dateTime.ToString("/yyyy/MM/");
                string datetimestr = dateTime.ToString("yyyy-MM-dd");

                logPath += yearmonthstr.Replace('/', '\\');


                checkExistFolder(logPath);
                logPath += datetimestr + $"_{keylogs}_log.txt";

                return logPath;
            }
            catch (Exception ex)
            {
                return "";
            }
        }
        public void writeLog(string message)
        {
            lock (_lock)
            {

                _logFilePath = setLogFilePath2();
                using (StreamWriter sw = File.AppendText(_logFilePath))
                {
                    try
                    {
                        sw.WriteLine($"{DateTime.Now} - {message}");
                    }
                    catch (Exception ex)
                    {
                        sw.WriteLine($"{DateTime.Now} - {ex.Message}");
                    }
                }
            }
            // WriteLog1(message);
        }
    }
}
