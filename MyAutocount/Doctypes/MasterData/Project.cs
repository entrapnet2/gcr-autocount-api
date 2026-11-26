using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using static GCR_autocount_api.Utils;

namespace GCR_autocount_api.Doctypes.MasterData
{
    public class Project : AuthenticatedModule
    {
        const string DoctypeName = "Project";
        const string DatabaseTable = "Project";
        const string PrimaryKey = "ProjNo";

        AutoCount.Data.DBSetting dbSetting;
        AutoCount.Authentication.UserSession userSession;

        public Project()
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

            Get($"/{DoctypeName}/getSingle/{{code}}", args =>
            {
                try
                {
                    return Sql.GetSingleFromSql(userSession, DatabaseTable, PrimaryKey, args.code);
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
