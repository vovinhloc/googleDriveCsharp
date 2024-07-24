using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Configuration;
using System.IO;

using System.Data.SqlClient;

namespace backup
{
    class SqlDb
    {
        private string connstr;
        private string dbName;
        public string buildDBString()
        {

            string myXmlString = "";

            myXmlString = ConfigurationManager.AppSettings["sqlConfigFile"]; //Properties.Settings.Default.sqlConfigFile;
            //System.IO.Path.GetDirectoryName(
            //System.Reflection.Assembly.GetExecutingAssembly().Location) + @"\..\sippbxv3.xml";

            XmlReader xmlFile;
            xmlFile = XmlReader.Create(myXmlString, new XmlReaderSettings());
            DataSet ds = new DataSet();
            ds.ReadXml(xmlFile);

            DataTable dt = ds.Tables["Database"];

            string kq = "Data Source=";
            string pcname = System.Net.Dns.GetHostName();

            string dbtype = dt.Rows[0]["DBType"].ToString();
            string dbname = dt.Rows[0]["DBName"].ToString();
            string DBServer = dt.Rows[0]["DBServer"].ToString();
            string AuthType = dt.Rows[0]["AuthType"].ToString();

            dbName = dbname;
            if (DBServer == "")
            {
                DBServer = System.Environment.MachineName;
                switch (dbtype)
                {
                    case "0":

                        DBServer += "\\SQLexpress";
                        break;
                    default:
                        break;
                }
            }
            if (AuthType == "0")
            {
                //0 is SQL Authentication, 1 is Windows Authentication
                kq += DBServer + ";Database=" + dbname + ";User Id=" + dt.Rows[0]["UserName"].ToString();
                kq += ";Password=" + dt.Rows[0]["Password"].ToString();
            }
            else
            {

                kq += DBServer + ";Initial Catalog=" + dbname + ";Integrated Security=True";


            }
            return kq;
        }

        public SqlDb()
        {
            connstr = buildDBString();
        }
        public string getConnStr()
        {
            return connstr;
        }
        public KQResponse getdatatablepw(string strcmd, Dictionary<string, string> paras)
        {
            KQResponse js = new KQResponse();

            js.cmd = strcmd;
            js.paras = paras;

            try
            {

                using (SqlConnection conn = new SqlConnection(connstr))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(strcmd, conn);

                    cmd.CommandTimeout = 0;
                    foreach (KeyValuePair<string, string> item in paras)
                    {
                        switch (item.Key)
                        {
                            case "NameLike":
                                cmd.Parameters.AddWithValue("@" + item.Key, string.Format("%{0}%", item.Value));
                                break;
                            default:
                                if (item.Key.Contains("_Like"))
                                {

                                    cmd.Parameters.AddWithValue("@" + item.Key, string.Format("%{0}%", item.Value));

                                }
                                else
                                {
                                    cmd.Parameters.AddWithValue("@" + item.Key, item.Value);
                                }
                                break;
                        }


                    }
                    SqlDataAdapter da = new SqlDataAdapter(cmd);

                    da.Fill(js.data);
                    js.message = "Successfully !";
                    js.cmd = strcmd;
                    js.rowEffected = js.data.Rows.Count;

                    return js;
                }
            }
            catch (Exception err)
            {
                js.error = "1";
                js.message = err.Message;
                return js;
            }

        }

        public KQResponse setdbpw(string strcmd, Dictionary<string, string> paras)
        {
            KQResponse js = new KQResponse();
            js.cmd = strcmd;
            js.paras = paras;

            try
            {
                using (SqlConnection conn = new SqlConnection(connstr))
                {
                    SqlCommand cmd = new SqlCommand(strcmd, conn);

                    foreach (KeyValuePair<string, string> item in paras)
                    {
                        cmd.Parameters.AddWithValue("@" + item.Key, item.Value);

                    }

                    conn.Open();
                    js.rowEffected = cmd.ExecuteNonQuery();


                    js.message = "Successfully !";


                    return js;
                }
            }
            catch (Exception err)
            {

                js.error = "1";
                js.message = err.Message;
                return js;
            }

        }

        public string getLogFileName(SqlConnection connection, string databaseName)
        {
            string query = $@"
            SELECT name 
            FROM sys.master_files 
            WHERE type = 1 AND database_id = DB_ID('{databaseName}')
        ";

            using (SqlCommand command = new SqlCommand(query, connection))
            {
                object result = command.ExecuteScalar();
                return result?.ToString();
            }
        }
        public void ShrinkLogFile( string logSizeMB)
        {
            string databaseName = dbName;
            using (SqlConnection connection = new SqlConnection(connstr))
            {
                connection.Open();

                // Determine the log file name
                string logFileName =getLogFileName(connection, databaseName);
                if (logFileName == null)
                {
                    Console.WriteLine("Log file not found.");
                    return;
                }

                string sqlScript = $@"
                USE {databaseName};
                GO

                ALTER DATABASE {databaseName}
                SET RECOVERY SIMPLE;
                GO

                DBCC SHRINKFILE ({logFileName}, {logSizeMB});
                GO

                ALTER DATABASE {databaseName}
                SET RECOVERY FULL;
                GO
            ";

                // Split the script into individual commands
                string[] sqlCommands = sqlScript.Split(new[] { "GO" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string commandText in sqlCommands)
                {
                    if (!string.IsNullOrWhiteSpace(commandText))
                    {
                        using (SqlCommand command = new SqlCommand(commandText, connection))
                        {
                            command.ExecuteNonQuery();
                        }
                    }
                }

                Console.WriteLine("SQL script executed successfully.");
            }
        }

        public void ExecuteBackup(string backupCommand)
        {
            using (SqlConnection connection = new SqlConnection(connstr))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(backupCommand, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }
        public string GetDatabaseRecoveryModel()
        {
            string query = $"SELECT recovery_model_desc FROM sys.databases WHERE name = '{ConfigurationManager.AppSettings["DatabaseName"]}'";
            using (SqlConnection connection = new SqlConnection(connstr))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    return command.ExecuteScalar().ToString();
                }
            }
        }
    }
}
