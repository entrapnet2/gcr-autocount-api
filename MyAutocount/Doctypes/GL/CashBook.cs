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
                    return Utils.ToErrorResponse(ex);
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
                    return Utils.ToErrorResponse(ex);
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
                    return Utils.ToErrorResponse(ex);
                }
            });

            Post($"/{DoctypeName}/add", args =>
            {
                try
                {
                    dynamic jsonData = Utils.ParseRequest(this.Request);
                    return Add(jsonData);
                }
                catch (Exception ex)
                {
                    Log(ex.ToString());
                    return Utils.ToErrorResponse(ex);
                }
            });

            Put($"/{DoctypeName}/edit", args =>
            {
                try
                {
                    dynamic jsonData = Utils.ParseRequest(this.Request);
                    return Edit(jsonData);
                }
                catch (Exception ex)
                {
                    Log(ex.ToString());
                    return Utils.ToErrorResponse(ex);
                }
            });

            Delete($"/{DoctypeName}/delete/{{docNo}}", args =>
            {
                try
                {
                    return Delete(args.docNo);
                }
                catch (Exception ex)
                {
                    Log(ex.ToString());
                    return Utils.ToErrorResponse(ex);
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
                return Utils.ToErrorResponse(ex);
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
                return Utils.ToErrorResponse(ex);
            }
        }

        private object DeleteByQuery()
        {
            try
            {
                return Delete(QueryDocNo());
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
                return Utils.ToErrorResponse(ex);
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

        /// <summary>
        /// Reads an optional field accepting two spellings, e.g. projNo/project.
        /// </summary>
        private static string Pick(dynamic data, string a, string b)
        {
            try { if (data[a] != null && !string.IsNullOrEmpty(data[a].ToString())) return data[a].ToString(); }
            catch { }
            try { if (data[b] != null && !string.IsNullOrEmpty(data[b].ToString())) return data[b].ToString(); }
            catch { }
            return null;
        }

        private static void SetHeader(AutoCount.GL.CashBook.CashBook doc, dynamic data)
        {
            if (data.docNo != null && !string.IsNullOrEmpty(data.docNo.ToString()))
                doc.DocNo = data.docNo.ToString();
            if (data.docNoFormatName != null && !string.IsNullOrEmpty(data.docNoFormatName.ToString()))
                doc.DocNoFormatName = data.docNoFormatName.ToString();
            if (data.date != null)
                doc.DocDate = DateStringToDateTime(data.date.ToString());
            if (data.payTo != null)
                doc.PayTo = data.payTo.ToString();
            if (data.description != null)
                doc.Description = data.description.ToString();
            if (data.note != null)
                doc.Note = data.note.ToString();
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
                    string dProj = Pick(d, "projNo", "project");
                    if (!string.IsNullOrEmpty(dProj))
                        detail.ProjNo = dProj;
                    string dDept = Pick(d, "deptNo", "department");
                    if (!string.IsNullOrEmpty(dDept))
                        detail.DeptNo = dDept;
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

        private object Add(dynamic data)
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

                    return Response.AsJson(new { success = true, docNo = doc.DocNo, message = $"{DoctypeName} added: {doc.DocNo}" });
                }
                catch (Exception ex)
                {
                    Log($"{DoctypeName} add error: {ex.Message}");
                    throw;
                }
            }
            Log($"{DoctypeName} add error: Login failed");
            return Response.AsJson(new { success = false, message = $"{DoctypeName} add error: Login failed" });
        }

        private object Edit(dynamic data)
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

                return Response.AsJson(new { success = true, docNo = docNo, message = $"{DoctypeName} edited: {docNo}" });
            }
            Log($"{DoctypeName} edit error: Login failed");
            return Response.AsJson(new { success = false, message = $"{DoctypeName} edit error: Login failed" });
        }

        private object Delete(string docNo)
        {
            if (Auth.Login(userSession))
            {
                AutoCount.GL.CashBook.CashBookCommand cmd =
                    AutoCount.GL.CashBook.CashBookCommand.Create(userSession, userSession.DBSetting);

                cmd.Delete(docNo);
                Log($"{DoctypeName} deleted: {docNo}");

                return Response.AsJson(new { success = true, docNo = docNo, message = $"{DoctypeName} deleted: {docNo}" });
            }
            Log($"{DoctypeName} delete error: Login failed");
            return Response.AsJson(new { success = false, message = $"{DoctypeName} delete error: Login failed" });
        }
    }
}
