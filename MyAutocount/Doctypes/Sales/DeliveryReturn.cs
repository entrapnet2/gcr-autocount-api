using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using Nancy.Extensions;
using Newtonsoft.Json;
using static GCR_autocount_api.Utils;

namespace GCR_autocount_api.Doctypes.Sales
{
        public class DeliveryReturn : AuthenticatedModule
    {
        const string DoctypeName = "DeliveryReturn";
        const string PrimaryKey = "DocNo";

        const string DatabaseTable = "vDeliveryReturn";
        const string DetailTable = "vDeliveryReturnDetail";

        AutoCount.Data.DBSetting dbSetting;
        AutoCount.Authentication.UserSession userSession;

        public DeliveryReturn()
        {
            dbSetting = Auth.dbSetting;
            userSession = Auth.userSession;
            Run();
        }

        public static void Test()
        {
            CancelSO cancelSO = new CancelSO();

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

            Get($"/{DoctypeName}/getDetail/{{docNo}}", args => {
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


        private string GetAll()
        {
            return Sql.GetAllFromSql(userSession, DatabaseTable);

        }
        private string GetSingle(string docNo)
        {
            return Sql.GetSingleFromSql(userSession, DatabaseTable, PrimaryKey, docNo);
        }

        private string GetSingleDetail(string docNo)
        {
            return Sql.GetSingleDetailFromSql(userSession, DatabaseTable, DetailTable, docNo);
        }

        private string Add(dynamic data)
        {
            if (Auth.Login(userSession))
            {
                string docNo = data[DeliveryReturnConstants.DocNo];

                AutoCount.Invoicing.Sales.DeliveryReturn.DeliveryReturnCommand cmd =
                    AutoCount.Invoicing.Sales.DeliveryReturn.DeliveryReturnCommand.Create(userSession, userSession.DBSetting);
                AutoCount.Invoicing.Sales.DeliveryReturn.DeliveryReturn doc = cmd.AddNew();

                doc.DocNo = docNo;
                doc.DebtorCode = data[DeliveryReturnConstants.DebtorCode];
                doc.DocDate = DateStringToDateTime(data[DeliveryReturnConstants.Date].ToString());

                dynamic detailList = data[DeliveryReturnConstants.DetailList];

                foreach (dynamic detailObject in detailList)
                {
                    doc.PartialTransfer(
                        AutoCount.Invoicing.Sales.TransferFrom.DeliveryOrder,
                        detailObject[DeliveryReturnConstants.DeliveryOrderNo].ToString(),
                        detailObject[DeliveryReturnConstants.ItemCode].ToString(),
                        detailObject[DeliveryReturnConstants.Uom].ToString(),
                        decimal.Parse(detailObject[DeliveryReturnConstants.Quantity].ToString()),
                        0M      // focQty 
                        );
                }

                doc.Save();
                Log($"{DoctypeName} added: {docNo}");
                PublishEvent("sales.deliveryreturn", "created", docNo, new { docNo, debtorCode = doc.DebtorCode, docDate = doc.DocDate });
                return $"{DoctypeName} added: {docNo}";

            }
            Log($"{DoctypeName} add error: Login failed");
            return $"{DoctypeName} add error: Login failed";
        }


        private string Edit(dynamic data)
        {
            if (Auth.Login(userSession))
            {
                string docNo = data[DeliveryReturnConstants.DocNo];

                AutoCount.Invoicing.Sales.DeliveryReturn.DeliveryReturnCommand cmd =
                    AutoCount.Invoicing.Sales.DeliveryReturn.DeliveryReturnCommand.Create(userSession, userSession.DBSetting);

                // Autocount Bug: doc will return null if use cmd.Edit(string docNo)
                // So get docKey from docNo, then call cmd.Edit(long docKey)
                long docKey = cmd.GetDocKeyByDocNo(docNo);
                
                //AutoCount.Invoicing.Sales.DeliveryReturn.DeliveryReturn doc = cmd.Edit(docNo);
                AutoCount.Invoicing.Sales.DeliveryReturn.DeliveryReturn doc = cmd.Edit(docKey);

                doc.DebtorCode = data[DeliveryReturnConstants.DebtorCode];
                doc.DocDate = DateStringToDateTime(data[DeliveryReturnConstants.Date].ToString());

                print(data[DeliveryReturnConstants.DebtorCode]);

                dynamic detailList = data[DeliveryReturnConstants.DetailList];

                doc.ClearDetails();

                foreach (dynamic detailObject in detailList)
                {
                    doc.PartialTransfer(
                        AutoCount.Invoicing.Sales.TransferFrom.DeliveryOrder,
                        detailObject[DeliveryReturnConstants.DeliveryOrderNo].ToString(),
                        detailObject[DeliveryReturnConstants.ItemCode].ToString(),
                        detailObject[DeliveryReturnConstants.Uom].ToString(),
                        decimal.Parse(detailObject[DeliveryReturnConstants.Quantity].ToString()),
                        0M      // focQty 
                        );
                }

                doc.Save();
                Log($"{DoctypeName} edited: {docNo}");
                PublishEvent("sales.deliveryreturn", "updated", docNo, new { docNo, debtorCode = doc.DebtorCode });
                return $"{DoctypeName} edited: {docNo}";

            }
            Log($"{DoctypeName} edit error: Login failed");
            return $"{DoctypeName} edit error: Login failed";
        }

        private string Delete(string docNo)
        {
            if (Auth.Login(userSession))
            {
                AutoCount.Invoicing.Sales.DeliveryReturn.DeliveryReturnCommand cmd =
                    AutoCount.Invoicing.Sales.DeliveryReturn.DeliveryReturnCommand.Create(userSession, userSession.DBSetting);

                cmd.Delete(docNo);
                Log($"{DoctypeName} deleted: {docNo}");
                PublishEvent("sales.deliveryreturn", "deleted", docNo);
                return $"{DoctypeName} deleted: {docNo}";
            }
            Log($"{DoctypeName} delete error: Login failed");
            return $"{DoctypeName} delete error: Login failed";

        }
    }

    internal static class DeliveryReturnConstants
    {
        // Key of dynamic data fetched from API.
        internal static string DocNo { get; } = "docNo";
        internal static string DebtorCode { get; } = "debtorCode";
        internal static string Date { get; } = "date";

        internal static string DetailList { get; } = "detailList";
        internal static string DeliveryOrderNo { get; } = "deliveryOrderNo";
        internal static string ItemCode { get; } = "itemCode";
        internal static string Uom { get; } = "uom";
        internal static string Quantity { get; } = "quantity";

    }
}

