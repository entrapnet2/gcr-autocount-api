using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using Nancy.Extensions;
using Newtonsoft.Json;
using static GCR_autocount_api.Utils;

namespace GCR_autocount_api.Doctypes.Purchase
{
        public class CancelPurchaseOrder : AuthenticatedModule
    {
        const string DoctypeName = "CancelPurchaseOrder";
        const string PrimaryKey = "DocNo";

        const string DatabaseTable = "vCancelPO";
        const string DetailTable = "vCancelPODetail";

        AutoCount.Data.DBSetting dbSetting;
        AutoCount.Authentication.UserSession userSession;

        public CancelPurchaseOrder()
        {
            dbSetting = Auth.dbSetting;
            userSession = Auth.userSession;
            Run();
        }

        private void Run()
        {
            Get($"/{DoctypeName}/getAll", _ =>
            {
                try
                {
                    Response response = Sql.GetAllFromSql(userSession, DatabaseTable);
                    return response;
                }
                catch (Exception ex)
                {
                    Log(ex.ToString());
                    Response response = ex.Message;
                    response.StatusCode = HttpStatusCode.InternalServerError;
                    return response;
                }
            });

            Get($"/{DoctypeName}/getSingle/{{docNo}}", args =>
            {
                try
                {
                    Response response = Sql.GetSingleFromSql(userSession, DatabaseTable, PrimaryKey, args.docNo);
                    return response;
                }
                catch (Exception ex)
                {
                    Log(ex.ToString());
                    Response response = ex.Message;
                    response.StatusCode = HttpStatusCode.InternalServerError;
                    return response;
                }
            });

            Post($"/{DoctypeName}/add", args =>
            {
                try
                {
                    dynamic jsonData = Utils.ParseRequest(this.Request);
                    Response response = Add(jsonData);
                    return response;
                }
                catch (Exception ex)
                {
                    Log(ex.ToString());
                    Response response = ex.Message;
                    response.StatusCode = HttpStatusCode.InternalServerError;
                    return response;
                }
            });

            Delete($"/{DoctypeName}/delete/{{docNo}}", args => {
                try
                {
                    Response response = Delete(args.docNo);
                    return response;
                }
                catch (Exception ex)
                {
                    Log(ex.ToString());
                    Response response = ex.Message;
                    response.StatusCode = HttpStatusCode.InternalServerError;
                    return response;
                }
            });
        }

        private string Add(dynamic data)
        {
            if (Auth.Login(userSession))
            {
                string docNo = data[CancelPOConstants.DocNo];

                AutoCount.Invoicing.Purchase.CancelPO.CancelPOCommand cmd =
                    AutoCount.Invoicing.Purchase.CancelPO.CancelPOCommand.Create(userSession, userSession.DBSetting);
                AutoCount.Invoicing.Purchase.CancelPO.CancelPO doc = cmd.AddNew();

                doc.DocNo = docNo;
                doc.CreditorCode = data[CancelPOConstants.CreditorCode];
                doc.DocDate = DateStringToDateTime(data[CancelPOConstants.Date].ToString());

                dynamic detailList = data[CancelPOConstants.DetailList];

                foreach (dynamic detailObject in detailList)
                {
                    doc.PartialTransfer(
                        AutoCount.Invoicing.Purchase.TransferFrom.PurchaseOrder,
                        detailObject[CancelPOConstants.PurchaseOrderNo].ToString(),
                        detailObject[CancelPOConstants.ItemCode].ToString(),
                        detailObject[CancelPOConstants.Uom].ToString(),
                        decimal.Parse(detailObject[CancelPOConstants.Quantity].ToString()),
                        0M      // focQty 
                        );
                }

                doc.Save();
                Log($"{DoctypeName} added: {docNo}");
                PublishEvent("purchase.cancelpo", "created", docNo, new { docNo, creditorCode = doc.CreditorCode, docDate = doc.DocDate });
                return $"{DoctypeName} added: {docNo}";
            }
            Log($"{DoctypeName} add error: Login failed");
            return $"{DoctypeName} add error: Login failed";
        }

        private string Delete(string docNo)
        {
            if (Auth.Login(userSession))
            {
                AutoCount.Invoicing.Purchase.CancelPO.CancelPOCommand cmd =
                    AutoCount.Invoicing.Purchase.CancelPO.CancelPOCommand.Create(userSession, userSession.DBSetting);

                cmd.Delete(docNo);
                Log($"{DoctypeName} deleted: {docNo}");
                PublishEvent("purchase.cancelpo", "deleted", docNo);
                return $"{DoctypeName} deleted: {docNo}";
            }
            Log($"{DoctypeName} delete error: Login failed");
            return $"{DoctypeName} delete error: Login failed";
        }
    }

    internal static class CancelPOConstants
    {
        internal static string DocNo { get; } = "docNo";
        internal static string CreditorCode { get; } = "creditorCode";
        internal static string Date { get; } = "date";
        internal static string DetailList { get; } = "detailList";
        internal static string PurchaseOrderNo { get; } = "purchaseOrderNo";
        internal static string ItemCode { get; } = "itemCode";
        internal static string Uom { get; } = "uom";
        internal static string Quantity { get; } = "quantity";
    }
}
