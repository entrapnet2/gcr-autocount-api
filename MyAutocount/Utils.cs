using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Nancy;
using Nancy.Extensions;

namespace GCR_autocount_api
{
    class Utils
    {
        private static string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "app.log");
        private static object logLock = new object();
        private static object versionLock = new object();
        private static string _appVersion;

        /// <summary>
        /// App version from the stamped assembly (e.g. "1.0.7"), "?" if unknown.
        /// </summary>
        public static string AppVersion
        {
            get
            {
                if (_appVersion == null)
                {
                    lock (versionLock)
                    {
                        if (_appVersion == null)
                        {
                            try
                            {
                                var v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                                _appVersion = v == null ? "?"
                                    : (v.Revision == 0 ? $"{v.Major}.{v.Minor}.{v.Build}" : v.ToString());
                            }
                            catch { _appVersion = "?"; }
                        }
                    }
                }
                return _appVersion;
            }
        }

        /// <summary>
        /// Called once at startup: archives the previous run's log as
        /// logs/archive/app-&lt;version&gt;-&lt;timestamp&gt;.log so each restart
        /// starts a fresh app.log and history stays grouped by version.
        /// </summary>
        public static void RotateLog()
        {
            try { lock (logLock) { RotateLogCore(); } } catch { }
        }

        private const int MaxArchivedLogs = 7;

        private static void RotateLogCore()
        {
            string logDir = Path.GetDirectoryName(logFilePath);
            string archiveDir = Path.Combine(logDir, "archive");
            Directory.CreateDirectory(archiveDir);
            if (File.Exists(logFilePath) && new FileInfo(logFilePath).Length > 0)
            {
                string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                string dest = Path.Combine(archiveDir, $"app-{AppVersion}-{stamp}.log");
                int i = 1;
                while (File.Exists(dest))
                {
                    dest = Path.Combine(archiveDir, $"app-{AppVersion}-{stamp}-{i++}.log");
                }
                try { File.Move(logFilePath, dest); } catch { }
            }
            EnforceLogRetention(archiveDir);
        }

        private static void EnforceLogRetention(string archiveDir)
        {
            // Retention: keep only the 7 newest archived logs.
            try
            {
                var old = new DirectoryInfo(archiveDir).GetFiles("app-*.log")
                    .OrderByDescending(f => f.CreationTimeUtc).Skip(MaxArchivedLogs).ToList();
                foreach (var f in old)
                {
                    try { f.Delete(); } catch { }
                }
            }
            catch { }
        }

        private static System.Threading.Timer _rotationTimer;
        private static readonly object rotationLock = new object();

        /// <summary>
        /// Splits app.log every midnight (00:00 local) so a long-running server
        /// never grows one giant log. Same naming + keep-7-newest rules as startup rotation.
        /// </summary>
        public static void StartDailyRotation()
        {
            try
            {
                lock (rotationLock)
                {
                    if (_rotationTimer != null) return;
                    _rotationTimer = new System.Threading.Timer(OnDailyRotation, null, NextMidnightMs(), System.Threading.Timeout.Infinite);
                    Log("Daily log rotation scheduled (every 12:00 AM).");
                }
            }
            catch { }
        }

        private static long NextMidnightMs()
        {
            double ms = (DateTime.Today.AddDays(1) - DateTime.Now).TotalMilliseconds;
            return ms < 1000 ? 1000 : (long)ms;
        }

        private static void OnDailyRotation(object state)
        {
            try
            {
                RotateLog();
                Log("Midnight log rotation done.");
            }
            catch { }
            try
            {
                lock (rotationLock)
                {
                    if (_rotationTimer != null)
                        _rotationTimer.Change(NextMidnightMs(), System.Threading.Timeout.Infinite);
                }
            }
            catch { }
        }

        public static void print(object str)
        {
            Console.WriteLine(str.ToString());
        }

        public static string PrintTime()
        {
            string time = DateTime.Now.ToString("HH:mm:ss.ffffff", System.Globalization.DateTimeFormatInfo.InvariantInfo);
            return time;
        }

        public static void Log(string str)
        {
            string logMessage = $"{PrintTime()} : {str}";
            print(logMessage);
            WriteToFile(logMessage);
        }

        private static void WriteToFile(string message)
        {
            try
            {
                lock (logLock)
                {
                    string logDir = Path.GetDirectoryName(logFilePath);
                    if (!Directory.Exists(logDir))
                    {
                        Directory.CreateDirectory(logDir);
                    }
                    File.AppendAllText(logFilePath, message + Environment.NewLine);
                }
            }
            catch
            {
            }
        }

        public static void Output(string str)
        {
            print($"Output: {str}");
        }
        public static string DataTableToJsonString(DataTable dataTable)
        {
            return JsonConvert.SerializeObject(dataTable, Formatting.Indented);
        }

        /// <summary>
        /// Input: dd-MM-yyyy , output: dd-MM-yyyy 12:00:00 AM
        /// </summary>
        /// <param name="dateString"></param>
        /// <returns></returns>
        public static DateTime DateStringToDateTime(string dateString)
        {
            dateString = $"{dateString} 12:00:00 AM";
            DateTime date = DateTime.ParseExact(dateString, "dd-MM-yyyy hh:mm:ss tt", CultureInfo.InvariantCulture);
            return date;
        }

        public static dynamic ParseRequest(Nancy.Request request)
        {
            dynamic jsonData = null;
            if (request.Headers.ContentType != null && request.Headers.ContentType.ToString().Contains("application/json"))
            {
                string jsonString = Nancy.IO.RequestStream.FromStream(request.Body).AsString();
                jsonData = JsonConvert.DeserializeObject(jsonString);
            }
            else
            {
                var formDict = request.Form.ToDictionary();
                if (formDict.ContainsKey("payload"))
                {
                    jsonData = JsonConvert.DeserializeObject(formDict["payload"].ToString());
                }
                else
                {
                    jsonData = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(formDict));
                    foreach (Newtonsoft.Json.Linq.JProperty prop in jsonData.Children())
                    {
                        if (prop.Value.Type == Newtonsoft.Json.Linq.JTokenType.String)
                        {
                            string strValue = prop.Value.ToString();
                            if ((strValue.StartsWith("[") && strValue.EndsWith("]")) ||
                                (strValue.StartsWith("{") && strValue.EndsWith("}")))
                            {
                                try
                                {
                                    jsonData[prop.Name] = JsonConvert.DeserializeObject(strValue);
                                }
                                catch
                                {
                                }
                            }
                        }
                    }
                }
            }
            return jsonData;
        }

        public static Response CreateErrorResponse(string message)
        {
            var errorObj = new { error = message };
            var response = (Response)JsonConvert.SerializeObject(errorObj);
            response.StatusCode = HttpStatusCode.InternalServerError;
            response.ContentType = "application/json";
            return response;
        }

        /// <summary>
        /// True when AutoCount blocked the call because the API module licence
        /// key is not registered (ModuleControl.CheckAPICall/IsAPILicenseKeyValid).
        /// The SDK throws NullReferenceException here instead of a clear error.
        /// </summary>
        public static bool IsApiLicenseError(Exception ex)
        {
            try
            {
                string text = ex?.ToString() ?? "";
                Exception inner = ex?.InnerException;
                while (inner != null)
                {
                    text += "\n" + inner.ToString();
                    inner = inner.InnerException;
                }
                return text.Contains("IsAPILicenseKeyValid") || text.Contains("CheckAPICall");
            }
            catch
            {
                return false;
            }
        }

        public static string FullErrorMessage(Exception ex)
        {
            var parts = new List<string>();
            while (ex != null)
            {
                if (!string.IsNullOrEmpty(ex.Message)) parts.Add(ex.Message);
                ex = ex.InnerException;
            }
            return string.Join(" <- ", parts);
        }

        /// <summary>
        /// Route-level error mapping: 402 with an actionable message for missing
        /// API licence, otherwise the existing 500 JSON (with full message chain).
        /// </summary>
        public static Response ToErrorResponse(Exception ex)
        {
            if (IsApiLicenseError(ex))
            {
                Log("API licence check failed - returning HTTP 402 API_LICENSE_MISSING to client (no document was saved).");
                var errorObj = new
                {
                    success = false,
                    code = "API_LICENSE_MISSING",
                    message = "AutoCount API licence key is not registered for this company book, so AutoCount blocked the posting. Please register the API module licence in AutoCount for this book (licence key from AutoCount or your vendor), then retry. No document was saved."
                };
                var response = (Response)JsonConvert.SerializeObject(errorObj);
                response.StatusCode = (HttpStatusCode)402;
                response.ContentType = "application/json";
                return response;
            }
            return CreateErrorResponse(FullErrorMessage(ex));
        }

        public static Response CreateErrorResponse(Exception ex)
        {
            return ToErrorResponse(ex);
        }

        /// <summary>
        /// One-line request summary for app.log: METHOD + path + query + body,
        /// with passwords and tokens redacted. Never throws.
        /// </summary>
        public static void LogRequest(Nancy.Request request)
        {
            try
            {
                string method = "?";
                try { method = request.Method ?? "?"; } catch { }
                string path = "?";
                try { path = request.Path ?? "?"; } catch { }
                string query = "";
                try
                {
                    query = request.Url != null ? (request.Url.Query ?? "") : "";
                    if (!string.IsNullOrEmpty(query))
                        query = System.Text.RegularExpressions.Regex.Replace(
                            query, @"(token|password|passwd|pwd|saPassword)=([^&\s]*)",
                            "$1=***", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                }
                catch { query = ""; }

                string bodySummary = "";
                try
                {
                    string verb = (method ?? "").ToUpperInvariant();
                    if ((verb == "POST" || verb == "PUT" || verb == "PATCH") && request.Body != null && request.Body.CanSeek && request.Body.Length > 0)
                    {
                        long pos = request.Body.Position;
                        try
                        {
                            request.Body.Position = 0;
                            string body = Nancy.IO.RequestStream.FromStream(request.Body).AsString();
                            if (!string.IsNullOrEmpty(body))
                            {
                                body = System.Text.RegularExpressions.Regex.Replace(
                                    body, "(\"(password|passwd|pwd|saPassword)\"\\s*:\\s*\")[^\"]*\"",
                                    "$1***\"", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                                body = System.Text.RegularExpressions.Regex.Replace(
                                    body, @"(password|passwd|pwd|saPassword|token)=([^&\s]*)",
                                    "$1=***", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                                if (body.Length > 2000) body = body.Substring(0, 2000) + "...[truncated]";
                                body = System.Text.RegularExpressions.Regex.Replace(body, @"\s+", " ");
                                bodySummary = " body=" + body;
                            }
                        }
                        finally
                        {
                            try { request.Body.Position = pos; } catch { }
                        }
                    }
                }
                catch { }

                Log(method + " " + path + query + bodySummary);
            }
            catch { }
        }

    }

}
