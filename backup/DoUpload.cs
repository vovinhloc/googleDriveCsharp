using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace backup
{
    class DoUpload
    {
        public DebugLogs debugLogs = new DebugLogs();
        private GGDriveAPIToken googleOAuth20 = new GGDriveAPIToken();
        public void callUploadToGGDrive(string fromDate, string toDate)
        {
            UploadToGGDrive(fromDate, toDate);
        }
        public void UploadToGGDrive(string fromDate, string toDate)
        {
            try
            {

                googleOAuth20.debugLogs = debugLogs;
                ProcessFile processFile = new ProcessFile();
                SqlDb sqlDb = new SqlDb();

                //WHERE StartTime>'2024-01-01 00:58:12.000' and (VMFILE is not NULL OR [RecordFile]!='') AND googledrive_id is NULL
                string cmd = $@"SELECT [ID],[CallID],[RecordFile],[VMFile],[googledrive_id],[Connected] FROM cdr_pbx 
                WHERE StartTime>=@fromDate AND StartTime<=@toDate  and (VMFILE is not NULL OR [RecordFile]!='')
                        AND googledrive_id is NULL ";
                debugLogs.writeLog($"cmd={cmd}");


                Dictionary<string, string> fin = new Dictionary<string, string>();
                fin.Add("fromDate", fromDate);
                fin.Add("toDate", toDate);
                string finJsonString = JsonConvert.SerializeObject(fin);
                debugLogs.writeLog($"finJsonString={finJsonString}");


                KQResponse kqGet_PbxLogs = sqlDb.getdatatablepw(cmd, fin);
                debugLogs.writeLog($"kqRows={kqGet_PbxLogs.rowEffected.ToString()}");

                int i = 0;
                debugLogs.writeLog($"BEGIN");
                foreach (DataRow row in kqGet_PbxLogs.data.Rows)
                {
                    debugLogs.writeLog($"{i.ToString()}");
                    //await Task.Delay(2);
                    i++;
                    string id = row["ID"].ToString();
                    string CallID = row["CallID"].ToString();
                    string RecordFile = row["RecordFile"].ToString();
                    string googledrive_id = row["googledrive_id"].ToString();
                    string Connected = row["Connected"].ToString();
                    string VMFile = row["VMFile"].ToString();
                    string fileID = "";
                    // string filePathToUpload = RecordFile;
                    // debugLogs.writeLog($"[{i.ToString()} - {id}]: dang kiem tra RecordFile {RecordFile}");
                    if (processFile.checkFileExisted(RecordFile))
                    {
                        Console.WriteLine($"RecordFile {RecordFile} is Existed");
                        fileID = processUpload(RecordFile);
                        debugLogs.writeLog($"[{i.ToString()} - {id}]: RecordFile {RecordFile} is Existed || [{fileID}]");


                    }
                    else
                    {
                        debugLogs.writeLog($"[{i.ToString()} - {id}]: *** RecordFile {VMFile} is Not Existed ");

                        if (processFile.checkFileExisted(VMFile))
                        {
                            Console.WriteLine($"[{i.ToString()}]: VMFile {VMFile} is Existed");
                            fileID = processUpload(VMFile);
                            debugLogs.writeLog($"[{i.ToString()} - {id}]: VMFile {VMFile} is Existed || [{fileID}]");


                        }
                        else
                        {
                            debugLogs.writeLog($"[{i.ToString()}]: *** VMFile {VMFile} is NOT Existed ");

                        }
                    }

                    saveGGFileIdToDB(fileID, id, CallID);

                }
                debugLogs.writeLog($"END");
            }
            catch (Exception ex)
            {
                debugLogs.writeLog($"[Error]: {ex.Message} ");
                debugLogs.writeLog($"END");

            }

        }

        private Dictionary<string, string> folderListId = new Dictionary<string, string>();
        private string processUpload(string RecordFile)
        {
            string kq = "";

            ProcessFile processFile = new ProcessFile();

            if (processFile.checkFileExisted(RecordFile))
            {
                string folderID = ConfigurationManager.AppSettings["ggRootFolder"]; //Properties.Settings.Default.ggRootFolder;
                //D:\2016\OneDrive - viendongtelecom\vmb\700\20240102\20240102 - 051052 - 0.wav

                string rootFolder = @"D:\2016\OneDrive - viendongtelecom\";
                string processRecordFilePath = RecordFile.Replace(rootFolder, "");
                string[] processRecordFilePath_arr = processRecordFilePath.Split('\\');
                int lenFilePath = processRecordFilePath_arr.Length - 1;
                string folderPath1 = "";
                int i = 0;
                for (; i < lenFilePath; i++)
                {
                    string folderName = processRecordFilePath_arr[i];
                    folderPath1 += $"{folderName}\\";
                    Console.WriteLine(folderName);
                    if (folderListId.ContainsKey(folderPath1))
                    {
                        folderID = folderListId[folderPath1];
                        continue;
                    }
                    string folderIDSearch = googleOAuth20.FindFolderIdByName(folderID, folderName);
                    Console.WriteLine($"folderName={folderName} - folderIDSearch=[{folderIDSearch}]");
                    if (folderIDSearch == "")
                    {
                        folderID = googleOAuth20.CreateFolder(folderID, folderName);
                        if (folderID == "")
                        {
                            break;
                        }
                    }
                    else
                    {
                        folderID = folderIDSearch;
                    }
                    folderListId[folderPath1] = folderID;
                }


                string fileID = googleOAuth20.FindFildIdByName(folderID, processRecordFilePath_arr[lenFilePath]);

                if (fileID == "")
                {
                    fileID = googleOAuth20.UploadFile(folderID, RecordFile);
                }


                kq = fileID;

            }

            return kq;
        }

        private void saveGGFileIdToDB(string fileID, string id, string CallID)
        {
            if (fileID != "")
            {
                SqlDb sqlDb = new SqlDb();
                string cmdUpdate = $"UPDATE cdr_pbx SET googledrive_id=@googleDriveID WHERE [ID]=@id ";
                Dictionary<string, string> fin = new Dictionary<string, string>();
                fin.Add("googleDriveID", fileID);
                fin.Add("id", id);
                KQResponse kqUpdatePBX = sqlDb.setdbpw(cmdUpdate, fin);

                cmdUpdate = $"UPDATE cdr_acd SET googledrive_id=@googleDriveID WHERE [CallID]=@CallID ";
                fin = new Dictionary<string, string>();
                fin.Add("googleDriveID", fileID);
                fin.Add("CallID", CallID);
                KQResponse kqUpdateACD = sqlDb.setdbpw(cmdUpdate, fin);

                cmdUpdate = $"UPDATE cdr_exten SET googledrive_id=@googleDriveID WHERE [CallID]=@CallID ";

                KQResponse kqUpdateEXTEN = sqlDb.setdbpw(cmdUpdate, fin);

                cmdUpdate = $"UPDATE [voice_mailbox] SET googledrive_id=@googleDriveID WHERE [CallID]=@CallID ";

                KQResponse kqUpdateVM = sqlDb.setdbpw(cmdUpdate, fin);
            }
        }
    }
}
