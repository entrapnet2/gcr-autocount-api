using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;

namespace GCR_autocount_api
{
    class Sql
    {
        public static string RunSqlQuery(AutoCount.Authentication.UserSession userSession, string query, object[] paramsArray)
        {
            DataTable table = userSession.DBSetting.GetDataTable(query, false, paramsArray);
            return Utils.DataTableToJsonString(table);
        }

        public static string GetAllFromSql(AutoCount.Authentication.UserSession userSession, string tableName, Request request = null, string customFrom = null, string customSelect = null)
        {
            string dbName = userSession.DBSetting.DBName;
            string selectClause = string.IsNullOrEmpty(customSelect) ? "*" : customSelect;
            string fromSource = string.IsNullOrEmpty(customFrom) ? "[" + dbName + "].[dbo].[" + tableName + "]" : customFrom;

            if (request != null && ODataHelper.HasODataParams(request))
            {
                string odataQuery = ODataHelper.BuildQuery("SELECT " + selectClause + " FROM " + fromSource, request, tableName, dbName, customFrom);
                DataTable table1 = userSession.DBSetting.GetDataTable(odataQuery, false);
                return Utils.DataTableToJsonString(table1);
            }

            // Default limit to prevent returning too many records
            string query = "SELECT TOP(5) " + selectClause + " FROM " + fromSource;
            DataTable table2 = userSession.DBSetting.GetDataTable(query, false);
            return Utils.DataTableToJsonString(table2);
        }

        public static string GetSingleFromSql(AutoCount.Authentication.UserSession userSession, string tableName, string keyName, string key, string customFrom = null, string customSelect = null)
        {
            string dbName = userSession.DBSetting.DBName;
            string selectClause = string.IsNullOrEmpty(customSelect) ? "*" : customSelect;
            string fromSource = string.IsNullOrEmpty(customFrom) ? "[" + dbName + "].[dbo].[" + tableName + "]" : customFrom;
            string query = "SELECT " + selectClause + " FROM " + fromSource + " WHERE " + keyName + " = @" + keyName;
            DataTable result = userSession.DBSetting.GetDataTable(query, false, new object[] {
                new SqlParameter(keyName, key),
            });
            return Utils.DataTableToJsonString(result);
        }

        public static string GetSingleDetailFromSql(AutoCount.Authentication.UserSession userSession, string tableName, string detailTable, string docNo)
        {
            string dbName = userSession.DBSetting.DBName;
            string query = "SELECT * FROM [" + dbName + "].[dbo].[" + detailTable + "] " +
                "WHERE DocKey IN (SELECT DocKey FROM [" + dbName + "].[dbo].[" + tableName + "] " +
                "WHERE DocNo = @docNo)";
            object[] paramsList = new object[]
            {
                new SqlParameter("docNo", docNo)
            };
            return RunSqlQuery(userSession, query, paramsList);
        }

        public static string TestSql(AutoCount.Authentication.UserSession userSession, string query)
        {
            DataTable table = userSession.DBSetting.GetDataTable(query, false, new object[] { });
            return Utils.DataTableToJsonString(table);
        }

        public static string GetCountFromSql(AutoCount.Authentication.UserSession userSession, string tableName, Request request = null, string customFrom = null)
        {
            string dbName = userSession.DBSetting.DBName;
            string countQuery = ODataHelper.BuildCountQuery(request, tableName, dbName, customFrom);
            DataTable table = userSession.DBSetting.GetDataTable(countQuery, false);
            long count = table.Rows.Count > 0 ? Convert.ToInt64(table.Rows[0][0]) : 0;
            return Newtonsoft.Json.JsonConvert.SerializeObject(new { count });
        }

        public static void ExecuteSql(AutoCount.Authentication.UserSession userSession, string sql)
        {
            using (var conn = new System.Data.SqlClient.SqlConnection(userSession.DBSetting.ConnectionString))
            {
                conn.Open();
                using (var cmd = new System.Data.SqlClient.SqlCommand(sql, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
