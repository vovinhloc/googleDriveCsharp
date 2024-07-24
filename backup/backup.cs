using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.CommandLine;
//using System.CommandLine.Invocation;
//using System.CommandLine.NamingConventionBinder;

using System.Data.SqlClient;
using System.Configuration;
using System.IO;
using Microsoft.IdentityModel.Tokens;
//https://claude.ai/chat/1936f570-7422-4df8-8be8-b84991c15e5f

namespace backup
{
    internal class backup
    {
        static DebugLogs debugLogs = new DebugLogs();
        private static  string connectionString = "Server=DESKTOP-TA73QS1\\SQLEXPRESS;Database=sipbpxv3_v37_autocall;Integrated Security=True;TrustServerCertificate=True;";
        private  static string backupPath = @"C:\backup\mssql";
        private static string dbName = "sipbpxv3_v37_autocall";
        private static string serverName = "";
        static int Main(string[] args)
        {
            string dbFilePathName = "";
            // Tạo một option để chọn loại backup
            var bkTypeOption = new Option<string>(
                "--bkType",
                "Loai backup: full, diff, hoặc log"
            )
            {
                IsRequired = false // Yêu cầu người dùng nhập loại backup
            };

            // Tạo root command
            var rootCommand = new RootCommand("thuc hien backup SQL Server");

            // Thêm option vào root command
            rootCommand.AddOption(bkTypeOption);

            // Xử lý khi command được gọi
            rootCommand.SetHandler((string bkType) =>
            {

                try
                {

                    

                    if (bkType.IsNullOrEmpty())
                    {                    
                        bkType = "";
                    }
                    DoBackup doBackup = new DoBackup();
                    doBackup.thuchienBackup(bkType);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }, bkTypeOption); // Truyền option vào handler

            // Chạy command
            return rootCommand.Invoke(args);
        }
        static int Main_error(string[] args)
        {
            string dbFilePathName = "";
            // Tạo một option để chọn loại backup
            var bkTypeOption = new Option<string>(
                "--bkType",
                "Loai backup: full, diff, hoặc log"
            )
            {
                IsRequired = false // Yêu cầu người dùng nhập loại backup
            };

            // Tạo root command
            var rootCommand = new RootCommand("thuc hien backup SQL Server");

            // Thêm option vào root command
            rootCommand.AddOption(bkTypeOption);

            // Xử lý khi command được gọi
            rootCommand.SetHandler((string bkType) =>
            {
                
                try
                {
                    
                    serverName = ConfigurationManager.AppSettings["ServerName"];
                    dbName = ConfigurationManager.AppSettings["DatabaseName"];
                    backupPath = ConfigurationManager.AppSettings["BackupPath"];
                    SqlDb sqlDb = new SqlDb();
                    connectionString = sqlDb.getConnStr();

                    if (bkType.IsNullOrEmpty())
                    {
                        Console.WriteLine("bkType  is null");
                        bkType = "";
                    }
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
                            PerformLogBackupIfPossible();
                            // Thêm logic để thực hiện backup log ở đây
                            break;
                        default:
                            Console.WriteLine("default");
                            bkDefault();
                            break;
                    }
                    UploadBKFiletoGoogle(dbFilePathName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }, bkTypeOption); // Truyền option vào handler

            // Chạy command
            return rootCommand.Invoke(args);
        }
        //static void Main_old(string[] args)
        //{
        //    // Tạo một command
        //    var command = new RootCommand
        //{
        //    new Option<string>("--bkType", "full,diff,log")
            
        //};
        //    // Xử lý sự kiện khi command được gọi
        //    command.Handler = System.CommandLine.Invocation.CommandHandler.Create<string>((bkType) =>
        //    {
        //        // Kiểm tra xem ngày bắt đầu và ngày kết thúc có tồn tại không
        //        switch (bkType) {
        //            case "full":
        //                Console.WriteLine("Backup Full");
        //                break;
        //            case "diff":
        //                Console.WriteLine("Backup Diff");
        //                break;
        //            case "log":
        //                Console.WriteLine("Backup Log");
        //                break;
        //            default:
        //                Console.WriteLine("Default");
        //                break;
        //        }               

        //    });

        //    // Chạy command với tham số đầu vào từ dòng lệnh
        //    //return command.Invoke(args);
        //    command.Invoke(args);

        //    //serverName = ConfigurationManager.AppSettings["ServerName"];
        //    // dbName = ConfigurationManager.AppSettings["DatabaseName"];
        //    // backupPath = ConfigurationManager.AppSettings["BackupPath"];
            
        //    ////connectionString = GetConnectionStringUser();

        //    //SqlDb sqlDb = new SqlDb();
        //    //connectionString = sqlDb.getConnStr();

        //    //Console.WriteLine(connectionString);
        //    //Console.WriteLine("Begin");
        //    //string dbFilePathName = "";
        //    //if (DateTime.Now.DayOfWeek != DayOfWeek.Sunday)
        //    //{
        //    //    Console.WriteLine("PerformFullBackup");
        //    //    dbFilePathName=PerformFullBackup();
        //    //    //Console.WriteLine("PerformLogBackup");
        //    //    //PerformLogBackup();
        //    //    Console.WriteLine("PerformLogBackupIfPossible");
        //    //    PerformLogBackupIfPossible();
        //    //}
        //    //else
        //    //{
        //    //    Console.WriteLine("PerformDiffBackup");
        //    //    dbFilePathName=PerformDiffBackup();
        //    //}
        //    //Console.WriteLine("SQL BACKUP Is Done");
        //    //sqlDb.ShrinkLogFile("100");
        //    //Console.WriteLine("ShrinkLogFile Is Done");
        //    //UploadBKFiletoGoogle(dbFilePathName); 

        //}
        static string  bkDefault()
        {
            string dbFilePathName;
            if (DateTime.Now.DayOfWeek != DayOfWeek.Sunday)
            {
                Console.WriteLine("PerformFullBackup");
                dbFilePathName = PerformFullBackup();                
                //Console.WriteLine("PerformLogBackupIfPossible");
                //PerformLogBackupIfPossible();
            }
            else
            {
                Console.WriteLine("PerformDiffBackup");
                dbFilePathName = PerformDiffBackup();
            }
            Console.WriteLine("SQL BACKUP Is Done");
            return dbFilePathName;
            //sqlDb.ShrinkLogFile("100");
            //Console.WriteLine("ShrinkLogFile Is Done");
            //UploadBKFiletoGoogle(dbFilePathName);
        }
        static void UploadBKFiletoGoogle(string filePathName)
        {
            Console.WriteLine("filePathName= "+ filePathName);
            return;
            Console.WriteLine("UploadBKFiletoGoogle");
            debugLogs.writeLog("UploadBKFiletoGoogle, filePathName= " + filePathName);
            GGDriveAPIToken googleOAuth20 = new GGDriveAPIToken();
            string folderID = ConfigurationManager.AppSettings["ggRootFolder"];
            Console.WriteLine("folderID_UploadFolder= " + folderID);
            string fileID = googleOAuth20.UploadFile(folderID, filePathName);
            Console.WriteLine("fileID= " + fileID);
            Console.WriteLine("UploadBKFiletoGoogle : DONE");
            debugLogs.writeLog("fileID= " + fileID);
        }
        static string GetConnectionStringUser()
        {
            string serverName = ConfigurationManager.AppSettings["ServerName"];
            string dbName = ConfigurationManager.AppSettings["DatabaseName"];
            string userId = ConfigurationManager.AppSettings["UserId"];
            string password = ConfigurationManager.AppSettings["Password"];
            return $"Server={serverName};Database={dbName};User Id={userId};Password={password};TrustServerCertificate=True;";
        }
        string GetConnectionString()
        {
            string serverName = ConfigurationManager.AppSettings["ServerName"];
            string dbName = ConfigurationManager.AppSettings["DatabaseName"];
            return $"Server={serverName};Database={dbName};Integrated Security=True;TrustServerCertificate=True;";
        }
        static string GetBackupPath()
        {
            return ConfigurationManager.AppSettings["BackupPath"];
        }
        static string PerformFullBackup()
        {
            string backupFile = $"{backupPath}FullBackup_{DateTime.Now:yyyyMMdd}.bak";
            Console.WriteLine("1.Full backup completed.|" + backupFile);
            return "backupFile";
            //debugLogs.writeLog(backupFile);
            //ExecuteBackup($"BACKUP DATABASE {dbName} TO DISK = '{backupFile}'");
            //Console.WriteLine("Full backup completed.|" + backupFile);
            //debugLogs.writeLog("Full backup completed.");
            //Console.WriteLine("1.Full backup completed.|" + backupFile);
            //return backupFile;
        }
        static string PerformLogBackup()
        {
            string backupFile = $"{backupPath}LogBackup_{DateTime.Now:yyyyMMdd}.trn";
            debugLogs.writeLog(backupFile);
            ExecuteBackup($"BACKUP LOG  {dbName} TO DISK = '{backupFile}'");
            Console.WriteLine("Log backup completed.");
            debugLogs.writeLog("Log backup completed.");
            return backupFile;
        }
        static string GetDatabaseRecoveryModel()
        {
            string query = $"SELECT recovery_model_desc FROM sys.databases WHERE name = '{ConfigurationManager.AppSettings["DatabaseName"]}'";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    return command.ExecuteScalar().ToString();
                }
            }
        }
        static void PerformLogBackupIfPossible()
        {
            //ALTER DATABASE YourDatabaseName SET RECOVERY FULL;
            string recoveryModel = GetDatabaseRecoveryModel();
            Console.WriteLine("recoveryModel=[" + recoveryModel + "]");
            
            if (recoveryModel != "SIMPLE")
            {
                string backupFile = Path.Combine(GetBackupPath(), $"LogBackup_{DateTime.Now:yyyyMMdd}.trn");
                debugLogs.writeLog(backupFile);
                ExecuteBackup($"BACKUP LOG {ConfigurationManager.AppSettings["DatabaseName"]} TO DISK = '{backupFile}'");
                Console.WriteLine("Log backup completed.");
                debugLogs.writeLog("backup completed.");
            }
            else
            {
                Console.WriteLine("Log backup skipped. Database is in SIMPLE recovery model.");
                debugLogs.writeLog("Log backup skipped. Database is in SIMPLE recovery model.");
            }
        }

        static string PerformDiffBackup()
        {
            string backupFile = $"{backupPath}DiffBackup_{DateTime.Now:yyyyMMdd}.bak";
            debugLogs.writeLog(backupFile);
            ExecuteBackup($"BACKUP DATABASE  {dbName} TO DISK = '{backupFile}' WITH DIFFERENTIAL");
            Console.WriteLine("Differential backup completed.");
            debugLogs.writeLog("Differential backup completed.");
            return backupFile;
        }
        static void ExecuteBackup(string backupCommand)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(backupCommand, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
