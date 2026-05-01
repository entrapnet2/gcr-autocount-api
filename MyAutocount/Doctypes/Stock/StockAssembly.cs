using System;
using Nancy;
using Nancy.Extensions;
using Newtonsoft.Json;
using static GCR_autocount_api.Utils;

namespace GCR_autocount_api.Doctypes.Stock
{
        public class StockAssembly : AuthenticatedModule
    {
        const string DoctypeName = "StockAssembly";
        const string DatabaseTable = "vStockAssembly";
        const string DetailTable = "vStockAssemblyDetail";

        AutoCount.Data.DBSetting dbSetting;
        AutoCount.Authentication.UserSession userSession;

        public StockAssembly()
        {
            dbSetting = Auth.dbSetting;
            userSession = Auth.userSession;
            Run();
        }

        public static void Test()
        {
            StockAssembly assembly = new StockAssembly();
        }

        private void Run()
        {
            Get($"/{DoctypeName}/getAll", _ =>
            {
                try { return GetAll(); }
                catch (Exception ex) { Log(ex.ToString()); return ex.Message; }
            });

            Get($"/{DoctypeName}/getSingle/{{docNo}}", args =>
            {
                try { return GetSingle(args.docNo); }
                catch (Exception ex) { Log(ex.ToString()); return ex.Message; }
            });

            Get($"/{DoctypeName}/getDetail/{{docNo}}", args =>
            {
                try { return GetDetail(args.docNo); }
                catch (Exception ex) { Log(ex.ToString()); return ex.Message; }
            });

            Post($"/{DoctypeName}/add", _ =>
            {
                try
                {
                    dynamic data = JsonConvert.DeserializeObject(Nancy.IO.RequestStream.FromStream(this.Request.Body).AsString());
                    return Add(data);
                }
                catch (Exception ex) { Log(ex.ToString()); return CreateErrorResponse(ex.Message); }
            });

            Put($"/{DoctypeName}/edit", _ =>
            {
                try
                {
                    dynamic data = JsonConvert.DeserializeObject(Nancy.IO.RequestStream.FromStream(this.Request.Body).AsString());
                    return Edit(data);
                }
                catch (Exception ex) { Log(ex.ToString()); return CreateErrorResponse(ex.Message); }
            });

            Delete($"/{DoctypeName}/delete/{{docNo}}", args =>
            {
                try { return Delete(args.docNo); }
                catch (Exception ex) { Log(ex.ToString()); return ex.Message; }
            });
        }

        private string GetAll() => Sql.GetAllFromSql(userSession, DatabaseTable);
        private string GetSingle(string docNo) => Sql.GetSingleFromSql(userSession, DatabaseTable, "DocNo", docNo);
        private string GetDetail(string docNo) => Sql.GetSingleDetailFromSql(userSession, DatabaseTable, DetailTable, docNo);

        private string Add(dynamic data)
        {
            if (!Auth.Login(userSession)) return $"{DoctypeName} add error: Login failed";

            try
            {
                string docNo = data[StockAssemblyConstants.DocNo];
                DateTime docDate = DateStringToDateTime(data[StockAssemblyConstants.DocDate].ToString());
                string description = data[StockAssemblyConstants.Description];

                var cmd = AutoCount.Manufacturing.StockAssembly.StockAssemblyCommand.Create(userSession, userSession.DBSetting);
                var doc = cmd.AddNew();
                doc.DocNo = docNo;
                doc.DocDate = docDate;
                doc.Description = description;

                // Add materials (use AddDetail for AutoCount2 API)
                if (data.Contains(StockAssemblyConstants.MaterialList))
                {
                    foreach (dynamic mat in data[StockAssemblyConstants.MaterialList])
                    {
                        var dtl = doc.AddDetail();
                        dtl.ItemCode = mat[StockAssemblyConstants.ItemCode].ToString();
                    }
                }

                doc.Save();
                Log($"{DoctypeName} added: {docNo}");
                PublishEvent("stock.assembly", "created", docNo, new { docNo, description = doc.Description, docDate = doc.DocDate });
                return $"{DoctypeName} added: {docNo}";
            }
            catch (Exception ex)
            {
                Log($"StockAssembly add error: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        private string Edit(dynamic data)
        {
            if (!Auth.Login(userSession)) return $"{DoctypeName} edit error: Login failed";

            try
            {
                string docNo = data[StockAssemblyConstants.DocNo];
                var cmd = AutoCount.Manufacturing.StockAssembly.StockAssemblyCommand.Create(userSession, userSession.DBSetting);
                var doc = cmd.Edit(docNo);
                doc.DocDate = DateStringToDateTime(data[StockAssemblyConstants.DocDate].ToString());
                doc.Description = data[StockAssemblyConstants.Description];

                doc.Save();
                Log($"{DoctypeName} edited: {docNo}");
                PublishEvent("stock.assembly", "updated", docNo, new { docNo, description = doc.Description });
                return $"{DoctypeName} edited: {docNo}";
            }
            catch (Exception ex)
            {
                Log($"StockAssembly edit error: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        private string Delete(string docNo)
        {
            if (!Auth.Login(userSession)) return $"{DoctypeName} delete error: Login failed";

            var cmd = AutoCount.Manufacturing.StockAssembly.StockAssemblyCommand.Create(userSession, userSession.DBSetting);
            cmd.Delete(docNo);
            Log($"{DoctypeName} deleted: {docNo}");
            PublishEvent("stock.assembly", "deleted", docNo);
            return $"{DoctypeName} deleted: {docNo}";
        }

        private Response CreateErrorResponse(string message)
        {
            var response = (Response)message;
            response.StatusCode = HttpStatusCode.InternalServerError;
            return response;
        }
    }

    internal static class StockAssemblyConstants
    {
        internal static string DocNo { get; } = "docNo";
        internal static string DocDate { get; } = "docDate";
        internal static string Description { get; } = "description";
        internal static string MaterialList { get; } = "materialList";
        internal static string ItemCode { get; } = "itemCode";
    }
}
