using System;
using Nancy;
using Nancy.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static GCR_autocount_api.Utils;

namespace GCR_autocount_api.Doctypes.Purchase
{
    public class GoodsReceivedNote : AuthenticatedModule
    {
        const string DoctypeName = "GoodsReceivedNote";
        const string DatabaseTable = "vGoodsReceivedNote";
        const string DetailTable = "vGoodsReceivedNoteDetail";
        const string PrimaryKey = "DocKey";

        AutoCount.Data.DBSetting dbSetting;
        AutoCount.Authentication.UserSession userSession;

        public GoodsReceivedNote()
        {
            dbSetting = Auth.dbSetting;
            userSession = Auth.userSession;
            Run();
        }

        private void Run()
        {
            Get($"/{DoctypeName}/getAll", _ =>
            {
                try { return GetAll(this.Request); }
                catch (Exception ex) { Log(ex.ToString()); return CreateErrorResponse(ex.Message); }
            });

            Get($"/{DoctypeName}/getSingle/{{docNo}}", args =>
            {
                try { return GetSingle(args.docNo); }
                catch (Exception ex) { Log(ex.ToString()); return CreateErrorResponse(ex.Message); }
            });

            Get($"/{DoctypeName}/getDetail/{{docNo}}", args =>
            {
                try { return GetSingleDetail(args.docNo); }
                catch (Exception ex) { Log(ex.ToString()); return CreateErrorResponse(ex.Message); }
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
                catch (Exception ex) { Log(ex.ToString()); return CreateErrorResponse(ex.Message); }
            });

            Get($"/{DoctypeName}/count", _ =>
            {
                try { return Sql.GetCountFromSql(userSession, DatabaseTable, this.Request); }
                catch (Exception ex) { Log(ex.ToString()); return CreateErrorResponse(ex.Message); }
            });
        }

        private string GetAll(Request request = null) => Sql.GetAllFromSql(userSession, DatabaseTable, request);
        private string GetSingle(string docNo) => Sql.GetSingleFromSql(userSession, DatabaseTable, "DocNo", docNo);
        private string GetSingleDetail(string docNo) => Sql.GetSingleDetailFromSql(userSession, DatabaseTable, DetailTable, docNo);

        private string Add(dynamic data)
        {
            if (!Auth.Login(userSession)) return $"{DoctypeName} add error: Login failed";

            string docNo = data.docNo;
            DateTime docDate = DateStringToDateTime(data.date.ToString());
            string creditorCode = data.creditorCode;
            string description = ((JObject)data).ContainsKey("description") ? data.description : "";

            var cmd = AutoCount.Invoicing.Purchase.GoodsReceivedNote.GoodsReceivedNoteCommand.Create(userSession, userSession.DBSetting);
            var doc = cmd.AddNew();

            if (!string.IsNullOrEmpty(docNo?.ToString()))
            {
                doc.DocNo = docNo;
            }
            doc.DocDate = docDate;
            doc.CreditorCode = creditorCode;
            doc.Description = description;

            if (((JObject)data).ContainsKey("detailList"))
            {
                foreach (dynamic item in data.detailList)
                {
                    var detail = doc.AddDetail();
                    detail.ItemCode = item.itemCode.ToString();
                    detail.UOM = item.uom.ToString();
                    detail.Qty = decimal.Parse(item.quantity.ToString());
                    if (((JObject)item).ContainsKey("unitPrice"))
                        detail.UnitPrice = decimal.Parse(item.unitPrice.ToString());
                    if (((JObject)item).ContainsKey("location"))
                        detail.Location = item.location.ToString();
                }
            }

            doc.Save();
            Log($"{DoctypeName} added: {doc.DocNo}");

            return $"{DoctypeName} added: {doc.DocNo}";
        }

        private string Edit(dynamic data)
        {
            if (!Auth.Login(userSession)) return $"{DoctypeName} edit error: Login failed";

            string docNo = data.docNo;
            var cmd = AutoCount.Invoicing.Purchase.GoodsReceivedNote.GoodsReceivedNoteCommand.Create(userSession, userSession.DBSetting);
            var doc = cmd.Edit(docNo);

            doc.DocDate = DateStringToDateTime(data.date.ToString());
            if (((JObject)data).ContainsKey("creditorCode")) doc.CreditorCode = data.creditorCode;
            if (((JObject)data).ContainsKey("description")) doc.Description = data.description;

            doc.ClearDetails();
            if (((JObject)data).ContainsKey("detailList"))
            {
                foreach (dynamic item in data.detailList)
                {
                    var detail = doc.AddDetail();
                    detail.ItemCode = item.itemCode.ToString();
                    detail.UOM = item.uom.ToString();
                    detail.Qty = decimal.Parse(item.quantity.ToString());
                    if (((JObject)item).ContainsKey("unitPrice"))
                        detail.UnitPrice = decimal.Parse(item.unitPrice.ToString());
                    if (((JObject)item).ContainsKey("location"))
                        detail.Location = item.location.ToString();
                }
            }

            doc.Save();
            Log($"{DoctypeName} edited: {docNo}");

            return $"{DoctypeName} edited: {docNo}";
        }

        private string Delete(string docNo)
        {
            if (!Auth.Login(userSession)) return $"{DoctypeName} delete error: Login failed";
            var cmd = AutoCount.Invoicing.Purchase.GoodsReceivedNote.GoodsReceivedNoteCommand.Create(userSession, userSession.DBSetting);
            cmd.Delete(docNo);
            Log($"{DoctypeName} deleted: {docNo}");

            return $"{DoctypeName} deleted: {docNo}";
        }

        private Response CreateErrorResponse(string message)
        {
            var response = (Response)message;
            response.StatusCode = HttpStatusCode.InternalServerError;
            return response;
        }
    }
}
