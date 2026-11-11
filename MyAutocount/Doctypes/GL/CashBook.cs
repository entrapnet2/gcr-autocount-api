using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using Nancy.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static GCR_autocount_api.Utils;

namespace GCR_autocount_api.Doctypes.GL
{
    /// <summary>
    /// Cash Book vouchers (Cash Payment and Cash Receipt).
    /// Direct GL postings with no knock-off — for AR/AP knock-off see Receipt/Payment modules (read-only).
    /// </summary>
    public class CashBook : AuthenticatedModule
    {
        const string DoctypeName = "CashBook";
        const string PrimaryKey = "DocNo";

        const string DatabaseTable = "vCashBook";
        const string DetailTable = "vCashBookDTL";

        AutoCount.Data.DBSetting dbSetting;
        AutoCount.Authentication.UserSession userSession;

        public CashBook()
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
                    return Sql.GetAllFromSql(userSession, DatabaseTable, this.Request);
                }
                catch (Exception ex)
                {
                    Log(ex.ToString());
                    return Utils.CreateErrorResponse(ex.Message);
                }
            });

            Get($"/{DoctypeName}/getSingle/{{docNo}}", args =>
            {
                try
                {
                    return Sql.GetSingleFromSql(userSession, DatabaseTable, PrimaryKey, args.docNo);
                }
                catch (Exception ex)
                {
                    Log(ex.ToString());
                    return Utils.CreateErrorResponse(ex.Message);
                }
            });

            Get($"/{DoctypeName}/getDetail/{{docNo}}", args =>
            {
                try
                {
                    return Sql.GetSingleDetailFromSql(userSession, DatabaseTable, DetailTable, args.docNo);
                }
                catch (Exception ex)
                {
                    Log(ex.ToString());
                    return Utils.CreateErrorResponse(ex.Message);
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

            // ?docNo= fallbacks for auto-generated numbers containing '/' (e.g. P/OR-26-11-001),
            // which cannot travel in a URL path segment even when encoded.
            Get($"/{DoctypeName}/getSingle", _ => GetSingleByQuery());
            Get($"/{DoctypeName}/getDetail", _ => GetDetailByQuery());
            Delete($"/{DoctypeName}/delete", _ => DeleteByQuery());

            Get($"/{DoctypeName}/count", _ =>
            {
                try
                {
                    return Sql.GetCountFromSql(userSession, DatabaseTable, this.Request);
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
        private string QueryDocNo()
        {
            string docNo = this.Request.Query.docNo;
            if (string.IsNullOrEmpty(docNo))
                throw new Exception("docNo query parameter required");
            return docNo;
        }

        private object GetSingleByQuery()
        {
            try
            {
                return Sql.GetSingleFromSql(userSession, DatabaseTable, PrimaryKey, QueryDocNo());
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
                return Utils.CreateErrorResponse(ex.Message);
            }
        }

        private object GetDetailByQuery()
        {
            try
            {
                return Sql.GetSingleDetailFromSql(userSession, DatabaseTable, DetailTable, QueryDocNo());
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
                return Utils.CreateErrorResponse(ex.Message);
            }
        }

        private object DeleteByQuery()
        {
            try
            {
                Response response = Delete(QueryDocNo());
                return response;
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
                Response response = ex.Message;
                response.StatusCode = HttpStatusCode.InternalServerError;
                return response;
            }
        }

        private static AutoCount.GL.CashBook.CashBookType ParseType(dynamic data)
        {
            string type = "payment";
            try { if (data.type != null) type = data.type.ToString().ToLower(); } catch { }
            return type == "receipt"
                ? AutoCount.GL.CashBook.CashBookType.CashReceipt
                : AutoCount.GL.CashBook.CashBookType.CashPayment;
        }

        private static void SetHeader(AutoCount.GL.CashBook.CashBook doc, dynamic data)
        {
            if (data.docNo != null && !string.IsNullOrEmpty(data.docNo.ToString()))
                doc.DocNo = data.docNo.ToString();
            if (data.date != null)
                doc.DocDate = DateStringToDateTime(data.date.ToString());
            if (data.payTo != null)
                doc.PayTo = data.payTo.ToString();
            if (data.description != null)
                doc.Description = data.description.ToString();
            if (data.currencyCode != null)
                doc.CurrencyCode = data.currencyCode.ToString();
            if (data.currencyRate != null)
                doc.CurrencyRate = decimal.Parse(data.currencyRate.ToString());
        }

        private static void AddDetails(AutoCount.GL.CashBook.CashBook doc, dynamic data)
        {
            if (data.payments != null)
            {
                foreach (dynamic p in data.payments)
                {
                    AutoCount.GL.CashBook.CashBookPaymentDetail payment = doc.AddPaymentDetail();
                    if (p.paymentMethod != null)
                        payment.PaymentMethod = p.paymentMethod.ToString();
                    if (p.chequeNo != null)
                        payment.ChequeNo = p.chequeNo.ToString();
                    if (p.bankCharge != null)
                        payment.BankCharge = decimal.Parse(p.bankCharge.ToString());
                    if (p.paymentAmount != null)
                        payment.PaymentAmount = decimal.Parse(p.paymentAmount.ToString());
                }
            }

            if (data.details != null)
            {
                foreach (dynamic d in data.details)
                {
                    AutoCount.GL.CashBook.CashBookDetail detail = doc.AddDetail();
                    if (d.accNo != null)
                        detail.AccNo = d.accNo.ToString();
                    if (d.description != null)
                        detail.Description = d.description.ToString();
                    if (d.amount != null)
                        detail.Amount = decimal.Parse(d.amount.ToString());
                    if (d.taxCode != null)
                        detail.TaxCode = d.taxCode.ToString();
                    if (d.inclusiveTax != null)
                        detail.InclusiveTax = bool.Parse(d.inclusiveTax.ToString());
                }
            }
        }

        private static void ClearDetails(AutoCount.GL.CashBook.CashBook doc)
        {
            int n = 0;
            try { n = doc.GetValidPaymentRows().Length; } catch { }
            for (int i = 0; i < n; i++)
            {
                try { doc.DeletePaymentDetail(0); } catch { break; }
            }
            try { n = doc.GetValidDetailRows().Length; } catch { n = 0; }
            for (int i = 0; i < n; i++)
            {
                try { doc.DeleteDetail(0); } catch { break; }
            }
        }

        private string Add(dynamic data)
        {
            if (Auth.Login(userSession))
            {
                try
                {
                    AutoCount.GL.CashBook.CashBookCommand cmd =
                        AutoCount.GL.CashBook.CashBookCommand.Create(userSession, userSession.DBSetting);
                    AutoCount.GL.CashBook.CashBook doc = cmd.AddNew(ParseType(data));

                    SetHeader(doc, data);
                    AddDetails(doc, data);

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

                AutoCount.GL.CashBook.CashBookCommand cmd =
                    AutoCount.GL.CashBook.CashBookCommand.Create(userSession, userSession.DBSetting);
                AutoCount.GL.CashBook.CashBook doc = cmd.Edit(docNo);

                SetHeader(doc, data);
                ClearDetails(doc);
                AddDetails(doc, data);

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
                AutoCount.GL.CashBook.CashBookCommand cmd =
                    AutoCount.GL.CashBook.CashBookCommand.Create(userSession, userSession.DBSetting);

                cmd.Delete(docNo);
                Log($"{DoctypeName} deleted: {docNo}");

                return $"{DoctypeName} deleted: {docNo}";
            }
            Log($"{DoctypeName} delete error: Login failed");
            return $"{DoctypeName} delete error: Login failed";
        }
    }
}
