using System;
using System.Collections.Generic;
using Nancy;
using Nancy.Extensions;
using Newtonsoft.Json;
using static GCR_autocount_api.Utils;

namespace GCR_autocount_api.Doctypes.Stock
{
        public class StockTransfer : AuthenticatedModule
    {
        const string DoctypeName = "StockTransfer";
        const string PrimaryKey = "DocKey";
        const string DatabaseTable = "vStockTransfer";
        const string DetailTable = "vStockTransferDetail";

        AutoCount.Data.DBSetting dbSetting;
        AutoCount.Authentication.UserSession userSession;

        public StockTransfer()
        {
            dbSetting = Auth.dbSetting;
            userSession = Auth.userSession;
            Run();
        }

        public static void Test()
        {
            StockTransfer transfer = new StockTransfer();
        }

        private void Run()
        {
            Get($"/{DoctypeName}/getAll", _ =>
            {
                try
                {
                    Response response = GetAll();
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
                    Response response = GetSingle(args.docNo);
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

            Get($"/{DoctypeName}/getDetail/{{docNo}}", args =>
            {
                try
                {
                    Response response = GetSingleDetail(args.docNo);
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

            Put($"/{DoctypeName}/edit", args =>
            {
                try
                {
                    dynamic jsonData = Utils.ParseRequest(this.Request);

                    Response response = Edit(jsonData);
                    response.StatusCode = HttpStatusCode.OK;
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

            Delete($"/{DoctypeName}/delete/{{docNo}}", args =>
            {
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

        private string GetAll()
        {
            return Sql.GetAllFromSql(userSession, DatabaseTable);
        }

        private string GetSingle(string docNo)
        {
            return Sql.GetSingleFromSql(userSession, DatabaseTable, "DocNo", docNo);
        }

        private string GetSingleDetail(string docNo)
        {
            return Sql.GetSingleDetailFromSql(userSession, DatabaseTable, DetailTable, docNo);
        }

        private string Add(dynamic data)
        {
            if (Auth.Login(userSession))
            {
                string docNo = data[StockTransferConstants.DocNo];
                DateTime docDate = DateStringToDateTime(data[StockTransferConstants.DocDate].ToString());
                string fromLocation = data[StockTransferConstants.FromLocation];
                string toLocation = data[StockTransferConstants.ToLocation];
                string reason = data.Contains(StockTransferConstants.Reason) ? data[StockTransferConstants.Reason] : "";

                AutoCount.Stock.StockTransfer.StockTransferCommand cmd =
                    AutoCount.Stock.StockTransfer.StockTransferCommand.Create(userSession, userSession.DBSetting);

                AutoCount.Stock.StockTransfer.StockTransfer doc = cmd.AddNew();
                doc.DocNo = docNo;
                doc.DocDate = docDate;
                doc.FromLocation = fromLocation;
                doc.ToLocation = toLocation;
                doc.Reason = reason;

                dynamic detailList = data[StockTransferConstants.DetailList];
                foreach (dynamic detailObject in detailList)
                {
                    var detail = doc.AddDetail();
                    detail.ItemCode = detailObject[StockTransferConstants.ItemCode].ToString();
                    // AutoCount2 uses UOM instead of Uom
                    detail.UOM = detailObject[StockTransferConstants.Uom].ToString();
                    detail.Qty = decimal.Parse(detailObject[StockTransferConstants.Quantity].ToString());
                }

                doc.Save();
                Log($"{DoctypeName} added: {docNo}");
                return $"{DoctypeName} added: {docNo}";
            }
            Log($"{DoctypeName} add error: Login failed");
            return $"{DoctypeName} add error: Login failed";
        }

        private string Edit(dynamic data)
        {
            if (Auth.Login(userSession))
            {
                string docNo = data[StockTransferConstants.DocNo];

                AutoCount.Stock.StockTransfer.StockTransferCommand cmd =
                    AutoCount.Stock.StockTransfer.StockTransferCommand.Create(userSession, userSession.DBSetting);

                AutoCount.Stock.StockTransfer.StockTransfer doc = cmd.Edit(docNo);

                doc.DocDate = DateStringToDateTime(data[StockTransferConstants.DocDate].ToString());
                doc.FromLocation = data[StockTransferConstants.FromLocation];
                doc.ToLocation = data[StockTransferConstants.ToLocation];
                if (data.Contains(StockTransferConstants.Reason))
                    doc.Reason = data[StockTransferConstants.Reason];

                doc.ClearDetails();
                dynamic detailList = data[StockTransferConstants.DetailList];
                foreach (dynamic detailObject in detailList)
                {
                    var detail = doc.AddDetail();
                    detail.ItemCode = detailObject[StockTransferConstants.ItemCode].ToString();
                    // AutoCount2 uses UOM
                    detail.UOM = detailObject[StockTransferConstants.Uom].ToString();
                    detail.Qty = decimal.Parse(detailObject[StockTransferConstants.Quantity].ToString());
                }

                doc.Save();
                Log($"{DoctypeName} edited: {docNo}");
                return $"{DoctypeName} edited: {docNo}";
            }
            Log($"{DoctypeName} edit error: Login failed");
            return $"{DoctypeName} edit error: Login failed";
        }

        private string Delete(string docNo)
        {
            if (Auth.Login(userSession))
            {
                AutoCount.Stock.StockTransfer.StockTransferCommand cmd =
                    AutoCount.Stock.StockTransfer.StockTransferCommand.Create(userSession, userSession.DBSetting);

                cmd.Delete(docNo);
                Log($"{DoctypeName} deleted: {docNo}");
                return $"{DoctypeName} deleted: {docNo}";
            }
            return $"{DoctypeName} delete error: Login failed";
        }
    }

    internal static class StockTransferConstants
    {
        internal static string DocNo { get; } = "docNo";
        internal static string DocDate { get; } = "docDate";
        internal static string FromLocation { get; } = "fromLocation";
        internal static string ToLocation { get; } = "toLocation";
        internal static string Reason { get; } = "reason";
        internal static string DetailList { get; } = "detailList";
        internal static string ItemCode { get; } = "itemCode";
        internal static string Uom { get; } = "uom";
        internal static string Quantity { get; } = "quantity";
    }
}
