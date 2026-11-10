using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using static GCR_autocount_api.Utils;

namespace GCR_autocount_api
{
    public class MySettings
    {
        static readonly string SettingsJsonFilename = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
        public string ipAddress;
        public string port;
        public string databaseName;
        public string serverName;
        public string saPassword;
        public string dmfFolder;
        public string dmfFile;

        public static string GetDefaultDmfFolder()
        {
            string appDataPath = System.Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            return System.IO.Path.Combine(appDataPath, "AutoCount", "Accounting 2");
        }

        public static string ResolveDmfFolder(MySettings settings)
        {
            try
            {
                string configured = settings != null ? (settings.dmfFolder ?? "") : "";
                configured = configured.Trim();
                if (!string.IsNullOrEmpty(configured))
                {
                    configured = System.Environment.ExpandEnvironmentVariables(configured);
                    if (!System.IO.Path.IsPathRooted(configured))
                        configured = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configured));
                    // If user accidentally pointed dmfFolder at a file, use its directory
                    if (System.IO.File.Exists(configured))
                        return System.IO.Path.GetDirectoryName(configured);
                    return configured;
                }
                // dmfFolder empty but dmfFile points at a real file -> show its directory
                string file = settings != null ? (settings.dmfFile ?? "") : "";
                file = file.Trim();
                if (!string.IsNullOrEmpty(file))
                {
                    file = System.Environment.ExpandEnvironmentVariables(file);
                    if (!System.IO.Path.IsPathRooted(file))
                        file = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, file));
                    if (System.IO.File.Exists(file))
                        return System.IO.Path.GetDirectoryName(file);
                    if (System.IO.Directory.Exists(file))
                        return file;
                }
            }
            catch { }
            return GetDefaultDmfFolder();
        }

        public static string[] ResolveDmfFiles(MySettings settings)
        {
            try
            {
                string configuredFile = settings != null ? (settings.dmfFile ?? "") : "";
                configuredFile = configuredFile.Trim();
                if (!string.IsNullOrEmpty(configuredFile))
                {
                    configuredFile = System.Environment.ExpandEnvironmentVariables(configuredFile);
                    if (!System.IO.Path.IsPathRooted(configuredFile))
                        configuredFile = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configuredFile));
                    if (System.IO.File.Exists(configuredFile))
                        return new string[] { configuredFile };
                    // dmfFile pointing at a folder behaves like dmfFolder
                    if (System.IO.Directory.Exists(configuredFile))
                    {
                        string[] inFolder = System.IO.Directory.GetFiles(configuredFile, "*.dmf");
                        if (inFolder.Length > 0) return inFolder;
                    }
                }
                string folder = ResolveDmfFolder(settings);
                if (System.IO.File.Exists(folder))
                    return new string[] { folder };
                if (System.IO.Directory.Exists(folder))
                    return System.IO.Directory.GetFiles(folder, "*.dmf");
            }
            catch { }
            return new string[0];
        }

        public void PrintInfo()
        {
            Console.WriteLine($"IP Address: {ipAddress}\nPort: {port}\nDatabase Name: {databaseName}\n" +
                $"Server Name: {serverName}\nSa Password: {saPassword}\nDmf Folder: {ResolveDmfFolder(this)}\nDmf File: {dmfFile}");
        }

        public static MySettings GetMySettings()
        {
            if (File.Exists(SettingsJsonFilename))
            {
                using (StreamReader r = new StreamReader(SettingsJsonFilename))
                {
                    string json = r.ReadToEnd();
                    MySettings settings = JsonConvert.DeserializeObject<MySettings>(json);
                    return settings;
                }
            } else
            {
                MySettings settings = WriteMySettings();
                return settings;
            }
        }

        public static MySettings WriteMySettings()
        {
            string folder = GetDefaultDmfFolder();
            string dmfPath = System.IO.Path.Combine(folder, "A2006.dmf");

            // Prefer the first readable entry from any .dmf in the default folder
            try
            {
                if (System.IO.Directory.Exists(folder))
                {
                    string[] dmfFiles = System.IO.Directory.GetFiles(folder, "*.dmf");
                    foreach (string candidate in dmfFiles)
                    {
                        try
                        {
                            DataTable table = DatabaseInfo.DatabaseManagementFileToDataTable(candidate);
                            if (table.Rows.Count > 0)
                            {
                                dmfPath = candidate;
                                break;
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
           
            try
            {
                DataTable table = DatabaseInfo.DatabaseManagementFileToDataTable(dmfPath);
                DatabaseInfo databaseInfo = new DatabaseInfo(table, 0);

                MySettings settings = new MySettings()
                {
                    // Write default settings to settings.json if not exists
                    ipAddress = "localhost",
                    port = "8888",
                    databaseName = databaseInfo.databaseName,
                    serverName = databaseInfo.serverName,
                    saPassword = "oCt2005-ShenZhou6_A2006",
                    dmfFolder = GetDefaultDmfFolder(),
                    dmfFile = ""
                };

                string json = JsonConvert.SerializeObject(settings);
                System.IO.File.WriteAllText(SettingsJsonFilename, json);
                return settings;

            } catch (System.IO.FileNotFoundException)
            {
                print("DMF File is not found. Check \"dmfFolder\"/\"dmfFile\" in settings.json (default: " + folder + "). ");
                return null;
            }
            

            
        }

    }
}
