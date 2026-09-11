using System;
using System.Collections.Generic;
using Nancy;
using Newtonsoft.Json;
using static GCR_autocount_api.Utils;

namespace GCR_autocount_api.Doctypes
{
    public class DocNoFormat : AuthenticatedModule
    {
        AutoCount.Data.DBSetting dbSetting;
        AutoCount.Authentication.UserSession userSession;

        static Dictionary<string, string> docTypeNameMap = new Dictionary<string, string>
        {
            {"OR", "Official Receipt"},
            {"PV", "Payment Voucher"},
            {"JE", "Journal Entry"},
            {"KS", "KIV"},
            {"AQ", "Sales Agent Quotation"},
            {"QT", "Quotation"},
            {"SO", "Sales Order"},
            {"DO", "Delivery Order"},
            {"IV", "Sales Invoice"},
            {"CS", "Cash Sale"},
            {"CN", "Credit Note"},
            {"DN", "Debit Note"},
            {"XS", "Cancel Sales Order"},
            {"DR", "Delivery Return"},
            {"CG", "Cash Given"},
            {"CR", "Cash Received"},
            {"BN", "Banking"},
            {"PQ", "Purchase Quotation"},
            {"RQ", "Request Quotation"},
            {"PO", "Purchase Order"},
            {"GR", "Goods Received Note"},
            {"PI", "Purchase Invoice"},
            {"CP", "Cash Purchase"},
            {"PR", "Purchase Return"},
            {"XP", "Cancel Purchase Order"},
            {"GT", "Goods Transfer Note"},
            {"SG", "Sub Goods Received Note"},
            {"PG", "Purchase Goods Return"},
            {"NR", "No Receipt"},
            {"PD", "Purchase Debit Note"},
            {"SA", "Stock Adjustment"},
            {"ST", "Stock Take"},
            {"SK", "Stock Take KIV"},
            {"SR", "Stock Receive"},
            {"SI", "Stock Issue"},
            {"WO", "Stock Write Off"},
            {"UC", "Stock Update Cost"},
            {"UT", "Stock Transfer"},
            {"AS", "Stock Assembly"},
            {"AO", "Assembly Order"},
            {"DA", "Disassembly Order"},
            {"SB", "Self-Billed Invoice"},
            {"CI", "Consolidated Invoice"},
            {"AT", "InvoiceNow"},
            {"BR", "Bounced Reversal"},
            {"PA", "Payment Advice"},
            {"GI", "GST Input"},
            {"S#", "Stock Section"}
        };

        public DocNoFormat()
        {
            dbSetting = Auth.dbSetting;
            userSession = Auth.userSession;
            Run();
        }

        private void Run()
        {
            Get($"/DocNoFormat/getDocTypes", _ =>
            {
                try { return GetDocTypes(); }
                catch (Exception ex) { Log(ex.ToString()); return CreateErrorResponse(ex.Message); }
            });

            Get($"/DocNoFormat/getAll", _ =>
            {
                try { return GetAll(this.Request); }
                catch (Exception ex) { Log(ex.ToString()); return CreateErrorResponse(ex.Message); }
            });

            Get($"/DocNoFormat/count", _ =>
            {
                try { return GetCount(this.Request); }
                catch (Exception ex) { Log(ex.ToString()); return CreateErrorResponse(ex.Message); }
            });
        }

        private string GetDocTypes()
        {
            if (!Auth.Login(userSession)) return "DocNoFormat getDocTypes error: Login failed";

            try
            {
                var docTypeList = AutoCount.Document.DocumentType.GetDocTypeListForDocNoFormat();
                var result = new List<object>();

                foreach (string docType in docTypeList)
                {
                    string name = docTypeNameMap.ContainsKey(docType)
                        ? docTypeNameMap[docType]
                        : AutoCount.Document.DocumentType.GetCategoryOfDocTypeForDocNoFormat(docType);

                    result.Add(new { code = docType, name = name });
                }

                return JsonConvert.SerializeObject(result);
            }
            catch (Exception ex)
            {
                Log("GetDocTypeListForDocNoFormat failed: " + ex.Message);
                return JsonConvert.SerializeObject(new List<object>());
            }
        }

        private string GetAll(Request request)
        {
            return Sql.GetAllFromSql(userSession, "DocNoFormat", request);
        }

        private string GetCount(Request request)
        {
            return Sql.GetCountFromSql(userSession, "DocNoFormat", request);
        }

        private Response CreateErrorResponse(string message)
        {
            var response = (Response)message;
            response.StatusCode = HttpStatusCode.InternalServerError;
            return response;
        }
    }
}
