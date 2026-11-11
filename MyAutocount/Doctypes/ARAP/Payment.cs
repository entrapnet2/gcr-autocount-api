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
    /// Supplier payment vouchers with knock-off against bills/debit notes.
    /// API: AutoCount.ARAP.APPayment.APPaymentDataAccess (NuGet AutoCount2.Accounting).
    /// </summary>
    public class Payment : AuthenticatedModule
    {
        const string DoctypeName = "Payment";
        const string PrimaryKey = "DocNo";

        const string DatabaseTable = "vAPPayment";
        const string DetailTable = "vAPPaymentDetail";

        AutoCount.Data.DBSetting dbSetting;
        AutoCount.Authentication.UserSession userSession;

        public Payment()
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
                    Response response = Add(jsonData);
                    return response;
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
                    Response response = Edit(jsonData);
                    response.StatusCode = HttpStatusCode.OK;
                    return response;
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
                    Response response = Delete(args.docNo);
                    return response;
                }
                catch (Exception ex)
                {
                    Log(ex.ToString());
                    return Utils.ToErrorResponse(ex);
                }
            });

            // ?docNo= fallbacks for auto-generated numbers containing '/' ,
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

        /// <summary>
        /// Supplier bills this payment was knocked off against.
        /// </summary>
        private string GetKnockOff(string docNo)
        {
            string dbName = userSession.DBSetting.DBName;
            string query = "SELECT k.KnockOffDocType AS DocType, k.KnockOffDocKey AS DocKey, " +
                "COALESCE(pi.DocNo, dn.DocNo) AS DocNo, COALESCE(pi.DocDate, dn.DocDate) AS DocDate, k.Amount AS KnockOffAmt " +
                "FROM [" + dbName + "].[dbo].[APPaymentKnockOff] k " +
                "INNER JOIN [" + dbName + "].[dbo].[vAPPayment] p ON p.DocKey = k.DocKey " +
                "LEFT JOIN [" + dbName + "].[dbo].[APInvoice] pi ON pi.DocKey = k.KnockOffDocKey AND k.KnockOffDocType = 'PB' " +
                "LEFT JOIN [" + dbName + "].[dbo].[APDN] dn ON dn.DocKey = k.KnockOffDocKey AND k.KnockOffDocType = 'PD' " +
                "WHERE p.DocNo = @docNo ORDER BY k.KnockOffDocType, COALESCE(pi.DocNo, dn.DocNo)";
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

        private static string MapDocType(string docType)
        {
            if (string.IsNullOrEmpty(docType)) return AutoCount.Document.DocumentType.APInvoice;
            string t = docType.Trim().ToUpper();
            if (t == "DN" || t == "PD") return AutoCount.Document.DocumentType.APDN;
            if (t == "PB" || t == "PI" || t == "INVOICE" || t == "BILL") return AutoCount.Document.DocumentType.APInvoice;
            return t;
        }

        private static void SetHeader(AutoCount.ARAP.APPayment.APPaymentEntity doc, dynamic data)
        {
            if (data.creditorCode != null)
                doc.CreditorCode = data.creditorCode.ToString();
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
            if (data.project != null)
                doc.ProjNo = data.project.ToString();
            if (data.department != null)
                doc.DeptNo = data.department.ToString();
        }

        private static void AddDetails(AutoCount.ARAP.APPayment.APPaymentEntity doc, dynamic data)
        {
            if (data.payments == null) return;
            foreach (dynamic p in data.payments)
            {
                AutoCount.ARAP.APPayment.APPaymentDTLEntity dtl = doc.NewDetail();
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

        private static void AddKnockOff(AutoCount.ARAP.APPayment.APPaymentEntity doc, dynamic data)
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

        private string Add(dynamic data)
        {
            if (Auth.Login(userSession))
            {
                try
                {
                    AutoCount.ARAP.APPayment.APPaymentDataAccess cmd =
                        AutoCount.ARAP.APPayment.APPaymentDataAccess.Create(userSession, userSession.DBSetting);
                    AutoCount.ARAP.APPayment.APPaymentEntity doc = cmd.NewAPPayment();

                    SetHeader(doc, data);
                    AddDetails(doc, data);
                    AddKnockOff(doc, data);

                    cmd.SaveAPPayment(doc, userSession.LoginUserID, false);
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

                AutoCount.ARAP.APPayment.APPaymentDataAccess cmd =
                    AutoCount.ARAP.APPayment.APPaymentDataAccess.Create(userSession, userSession.DBSetting);
                AutoCount.ARAP.APPayment.APPaymentEntity doc = cmd.GetAPPayment(docNo);
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

                cmd.SaveAPPayment(doc, userSession.LoginUserID, false);
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
                AutoCount.ARAP.APPayment.APPaymentDataAccess cmd =
                    AutoCount.ARAP.APPayment.APPaymentDataAccess.Create(userSession, userSession.DBSetting);

                cmd.DeleteAPPayment(docNo, userSession.LoginUserID);
                Log($"{DoctypeName} deleted: {docNo}");

                return $"{DoctypeName} deleted: {docNo}";
            }
            Log($"{DoctypeName} delete error: Login failed");
            return $"{DoctypeName} delete error: Login failed";
        }
    }
}
