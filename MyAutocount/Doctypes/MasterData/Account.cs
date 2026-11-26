using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using static GCR_autocount_api.Utils;

namespace GCR_autocount_api.Doctypes.MasterData
{
    /// <summary>
    /// Chart of accounts (GLMast). Read-only.
    /// AccType codes: CA/FA current-fixed assets, CL/LL liabilities, CP capital,
    /// RE retained earnings, SL/SA/OI income, CO cost of sales, EP expenses, AP/TX payables-tax.
    /// </summary>
    public class Account : AuthenticatedModule
    {
        const string DoctypeName = "Account";
        const string DatabaseTable = "GLMast";
        const string PrimaryKey = "AccNo";

        AutoCount.Data.DBSetting dbSetting;
        AutoCount.Authentication.UserSession userSession;

        public Account()
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

            Get($"/{DoctypeName}/getSingle/{{accNo}}", args =>
            {
                try
                {
                    return Sql.GetSingleFromSql(userSession, DatabaseTable, PrimaryKey, args.accNo);
                }
                catch (Exception ex)
                {
                    Log(ex.ToString());
                    return Utils.ToErrorResponse(ex);
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
    }
}
