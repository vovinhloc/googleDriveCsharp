using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Newtonsoft.Json;

namespace backup
{
    class GGDriveAPIToken
    {

        private string ApplicationName = "YourAppName";
        private string[] Scopes = { DriveService.Scope.Drive };
        public string apiEndpoint = "https://www.googleapis.com/drive/v3/files";
        private DriveService service;
        private UserCredential credential;

        DateTime at_expirationTime = DateTime.Now.AddSeconds(-100);
        public DebugLogs debugLogs = new DebugLogs();
        // Helpper
        private async Task IsTokenExpiredAsync()
        {
            // Lấy thời điểm hiện tại
            DateTime currentTime = DateTime.Now;

            // So sánh thời điểm hết hạn với thời điểm hiện tại
            if (currentTime >= at_expirationTime)
            {
                debugLogs.writeLog("can get accesstonke");
                await getATFromRTAsync();
            }
            else
            {
                debugLogs.writeLog("Access Token is OK");
            }
        }
        public FormUrlEncodedContent buildContentRT()
        {
            string credentialsPath = ConfigurationManager.AppSettings["credentialsPath"];// Properties.Settings.Default.credentialsPath;
            string refreshToken = ConfigurationManager.AppSettings["refreshToken"];//Properties.Settings.Default.refreshToken;
            // Tạo đối tượng GoogleCredential từ refresh token



            // Read the file contents
            string jsonString = File.ReadAllText(credentialsPath);

            // Deserialize the JSON into a dynamic object
            dynamic jsonData = JsonConvert.DeserializeObject(jsonString);

            // Access the data using property names
            string client_id = jsonData.web.client_id;
            string client_secret = jsonData.web.client_secret;
            //List<string> hobbies = jsonData.hobbies;

            FormUrlEncodedContent content = new FormUrlEncodedContent(new[]
            {


                new KeyValuePair<string, string>("client_id", client_id),
                new KeyValuePair<string, string>("client_secret", client_secret),
                new KeyValuePair<string, string>("refresh_token",refreshToken),// ; Properties.Settings.Default.refreshToken),

                new KeyValuePair<string, string>("grant_type", "refresh_token")
            });
            return content;
        }
        public async Task<string> postURLContentAsync(string url, FormUrlEncodedContent content)
        {

            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.PostAsync(url, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                Console.WriteLine(responseBody);
                return responseBody;
            }

        }
        private void UpdateAppSetting(string key, string value)
        {
            // Mở Configuration của ứng dụng
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            // Cập nhật giá trị của key
            config.AppSettings.Settings[key].Value = value;

            // Lưu thay đổi
            config.Save(ConfigurationSaveMode.Modified);

            // Tải lại cấu hình để áp dụng ngay lập tức
            ConfigurationManager.RefreshSection("appSettings");
        }
        public async Task getATFromRTAsync()
        {


            string access_token;
            using (var httpClient = new HttpClient())
            {
                var url = "https://oauth2.googleapis.com/token";


                FormUrlEncodedContent content = buildContentRT();
                string kq = await postURLContentAsync(url, content);
                debugLogs.writeLog(kq);
                dynamic tokenResponse = JsonConvert.DeserializeObject(kq);
                //string error = "" + tokenResponse.error;
                access_token = "" + tokenResponse.access_token;

                debugLogs.writeLog($"access_token={access_token}");
                //Properties.Settings.Default.at_expires_in = Convert.ToInt32(tokenResponse.expires_in) - 600;
                //Properties.Settings.Default.accessToken = access_token;
                //Properties.Settings.Default.Save();

                UpdateAppSetting("accessToken", access_token);


                at_expirationTime = DateTime.Now.AddSeconds(Convert.ToInt32(tokenResponse.expires_in) - 600); // Thời điểm hết hạn của token


            }

        }




        private void checkTokenExpired()
        {
            // Lấy thời điểm hiện tại
            DateTime currentTime = DateTime.Now;

            // So sánh thời điểm hết hạn với thời điểm hiện tại
            //debugLogs.writeLog($"currentTime={Convert.ToString(currentTime)}");
            //debugLogs.writeLog($"at_expirationTime={Convert.ToString(at_expirationTime)}");
            if (currentTime >= at_expirationTime)
            {
                //debugLogs.writeLog("**** Access Token is Expired, can tao Access Token moi");
                InitializeDriveService();
            }
            //else
            //{
            //    debugLogs.writeLog("Access Token is OK");
            //}
        }
        public void InitializeDriveService()
        {
            string credentialsPath = ConfigurationManager.AppSettings["credentialsPath"];// Properties.Settings.Default.credentialsPath;
            string refreshToken = ConfigurationManager.AppSettings["refreshToken"];// Properties.Settings.Default.refreshToken;
            // Tạo đối tượng GoogleCredential từ refresh token
            // Read the file contents
            string jsonString = File.ReadAllText(credentialsPath);
            // Deserialize the JSON into a dynamic object
            dynamic jsonData = JsonConvert.DeserializeObject(jsonString);

            // Access the data using property names
            string clientId = jsonData.web.client_id;
            string clientSecret = jsonData.web.client_secret;


            credential = new UserCredential(new GoogleAuthorizationCodeFlow(
                new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId = clientId,
                        ClientSecret = clientSecret
                    },
                    Scopes = new[] { DriveService.Scope.Drive }
                }), "user", new TokenResponse { RefreshToken = refreshToken });

            credential.RefreshTokenAsync(CancellationToken.None).Wait();

            service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "UploadToGoogle" //"YourAppName"
            });
            at_expirationTime = DateTime.Now.AddSeconds(45 * 60); // Thời điểm hết hạn của token
            debugLogs.writeLog($"accessToken={credential.Token.AccessToken}");
            debugLogs.writeLog($"at_expirationTime={Convert.ToString(at_expirationTime)}");
        }

        public string ListFilesAndFolders(string folderId)
        {
            checkTokenExpired();
            string kq = "";
            // Define parameters for the request
            FilesResource.ListRequest listRequest = service.Files.List();
            listRequest.Q = $"'{folderId}' in parents";
            listRequest.Fields = "files(id, name, mimeType)";

            // Execute the request
            IList<Google.Apis.Drive.v3.Data.File> files = listRequest.Execute().Files;

            if (files != null && files.Count > 0)
            {
                Console.WriteLine("Files and Folders:");
                foreach (var file in files)
                {
                    Console.WriteLine($"{file.Name} ({file.Id}) - {file.MimeType}");
                    kq += $"{file.Name} ({file.Id}) - {file.MimeType} \n\r";
                }
            }
            else
            {
                kq += "No files or folders found. \n\r";
                Console.WriteLine("No files or folders found.");
            }
            return kq;
        }

        public string CreateFolder(string parentFolderId, string folderName)
        {
            checkTokenExpired();
            var folderMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = folderName,
                MimeType = "application/vnd.google-apps.folder",
                Parents = new List<string> { parentFolderId }
            };

            var request = service.Files.Create(folderMetadata);
            request.Fields = "id";

            var createdFolder = request.Execute();
            Console.WriteLine("created  a folder " + folderName + " voi Id =" + createdFolder.Id);
            return createdFolder.Id;
        }

        public string FindFolderIdByName(string parentFolderId, string folderName)
        {
            checkTokenExpired();
            var listRequest = service.Files.List();
            listRequest.Q = $"'{parentFolderId}' in parents and name='{folderName}' and trashed=false and mimeType='application/vnd.google-apps.folder'";
            listRequest.Fields = "files(id)";

            var files = listRequest.Execute().Files;

            if (files != null && files.Count > 0)
            {
                return files[0].Id;
            }

            return ""; // Trả về "" nếu không tìm thấy thư mục 
        }
        public string FindFildIdByName(string parentFolderId, string fileName)
        {
            checkTokenExpired();
            var listRequest = service.Files.List();
            listRequest.Q = $"'{parentFolderId}' in parents and name='{fileName}' and trashed=false and mimeType!='application/vnd.google-apps.folder'";
            listRequest.Fields = "files(id)";

            var files = listRequest.Execute().Files;

            if (files != null && files.Count > 0)
            {
                return files[0].Id;
            }

            return ""; // Trả về "" nếu không tìm thấy thư mục
        }

        public string UploadFile(string folderId, string filePath)
        {
            checkTokenExpired();
            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = Path.GetFileName(filePath),
                Parents = new List<string> { folderId }
            };

            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                var request = service.Files.Create(fileMetadata, stream, "application/octet-stream");
                request.Fields = "id";
                request.Upload();

                var uploadedFile = request.ResponseBody;

                return uploadedFile.Id;
            }
        }


    }
}
