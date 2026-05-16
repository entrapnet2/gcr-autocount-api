using System;
using System.Collections.Generic;
using System.Data;
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

        public static string BuildQueryWithFilter(string baseQuery, Nancy.Request request, string tableName, string dbName, string existingWhere = null)
        {
            var queryParams = request.Query;
            var queryBuilder = new StringBuilder();

            int topValue = DefaultRows;
            if (queryParams.ContainsKey("$top"))
            {
                string top = queryParams["$top"];
                if (!string.IsNullOrEmpty(top) && int.TryParse(top, out int requestedTop))
                {
                    topValue = Math.Min(requestedTop, MaxRows);
                }
            }

            if (string.IsNullOrEmpty(existingWhere))
            {
                queryBuilder.Append(baseQuery.Replace("SELECT ", "SELECT TOP " + topValue + " "));
            }
            else
            {
                int whereIndex = baseQuery.IndexOf(" WHERE ", StringComparison.OrdinalIgnoreCase);
                if (whereIndex > 0)
                {
                    string selectPart = baseQuery.Substring(0, whereIndex);
                    string wherePart = baseQuery.Substring(whereIndex + 7);
                    queryBuilder.Append(selectPart.Replace("SELECT ", "SELECT TOP " + topValue + " "));
                    queryBuilder.Append(" WHERE ");
                    queryBuilder.Append(wherePart);

                    if (queryParams.ContainsKey("$filter"))
                    {
                        string filter = queryParams["$filter"];
                        if (!string.IsNullOrEmpty(filter))
                        {
                            queryBuilder.Append(" AND " + ParseFilter(filter));
                        }
                    }
                }
                else
                {
                    queryBuilder.Append(baseQuery.Replace("SELECT ", "SELECT TOP " + topValue + " "));
                }
            }

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

        public static string ApplyODataToDataTable(DataTable table, Nancy.Request request)
        {
            if (table == null)
                return "[]";

            int topValue = DefaultRows;
            DataView dv = table.DefaultView;

            if (request != null && request.Query != null)
            {
                if (!string.IsNullOrEmpty(request.Query["$filter"]))
                {
                    string filter = request.Query["$filter"];
                    dv.RowFilter = ParseFilterForDataView(filter);
                }

                if (!string.IsNullOrEmpty(request.Query["$orderby"]))
                {
                    string orderBy = request.Query["$orderby"];
                    dv.Sort = orderBy.Replace(" eq ", " = ").Replace(" desc", " DESC").Replace(" asc", " ASC");
                }

                if (!string.IsNullOrEmpty(request.Query["$top"]))
                {
                    if (int.TryParse(request.Query["$top"], out int requestedTop))
                    {
                        topValue = Math.Min(requestedTop, MaxRows);
                    }
                }
            }

            DataTable resultTable = dv.ToTable();
            if (resultTable.Rows.Count > topValue)
            {
                DataTable limitedTable = resultTable.Clone();
                for (int i = 0; i < topValue && i < resultTable.Rows.Count; i++)
                {
                    limitedTable.ImportRow(resultTable.Rows[i]);
                }
                return Utils.DataTableToJsonString(limitedTable);
            }

            return Utils.DataTableToJsonString(resultTable);
        }

        private static string ParseFilterForDataView(string filter)
        {
            filter = filter.Replace(" eq ", " = ");
            filter = filter.Replace(" ne ", " <> ");
            filter = filter.Replace(" gt ", " > ");
            filter = filter.Replace(" lt ", " < ");
            filter = filter.Replace(" ge ", " >= ");
            filter = filter.Replace(" le ", " <= ");
            filter = filter.Replace(" and ", " AND ");
            filter = filter.Replace(" or ", " OR ");
            return filter;
        }
    }
}
