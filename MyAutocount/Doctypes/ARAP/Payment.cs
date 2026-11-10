using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using static GCR_autocount_api.Utils;

namespace GCR_autocount_api.Doctypes.ARAP
{
    /// <summary>
    /// Supplier payment vouchers. READ-ONLY: the AutoCount NuGet SDK
    /// exposes no Payment document API, so posting must be done in AutoCount itself.
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
                "pi.DocNo AS DocNo, pi.DocDate AS DocDate, pi.NetTotal AS DocTotal, k.Amount AS KnockOffAmt " +
                "FROM [" + dbName + "].[dbo].[APPaymentKnockOff] k " +
                "INNER JOIN [" + dbName + "].[dbo].[vAPPayment] p ON p.DocKey = k.DocKey " +
                "LEFT JOIN [" + dbName + "].[dbo].[vPurchaseInvoice] pi ON pi.DocKey = k.KnockOffDocKey " +
                "WHERE p.DocNo = @docNo ORDER BY k.KnockOffDocType, pi.DocNo";
            return Sql.RunSqlQuery(userSession, query, new object[] { new SqlParameter("docNo", (string)docNo) });
        }
    }
}
