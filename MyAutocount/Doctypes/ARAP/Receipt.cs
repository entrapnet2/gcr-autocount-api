using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using Nancy.Extensions;
using Newtonsoft.Json;
using static GCR_autocount_api.Utils;

namespace GCR_autocount_api.Doctypes.ARAP
{
    /// <summary>
    /// Customer receipts (Official Receipts) with knock-off against invoices/debit notes.
    /// API: AutoCount.ARAP.ARPayment.ARPaymentDataAccess (NuGet AutoCount2.Accounting).
    /// </summary>
    public class Receipt : AuthenticatedModule
    {
        const string DoctypeName = "Receipt";
        const string PrimaryKey = "DocNo";

        const string DatabaseTable = "vARPayment";
        const string DetailTable = "vARPaymentDetail";

        AutoCount.Data.DBSetting dbSetting;
        AutoCount.Authentication.UserSession userSession;

        public Receipt()
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
                    Response response = ex.Message;
                    response.StatusCode = HttpStatusCode.InternalServerError;
                    return response;
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
                    Response response = ex.Message;
                    response.StatusCode = HttpStatusCode.InternalServerError;
                    return response;
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
                    Response response = ex.Message;
                    response.StatusCode = HttpStatusCode.InternalServerError;
                    return response;
                }
            });

            Get($"/{DoctypeName}/getKnockOff/{{docNo}}", args =>
            {
                try
                {
                    return GetKnockOff(args.docNo);
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

            // ?docNo= fallbacks for auto-generated numbers containing '/' (e.g. P/OR-26-11-002),
            // which cannot travel in a URL path segment even when encoded.
            Get($"/{DoctypeName}/getSingle", _ => GetSingleByQuery());
            Get($"/{DoctypeName}/getDetail", _ => GetDetailByQuery());
            Get($"/{DoctypeName}/getKnockOff", _ => GetKnockOffByQuery());
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

        private string GetKnockOff(string docNo)
        {            string dbName = userSession.DBSetting.DBName;
            string query = "SELECT k.KnockOffDocType AS DocType, k.KnockOffDocKey AS DocKey, " +
                "COALESCE(iv.DocNo, dn.DocNo) AS DocNo, COALESCE(iv.DocDate, dn.DocDate) AS DocDate, k.Amount AS KnockOffAmt " +
                "FROM [" + dbName + "].[dbo].[ARPaymentKnockOff] k " +
                "INNER JOIN [" + dbName + "].[dbo].[vARPayment] r ON r.DocKey = k.DocKey " +
                "LEFT JOIN [" + dbName + "].[dbo].[ARInvoice] iv ON iv.DocKey = k.KnockOffDocKey AND k.KnockOffDocType = 'RI' " +
                "LEFT JOIN [" + dbName + "].[dbo].[ARDN] dn ON dn.DocKey = k.KnockOffDocKey AND k.KnockOffDocType = 'RD' " +
                "WHERE r.DocNo = @docNo ORDER BY k.KnockOffDocType, COALESCE(iv.DocNo, dn.DocNo)";
            return Sql.RunSqlQuery(userSession, query, new object[] { new SqlParameter("docNo", (string)docNo) });
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

        private object GetKnockOffByQuery()
        {
            try
            {
                return GetKnockOff(QueryDocNo());
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
                Response response = ex.Message;
                response.StatusCode = HttpStatusCode.InternalServerError;
                return response;
            }
        }

        private static string MapDocType(string docType)
        {
            if (string.IsNullOrEmpty(docType)) return AutoCount.Document.DocumentType.ARInvoice;
            string t = docType.Trim().ToUpper();
            if (t == "DN" || t == "RD") return AutoCount.Document.DocumentType.ARDN;
            if (t == "RI" || t == "IV" || t == "INVOICE") return AutoCount.Document.DocumentType.ARInvoice;
            return t;
        }

        private static void SetHeader(AutoCount.ARAP.ARPayment.ARPaymentEntity doc, dynamic data)
        {
            if (data.debtorCode != null)
                doc.DebtorCode = data.debtorCode.ToString();
            if (data.docNo != null && !string.IsNullOrEmpty(data.docNo.ToString()))
                doc.DocNo = data.docNo.ToString();
            if (data.docNo2 != null)
                doc.DocNo2 = data.docNo2.ToString();
            if (data.date != null)
                doc.DocDate = DateStringToDateTime(data.date.ToString());
            if (data.description != null)
                doc.Description = data.description.ToString();
            if (data.docNoFormatName != null)
                doc.DocNoFormatName = data.docNoFormatName.ToString();
            if (data.currencyCode != null)
                doc.CurrencyCode = data.currencyCode.ToString();
            if (data.note != null)
                doc.Note = data.note.ToString();
            string projNo = Pick(data, "projNo", "project");
            if (!string.IsNullOrEmpty(projNo))
                doc.ProjNo = projNo;
            string deptNo = Pick(data, "deptNo", "department");
            if (!string.IsNullOrEmpty(deptNo))
                doc.DeptNo = deptNo;
        }

        private static void AddDetails(AutoCount.ARAP.ARPayment.ARPaymentEntity doc, dynamic data)
        {
            if (data.payments == null) return;
            foreach (dynamic p in data.payments)
            {
                AutoCount.ARAP.ARPayment.ARPaymentDTLEntity dtl = doc.NewDetail();
                if (p.paymentMethod != null)
                    dtl.PaymentMethod = p.paymentMethod.ToString();
                if (p.chequeNo != null)
                    dtl.ChequeNo = p.chequeNo.ToString();
                if (p.paymentAmount != null)
                    dtl.PaymentAmt = decimal.Parse(p.paymentAmount.ToString());
                if (p.bankCharge != null)
                    dtl.BankCharge = decimal.Parse(p.bankCharge.ToString());
                if (p.paymentBy != null)
                    dtl.PaymentBy = p.paymentBy.ToString();
            }
        }

        private static void AddKnockOff(AutoCount.ARAP.ARPayment.ARPaymentEntity doc, dynamic data)
        {
            if (data.knockOff == null) return;
            foreach (dynamic k in data.knockOff)
            {
                string docType = MapDocType(k.docType != null ? k.docType.ToString() : null);
                string knockDocNo = k.docNo.ToString();
                decimal amount = decimal.Parse(k.amount.ToString());
                if (k.date != null && !string.IsNullOrEmpty(k.date.ToString()))
                    doc.KnockOff(docType, knockDocNo, amount, DateStringToDateTime(k.date.ToString()));
                else
                    doc.KnockOff(docType, knockDocNo, amount);
            }
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

        /// <summary>
        /// The receipt master keeps header ProjNo natively, but the GL postings
        /// created at save time do not inherit it, so it is applied to this
        /// receipt's GL lines (SourceType RP) right after a successful save.
        /// </summary>
        private void ApplyProjectToGL(string docNo, dynamic data)
        {
            try
            {
                string projNo = Pick(data, "projNo", "project");
                if (string.IsNullOrEmpty(projNo)) return;
                string dbName = userSession.DBSetting.DBName;
                using (var conn = new SqlConnection(userSession.DBSetting.ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(
                        "UPDATE [" + dbName + "].[dbo].[GLDTL] SET ProjNo = @p " +
                        "WHERE SourceType = 'RP' AND SourceKey = " +
                        "(SELECT DocKey FROM [" + dbName + "].[dbo].[ARPayment] WHERE DocNo = @d)", conn))
                    {
                        cmd.Parameters.AddWithValue("@p", projNo);
                        cmd.Parameters.AddWithValue("@d", docNo);
                        cmd.ExecuteNonQuery();
                    }
                }
                Log($"{DoctypeName} GL project applied: {docNo} -> {projNo}");
            }
            catch (Exception ex)
            {
                Log($"{DoctypeName} GL project warning: {ex.Message}");
            }
        }

        private object Add(dynamic data)
        {
            if (Auth.Login(userSession))
            {
                try
                {
                    AutoCount.ARAP.ARPayment.ARPaymentDataAccess cmd =
                        AutoCount.ARAP.ARPayment.ARPaymentDataAccess.Create(userSession, userSession.DBSetting);
                    AutoCount.ARAP.ARPayment.ARPaymentEntity doc = cmd.NewARPayment();

                    SetHeader(doc, data);
                    AddDetails(doc, data);
                    AddKnockOff(doc, data);

                    cmd.SaveARPayment(doc, userSession.LoginUserID, false);
                    ApplyProjectToGL(doc.DocNo, data);
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

                AutoCount.ARAP.ARPayment.ARPaymentDataAccess cmd =
                    AutoCount.ARAP.ARPayment.ARPaymentDataAccess.Create(userSession, userSession.DBSetting);
                AutoCount.ARAP.ARPayment.ARPaymentEntity doc = cmd.GetARPayment(docNo);
                if (doc == null)
                    throw new Exception($"{DoctypeName} {docNo} not found");

                SetHeader(doc, data);
                if (data.payments != null)
                {
                    doc.ClearDetails();
                    AddDetails(doc, data);
                }
                if (data.knockOff != null)
                {
                    doc.ClearKnockOff();
                    AddKnockOff(doc, data);
                }

                cmd.SaveARPayment(doc, userSession.LoginUserID, false);
                ApplyProjectToGL(docNo, data);
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
                AutoCount.ARAP.ARPayment.ARPaymentDataAccess cmd =
                    AutoCount.ARAP.ARPayment.ARPaymentDataAccess.Create(userSession, userSession.DBSetting);

                cmd.DeleteARPayment(docNo, userSession.LoginUserID);
                Log($"{DoctypeName} deleted: {docNo}");

                return Response.AsJson(new { success = true, docNo = docNo, message = $"{DoctypeName} deleted: {docNo}" });
            }
            Log($"{DoctypeName} delete error: Login failed");
            return Response.AsJson(new { success = false, message = $"{DoctypeName} delete error: Login failed" });
        }
    }
}
