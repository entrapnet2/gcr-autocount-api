using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using static GCR_autocount_api.Utils;

namespace GCR_autocount_api.Doctypes.GL
{
    /// <summary>
    /// READ-ONLY ledger movement aggregated straight from GL postings (GLDTL),
    /// so every source is covered: invoices, debit/credit notes, Cash Book,
    /// journals, opening balances. Amounts are home-currency (HomeDR/HomeCR).
    /// GET /GL/getMovement?fromDate=01-01-2025&toDate=31-12-2025&groupBy=month,project,dept
    /// </summary>
    public class GL : AuthenticatedModule
    {
        const string DoctypeName = "GL";

        AutoCount.Data.DBSetting dbSetting;
        AutoCount.Authentication.UserSession userSession;

        public GL()
        {
            dbSetting = Auth.dbSetting;
            userSession = Auth.userSession;
            Run();
        }

        private void Run()
        {
            Get($"/{DoctypeName}/getMovement", _ =>
            {
                try
                {
                    return GetMovement(this.Request);
                }
                catch (Exception ex)
                {
                    Log(ex.ToString());
                    return Utils.ToErrorResponse(ex);
                }
            });
        }

        private string GetMovement(Request request)
        {
            string dbName = userSession.DBSetting.DBName;

            DateTime from = new DateTime(1900, 1, 1);
            DateTime to = DateTime.Today;
            if (request.Query.fromDate != null && !string.IsNullOrEmpty((string)request.Query.fromDate))
            {
                try { from = DateStringToDateTime((string)request.Query.fromDate); }
                catch { throw new Exception("Invalid fromDate, expected dd-MM-yyyy"); }
            }
            if (request.Query.toDate != null && !string.IsNullOrEmpty((string)request.Query.toDate))
            {
                try { to = DateStringToDateTime((string)request.Query.toDate); }
                catch { throw new Exception("Invalid toDate, expected dd-MM-yyyy"); }
            }

            var dims = new List<string>();
            string groupBy = request.Query.groupBy != null ? ((string)request.Query.groupBy).ToLower() : "month";
            foreach (string part in groupBy.Split(','))
            {
                string p = part.Trim();
                if ((p == "month" || p == "project" || p == "dept") && !dims.Contains(p))
                    dims.Add(p);
            }
            if (dims.Count == 0) dims.Add("month");

            string monthSel = dims.Contains("month") ? "CONVERT(varchar(7), g.TransDate, 120) AS [Month], " : "'ALL' AS [Month], ";
            string projSel = dims.Contains("project") ? "g.ProjNo AS ProjNo, " : "'ALL' AS ProjNo, ";
            string deptSel = dims.Contains("dept") ? "g.DeptNo AS DeptNo, " : "'ALL' AS DeptNo, ";

            string monthGrp = dims.Contains("month") ? ", CONVERT(varchar(7), g.TransDate, 120)" : "";
            string projGrp = dims.Contains("project") ? ", g.ProjNo" : "";
            string deptGrp = dims.Contains("dept") ? ", g.DeptNo" : "";

            string query = "SELECT g.AccNo, MAX(m.Description) AS AccDesc, MAX(m.AccType) AS AccType, " +
                monthSel + projSel + deptSel +
                "SUM(g.HomeDR) AS Debit, SUM(g.HomeCR) AS Credit " +
                "FROM [" + dbName + "].[dbo].[GLDTL] g " +
                "LEFT JOIN [" + dbName + "].[dbo].[GLMast] m ON m.AccNo = g.AccNo " +
                "WHERE g.TransDate >= @from AND g.TransDate < DATEADD(day, 1, @to) " +
                "GROUP BY g.AccNo" + monthGrp + projGrp + deptGrp + " " +
                "ORDER BY g.AccNo" + monthGrp + projGrp + deptGrp;

            return Sql.RunSqlQuery(userSession, query, new object[]
            {
                new SqlParameter("from", from),
                new SqlParameter("to", to)
            });
        }
    }
}
