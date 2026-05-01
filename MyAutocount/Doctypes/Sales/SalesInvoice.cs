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
    public class SalesInvoice : AuthenticatedModule
    {
        const string DoctypeName = "SalesInvoice";
        const string PrimaryKey = "DocNo";

        const string DatabaseTable = "vInvoice";
        const string DetailTable = "vInvoiceDetail";

        AutoCount.Data.DBSetting dbSetting;
        AutoCount.Authentication.UserSession userSession;

        public SalesInvoice()
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
                    return Utils.CreateErrorResponse(ex.Message);
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
                    return Utils.CreateErrorResponse(ex.Message);
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
                try
                {
                    string docNo = data.docNo;

                    AutoCount.Invoicing.Sales.Invoice.InvoiceCommand cmd =
                        AutoCount.Invoicing.Sales.Invoice.InvoiceCommand.Create(userSession, userSession.DBSetting);
                    AutoCount.Invoicing.Sales.Invoice.Invoice doc = cmd.AddNew();

                    if (!string.IsNullOrEmpty(docNo?.ToString()))
                    {
                        doc.DocNo = docNo;
                    }
                    doc.DebtorCode = data.debtorCode;
                    doc.DocDate = DateStringToDateTime(data.date.ToString());

                    if (data.description != null)
                        doc.Description = data.description.ToString();

                    if (data.currencyRate != null)
                        doc.CurrencyRate = decimal.Parse(data.currencyRate.ToString());

                    if (data.agent != null)
                        doc.Agent = data.agent.ToString();

                    if (data.shipInfo != null)
                        doc.ShipInfo = data.shipInfo;

                    if (data.detailList != null)
                    {
                        dynamic detailList = data.detailList;

                        foreach (dynamic detailObject in detailList)
                        {
                            AutoCount.Invoicing.Sales.Invoice.InvoiceDetail detail = doc.AddDetail();

                            if (detailObject.accNo != null)
                                detail.AccNo = detailObject.accNo.ToString();

                            if (detailObject.itemCode != null)
                                detail.ItemCode = detailObject.itemCode.ToString();

                            if (detailObject.description != null)
                                detail.Description = detailObject.description.ToString();
                            else if (detailObject.itemCode == null && detailObject.accNo == null)
                                detail.Description = "Particulars";

                            if (detailObject.location != null)
                                detail.Location = detailObject.location.ToString();

                            if (detailObject.project != null)
                                detail.ProjNo = detailObject.project.ToString();

                            if (detailObject.department != null)
                                detail.DeptNo = detailObject.department.ToString();

                            if (detailObject.uom != null)
                                detail.UOM = detailObject.uom.ToString();

                            if (detailObject.quantity != null)
                                detail.Qty = decimal.Parse(detailObject.quantity.ToString());

                            if (detailObject.unitPrice != null)
                                detail.UnitPrice = decimal.Parse(detailObject.unitPrice.ToString());

                            if (detailObject.discount != null)
                                detail.Discount = detailObject.discount.ToString();

                            if (detailObject.amount != null)
                                detail.SubTotal = decimal.Parse(detailObject.amount.ToString());

                            if (detailObject.taxCode != null)
                                detail.TaxCode = detailObject.taxCode.ToString();
                        }
                    }

                    doc.Save();
                    Log($"{DoctypeName} added: {doc.DocNo}");
                    return $"{DoctypeName} added: {doc.DocNo}";
                }
                catch (Exception ex)
                {
                    Log($"{DoctypeName} add error: {ex.Message}");
                    throw;
                }
            }
            Log($"{DoctypeName} add error: Login failed");
            return $"{DoctypeName} add error: Login failed";
        }

        private string Edit(dynamic data)
        {
            if (Auth.Login(userSession))
            {
                string docNo = data.docNo;

                AutoCount.Invoicing.Sales.Invoice.InvoiceCommand cmd =
                    AutoCount.Invoicing.Sales.Invoice.InvoiceCommand.Create(userSession, userSession.DBSetting);
                AutoCount.Invoicing.Sales.Invoice.Invoice doc = cmd.Edit(docNo);

                doc.DebtorCode = data.debtorCode;
                doc.DocDate = DateStringToDateTime(data.date.ToString());

                if (data.description != null)
                    doc.Description = data.description.ToString();

                if (data.currencyRate != null)
                    doc.CurrencyRate = decimal.Parse(data.currencyRate.ToString());

                if (data.agent != null)
                    doc.Agent = data.agent.ToString();

                if (data.shipInfo != null)
                    doc.ShipInfo = data.shipInfo;

                if (data.detailList != null)
                {
                    dynamic detailList = data.detailList;

                    doc.ClearDetails();

                    foreach (dynamic detailObject in detailList)
                    {
                        AutoCount.Invoicing.Sales.Invoice.InvoiceDetail detail = doc.AddDetail();

                        if (detailObject.accNo != null)
                            detail.AccNo = detailObject.accNo.ToString();

                        if (detailObject.itemCode != null)
                            detail.ItemCode = detailObject.itemCode.ToString();

                        if (detailObject.description != null)
                            detail.Description = detailObject.description.ToString();
                        else if (detailObject.itemCode == null && detailObject.accNo == null)
                            detail.Description = "Particulars";

                        if (detailObject.location != null)
                            detail.Location = detailObject.location.ToString();

                        if (detailObject.project != null)
                            detail.ProjNo = detailObject.project.ToString();

                        if (detailObject.department != null)
                            detail.DeptNo = detailObject.department.ToString();

                        if (detailObject.uom != null)
                            detail.UOM = detailObject.uom.ToString();

                        if (detailObject.quantity != null)
                            detail.Qty = decimal.Parse(detailObject.quantity.ToString());

                        if (detailObject.unitPrice != null)
                            detail.UnitPrice = decimal.Parse(detailObject.unitPrice.ToString());

                        if (detailObject.discount != null)
                            detail.Discount = detailObject.discount.ToString();

                        if (detailObject.amount != null)
                            detail.SubTotal = decimal.Parse(detailObject.amount.ToString());

                        if (detailObject.taxCode != null)
                            detail.TaxCode = detailObject.taxCode.ToString();
                    }
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
                AutoCount.Invoicing.Sales.Invoice.InvoiceCommand cmd =
                    AutoCount.Invoicing.Sales.Invoice.InvoiceCommand.Create(userSession, userSession.DBSetting);

                cmd.Delete(docNo);
                Log($"{DoctypeName} deleted: {docNo}");
                return $"{DoctypeName} deleted: {docNo}";
            }
            Log($"{DoctypeName} delete error: Login failed");
            return $"{DoctypeName} delete error: Login failed";
        }
    }
}
