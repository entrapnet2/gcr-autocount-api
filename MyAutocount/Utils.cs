using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
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
            print($"{PrintTime()} : {str}");
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

    }

}
