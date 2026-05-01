using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;

namespace GCR_autocount_api
{
        public class Database : AuthenticatedModule
    {
        AutoCount.Data.DBSetting dbSetting;
        AutoCount.Authentication.UserSession userSession;
        public Database()
        {
            dbSetting = Auth.dbSetting;
            userSession = Auth.userSession;
            Run();

        }

        public static void Test()
        {
            Database dtb = new Database();
            Utils.print(dtb.GetTId());

        }

        private void Run()
        {
            Get($"/getTId", _ => {
                try
                {
                    return GetTId(this.Request);
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }

            });

        }

        private string GetTId(Request request = null)
        {
            string query = "SELECT TOP(1) [Transaction ID] FROM sys.fn_dblog(null,null) ORDER BY [Begin Time] DESC;";
            return Sql.RunSqlQuery(userSession, query, new object[] { });
        }

        public string GetTId()
        {
            string tid = Sql.RunSqlQuery(userSession, "SELECT TOP(1) [Transaction ID] FROM sys.fn_dblog(null,null) ORDER BY [Begin Time] DESC;", 
                new object[] { });
            return tid;
        }

    }
}
