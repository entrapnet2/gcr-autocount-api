using System;
using System.Collections.Generic;
using System.Text;
using Nancy;

namespace GCR_autocount_api
{
    public class ODataHelper
    {
        private const int DefaultRows = 5;
        private const int MaxRows = 1000;

        public static string BuildQuery(string baseQuery, Nancy.Request request, string tableName, string dbName)
        {
            var queryParams = request.Query;

            // Determine $top value - default to 5 if not specified
            int topValue = DefaultRows;
            if (queryParams.ContainsKey("$top"))
            {
                string top = queryParams["$top"];
                if (!string.IsNullOrEmpty(top) && int.TryParse(top, out int requestedTop))
                {
                    topValue = Math.Min(requestedTop, MaxRows);
                }
            }

            string selectClause = "*";
            // $select
            if (queryParams.ContainsKey("$select"))
            {
                string selectFields = queryParams["$select"];
                if (!string.IsNullOrEmpty(selectFields))
                {
                    string[] fields = selectFields.Split(',');
                    selectClause = string.Join(", ", Array.ConvertAll(fields, f => "[" + f.Trim() + "]"));
                }
            }

            var queryBuilder = new StringBuilder("SELECT TOP " + topValue + " " + selectClause + " FROM [" + dbName + "].[dbo].[" + tableName + "]");

            // $filter
            if (queryParams.ContainsKey("$filter"))
            {
                string filter = queryParams["$filter"];
                if (!string.IsNullOrEmpty(filter))
                {
                    string whereClause = ParseFilter(filter);
                    queryBuilder.Append(" WHERE " + whereClause);
                }
            }

            // $orderby
            if (queryParams.ContainsKey("$orderby"))
            {
                string orderBy = queryParams["$orderby"];
                if (!string.IsNullOrEmpty(orderBy))
                {
                    string[] orderParts = orderBy.Split(' ');
                    string field = orderParts[0];
                    string direction = (orderParts.Length > 1 && orderParts[1].ToLower() == "desc") ? "DESC" : "ASC";
                    queryBuilder.Append(" ORDER BY [" + field + "] " + direction);
                }
            }

            return queryBuilder.ToString();
        }

        private static string ParseFilter(string filter)
        {
            // Simple filter parser for common operations
            // Supports: eq, ne, gt, lt, ge, le, and, or
            // Example: $filter=DocNo eq 'SO-0001' and DocDate gt '2024-01-01'

            filter = filter.Replace(" eq ", " = ").Replace(" ne ", " <> ");
            filter = filter.Replace(" gt ", " > ").Replace(" lt ", " < ");
            filter = filter.Replace(" ge ", " >= ").Replace(" le ", " <= ");
            filter = filter.Replace(" and ", " AND ").Replace(" or ", " OR ");

            // Handle string values (single quotes)
            // This is a simplified version - production would need more robust parsing
            return filter;
        }

        public static bool HasODataParams(Nancy.Request request)
        {
            var queryParams = request.Query;
            return queryParams.ContainsKey("$select") ||
                   queryParams.ContainsKey("$filter") ||
                   queryParams.ContainsKey("$orderby") ||
                   queryParams.ContainsKey("$top") ||
                   queryParams.ContainsKey("$skip");
        }
    }
}
