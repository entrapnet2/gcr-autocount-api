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
    /// Customer receipts (Official Receipts). READ-ONLY: the AutoCount NuGet SDK
    /// exposes no Receipt document API, so posting must be done in AutoCount itself.
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
        /// Invoices/documents this receipt was knocked off against.
        /// </summary>
        private string GetKnockOff(string docNo)
        {
            string dbName = userSession.DBSetting.DBName;
            string query = "SELECT k.KnockOffDocType AS DocType, k.KnockOffDocKey AS DocKey, " +
                "iv.DocNo AS DocNo, iv.DocDate AS DocDate, iv.NetTotal AS DocTotal, k.Amount AS KnockOffAmt " +
                "FROM [" + dbName + "].[dbo].[ARPaymentKnockOff] k " +
                "INNER JOIN [" + dbName + "].[dbo].[vARPayment] r ON r.DocKey = k.DocKey " +
                "LEFT JOIN [" + dbName + "].[dbo].[vInvoice] iv ON iv.DocKey = k.KnockOffDocKey " +
                "WHERE r.DocNo = @docNo ORDER BY k.KnockOffDocType, iv.DocNo";
            return Sql.RunSqlQuery(userSession, query, new object[] { new SqlParameter("docNo", (string)docNo) });
        }
    }
}
