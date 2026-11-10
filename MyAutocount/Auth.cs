using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GCR_autocount_api
{
    class Auth
    {
        private static AutoCount.Data.DBSetting _dbSetting;
        private static AutoCount.Authentication.UserSession _userSession;
        private static readonly object _sessionLock = new object();
        public static string ipAddress;
        public static string port;
        private static string databaseName;
        private static string serverName;
        private static string saPassword;
        private static string dmfFolder;
        private static string dmfFile;

        public static string CurrentDatabaseName { get { return databaseName; } }
        public static string CurrentServerName { get { return serverName; } }
        public static string CurrentDmfFolder { get { return MySettings.ResolveDmfFolder(GetCurrentDmfSettings()); } }
        public static string[] CurrentDmfFiles { get { return MySettings.ResolveDmfFiles(GetCurrentDmfSettings()); } }
        public static string CurrentCompanyName { get { return _currentCompanyName; } }
        private static string _currentCompanyName;
        private static string _currentRemark;

        public static AutoCount.Data.DBSetting dbSetting
        {
            get
            {
                if (_dbSetting == null)
                {
                    InitDbSetting();
                }
                return _dbSetting;
            }
            set
            {
                _dbSetting = value;
            }
        }

        public static AutoCount.Authentication.UserSession userSession
        {
            get
            {
                if (_userSession == null)
                {
                    InitUserSession();
                }
                return _userSession;
            }
            set
            {
                _userSession = value;
            }
        }

        public static void InitDbSetting()
        {
            // For testing
            //_dbSetting = new AutoCount.Data.DBSetting(AutoCount.Data.DBServerType.SQL2000, "(local)\\A2006", "sa", "oCt2005-ShenZhou6_A2006", "AED_Shrdc");

            // For release
            _dbSetting = new AutoCount.Data.DBSetting(AutoCount.Data.DBServerType.SQL2000, serverName, "sa", saPassword, databaseName);

        }

        public static List<DatabaseInfo> GetAvailableDatabases()
        {
            var list = new List<DatabaseInfo>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                string[] dmfFiles = CurrentDmfFiles;
                if (dmfFiles == null || dmfFiles.Length == 0)
                {
                    Utils.Log("No .dmf files found in: " + CurrentDmfFolder + " (check dmfFolder/dmfFile in settings.json)");
                    return list;
                }
                // dmfFile set = use only that file; otherwise scan every .dmf in dmfFolder
                foreach (string dmfPath in dmfFiles)
                {
                    try
                    {
                        System.Data.DataTable table = DatabaseInfo.DatabaseManagementFileToDataTable(dmfPath);
                        for (int i = 0; i < table.Rows.Count; i++)
                        {
                            try
                            {
                                var info = new DatabaseInfo(table, i);
                                if (string.IsNullOrWhiteSpace(info.databaseName)) continue;
                                if (seen.Add(info.databaseName)) list.Add(info);
                            }
                            catch (Exception ex)
                            {
                                Utils.Log("Skipping unreadable DMF row " + i + " in " + Path.GetFileName(dmfPath) + ": " + ex.Message);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Utils.Log("Skipping unreadable DMF file " + Path.GetFileName(dmfPath) + ": " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.Log("GetAvailableDatabases error: " + ex.Message);
            }
            return list;
        }

        public static DatabaseInfo FindDatabase(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier)) return null;
            string id = identifier.Trim();
            var all = GetAvailableDatabases();
            // Match databaseName first, then companyName, then remark (case-insensitive)
            var match = all.FirstOrDefault(d =>
                string.Equals(d.databaseName, id, StringComparison.OrdinalIgnoreCase));
            if (match != null) return match;
            match = all.FirstOrDefault(d =>
                string.Equals(d.companyName, id, StringComparison.OrdinalIgnoreCase));
            if (match != null) return match;
            match = all.FirstOrDefault(d =>
                d.remark != null && string.Equals(d.remark, id, StringComparison.OrdinalIgnoreCase));
            return match;
        }

        /// <summary>
        /// Switch the active project/database. Returns the DatabaseInfo switched to, or null if not found.
        /// Persists choice to settings.json so restarts keep the selection.
        /// </summary>
        public static DatabaseInfo SwitchDatabase(string identifier)
        {
            DatabaseInfo info = FindDatabase(identifier);
            if (info == null) return null;
            lock (_sessionLock)
            {
                databaseName = info.databaseName;
                serverName = info.serverName;
                _currentCompanyName = info.companyName;
                _currentRemark = info.remark;
                _dbSetting = new AutoCount.Data.DBSetting(AutoCount.Data.DBServerType.SQL2000, serverName, "sa", saPassword, databaseName);
                _userSession = new AutoCount.Authentication.UserSession(_dbSetting);
            }
            PersistDatabaseChoice(info);
            Utils.Log("Switched project to: " + info.companyName + " [" + info.databaseName + "]");
            return info;
        }

        private static void PersistDatabaseChoice(DatabaseInfo info)
        {
            try
            {
                string settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
                if (File.Exists(settingsPath))
                {
                    string json = File.ReadAllText(settingsPath);
                    MySettings settings = JsonConvert.DeserializeObject<MySettings>(json);
                    if (settings != null)
                    {
                        settings.databaseName = info.databaseName;
                        settings.serverName = info.serverName;
                        File.WriteAllText(settingsPath, JsonConvert.SerializeObject(settings));
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.Log("PersistDatabaseChoice warning: " + ex.Message);
            }
        }

        public static void InitUserSession()
        {
            _userSession = new AutoCount.Authentication.UserSession(_dbSetting);
        }


        public static bool Login(AutoCount.Authentication.UserSession userSession, string username = "KENNY", string password = "1111")
        {
            if (!userSession.IsLogin)
            {
                if (userSession.Login(username, password))
                {
                    Utils.Log("Login successfully");
                    return true;
                }
                Utils.Log("Login failed");
                return false;
            }
            Utils.Log("Already login.");
            return true;
        }

        public static void InitCrudentials()
        {
            MySettings settings = MySettings.GetMySettings();
            if (settings == null) return;
            ipAddress = settings.ipAddress;
            port = settings.port;
            databaseName = settings.databaseName;
            serverName = settings.serverName;
            saPassword = settings.saPassword;
            dmfFolder = settings.dmfFolder;
            dmfFile = settings.dmfFile;
        }

        private static MySettings GetCurrentDmfSettings()
        {
            // Re-read settings.json so edits to dmfFolder/dmfFile apply without restart.
            // Falls back to cached values, then to defaults.
            try
            {
                string settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
                if (File.Exists(settingsPath))
                {
                    string json = File.ReadAllText(settingsPath);
                    MySettings fromFile = JsonConvert.DeserializeObject<MySettings>(json);
                    if (fromFile != null && (!string.IsNullOrWhiteSpace(fromFile.dmfFolder) || !string.IsNullOrWhiteSpace(fromFile.dmfFile)))
                        return fromFile;
                    if (fromFile != null)
                    {
                        fromFile.dmfFolder = dmfFolder;
                        fromFile.dmfFile = dmfFile;
                        return fromFile;
                    }
                }
            }
            catch { }
            return new MySettings() { dmfFolder = dmfFolder, dmfFile = dmfFile };
        }

        public static void Init(string username = "KENNY", string password = "1111")
        {
            InitCrudentials();
            InitDbSetting();
            InitUserSession();
            RefreshCurrentCompanyName();
            Login(_userSession, username, password);
        }

        private static void RefreshCurrentCompanyName()
        {
            try
            {
                var all = GetAvailableDatabases();
                var match = all.FirstOrDefault(d =>
                    string.Equals(d.databaseName, databaseName, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    _currentCompanyName = match.companyName;
                    _currentRemark = match.remark;
                }
            }
            catch { }
        }


    }
}
