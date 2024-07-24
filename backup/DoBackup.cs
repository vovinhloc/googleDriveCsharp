using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Xml.Linq;

namespace backup
{
    internal class DoBackup
    {
        private DebugLogs debugLogs = new DebugLogs();
        GGDriveAPIToken googleOAuth20 = new GGDriveAPIToken();
        private SqlDb sqlDb = new SqlDb();
        private  string connectionString = "Server=DESKTOP-TA73QS1\\SQLEXPRESS;Database=sipbpxv3_v37_autocall;Integrated Security=True;TrustServerCertificate=True;";
        private  string backupPath = @"C:\backup\mssql";
        private  string dbName = "sipbpxv3_v37_autocall";
        private  string serverName = "";

        public DoBackup()
        {
            serverName = ConfigurationManager.AppSettings["ServerName"];
            dbName = ConfigurationManager.AppSettings["DatabaseName"];
            backupPath = ConfigurationManager.AppSettings["BackupPath"];
        }
        public void thuchienBackup(string bkType)
        {
            Console.WriteLine(bkType);
            string dbFilePathName = "";
            switch (bkType.ToLower()) // Chuyển về chữ thường để so sánh không phân biệt hoa thường
            {
                case "full":
                    Console.WriteLine("backup full");
                    Console.WriteLine("PerformFullBackup");
                    dbFilePathName = PerformFullBackup();
                    Console.WriteLine(dbFilePathName);
                    // Thêm logic để thực hiện backup full ở đây
                    break;
                case "diff":
                    Console.WriteLine("backup diff");

                    // Thêm logic để thực hiện backup diff ở đây
                    Console.WriteLine("PerformDiffBackup");
                    dbFilePathName = PerformDiffBackup();

                    break;
                case "log":
                    Console.WriteLine("backup log");
                    Console.WriteLine("PerformLogBackupIfPossible");
                    dbFilePathName=PerformLogBackupIfPossible();
                    // Thêm logic để thực hiện backup log ở đây
                    break;
                default:
                    Console.WriteLine("default");
                    dbFilePathName=bkDefault();
                    break;
            }
            Console.WriteLine("chu bi upload file =" +dbFilePathName);
            UploadBKFiletoGoogle(dbFilePathName);
        }
         string PerformFullBackup()
        {
            string backupFile = $"{backupPath}\\FullBackup_{DateTime.Now:yyyyMMddHHmmss}.bak";
            string bkCMD = $"BACKUP DATABASE {dbName} TO DISK = '{backupFile}'";
            debugLogs.writeLog("1.Full backup| bkCMD = " + bkCMD);
            sqlDb.ExecuteBackup(bkCMD);            
            debugLogs.writeLog("Full backup completed.|" + backupFile);
            
            return backupFile;
        }
        string PerformDiffBackup()
        {
            string backupFile = $"{backupPath}\\DiffBackup_{DateTime.Now:yyyyMMddHHmmss}.bak";
            string bkCMD = $"BACKUP DATABASE  {dbName} TO DISK = '{backupFile}' WITH DIFFERENTIAL";
            debugLogs.writeLog(backupFile);
            sqlDb.ExecuteBackup(bkCMD);
            
            debugLogs.writeLog("Differential backup completed.|" + backupFile);
            return backupFile;
        }

        string PerformLogBackupIfPossible()
        {
            //ALTER DATABASE YourDatabaseName SET RECOVERY FULL;
            string recoveryModel = sqlDb.GetDatabaseRecoveryModel();
            debugLogs.writeLog("recoveryModel=[" + recoveryModel + "]");
            string backupFile = Path.Combine(backupPath, $"LogBackup_{DateTime.Now:yyyyMMddHHmmss}.trn");

            if (recoveryModel != "SIMPLE")
            {
                
                string cmd= $"BACKUP LOG {dbName} TO DISK = '{backupFile}'";
                debugLogs.writeLog( "Backup Log, cmd= " + cmd);
                sqlDb.ExecuteBackup(cmd);
                
                debugLogs.writeLog("Log backup completed.|" + backupFile);
            }
            else
            {
                
                debugLogs.writeLog("Log backup skipped. Database is in SIMPLE recovery model.");
            }
            return backupFile;
        }

        string bkDefault()
        {
            string dbFilePathName;
            if (DateTime.Now.DayOfWeek == DayOfWeek.Sunday)
            {
                
                dbFilePathName = PerformFullBackup();
                
            }
            else
            {
                
                dbFilePathName = PerformDiffBackup();
            }            
            return dbFilePathName;
            
        }

        void UploadBKFiletoGoogle(string filePathName)
        {
            processUpload(filePathName);
            //debugLogs.writeLog("UploadBKFiletoGoogle, filePathName= " + filePathName);
            //GGDriveAPIToken googleOAuth20 = new GGDriveAPIToken();
            //string folderID = ConfigurationManager.AppSettings["ggRootFolder"];
            //debugLogs.writeLog("folderID_UploadFolder= " + folderID);
            //string fileID = googleOAuth20.UploadFile(folderID, filePathName);
            //debugLogs.writeLog("fileID= " + fileID);
            //debugLogs.writeLog("UploadBKFiletoGoogle : DONE");
        }
        private string getFolderID(string folderID, string fdName)
        {
            string folderIDSearch = googleOAuth20.FindFolderIdByName(folderID, fdName);
            if (folderIDSearch == "")
            {
                folderIDSearch = googleOAuth20.CreateFolder(folderID, fdName);
            }
            
            return folderIDSearch;
        }
        private string processUpload(string RecordFile)
        {
            string kq = "";
            

            ProcessFile processFile = new ProcessFile();

            if (processFile.checkFileExisted(RecordFile))
            {
                string folderID = ConfigurationManager.AppSettings["ggRootFolder"];
                string year = $"{DateTime.Now:yyyy}";
                string month = $"{DateTime.Now:MM}";

                string folderIDSearch = getFolderID(folderID, year);
                folderIDSearch = getFolderID(folderIDSearch, month);

                string fileID;

                if (folderIDSearch != "")
                {
                    debugLogs.writeLog($"folderIDSearch={folderIDSearch}");
                    debugLogs.writeLog($"RecordFile={RecordFile}");
                    fileID = googleOAuth20.UploadFile(folderIDSearch, RecordFile);
                    kq = fileID;
                    debugLogs.writeLog($"uploaded, fileID={fileID}");
                } else
                {
                    debugLogs.writeLog($"folderIDSearch [{folderIDSearch}] khong ton tai");
                }
                               
            }else
            {
                debugLogs.writeLog($"file [{RecordFile}] khong ton tai");
            }

            return kq;
        }
    }
}
