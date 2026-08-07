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

        public static string BuildQuery(string baseQuery, Nancy.Request request, string tableName, string dbName, string customFrom = null)
        {
            var queryParams = request.Query;

            int topValue = DefaultRows;
            if (queryParams.ContainsKey("$top"))
            {
                string top = queryParams["$top"];
                if (!string.IsNullOrEmpty(top) && int.TryParse(top, out int requestedTop))
                {
                    topValue = Math.Min(requestedTop, MaxRows);
                }
            }

            int skipValue = 0;
            if (queryParams.ContainsKey("$skip"))
            {
                string skip = queryParams["$skip"];
                if (!string.IsNullOrEmpty(skip) && int.TryParse(skip, out int parsedSkip) && parsedSkip > 0)
                {
                    skipValue = parsedSkip;
                }
            }

            string selectClause = "*";
            if (queryParams.ContainsKey("$select"))
            {
                string selectFields = queryParams["$select"];
                if (!string.IsNullOrEmpty(selectFields))
                {
                    string[] fields = selectFields.Split(',');
                    selectClause = string.Join(", ", Array.ConvertAll(fields, f => "[" + f.Trim() + "]"));
                }
            }

            string orderByClause = null;
            if (queryParams.ContainsKey("$orderby"))
            {
                string orderBy = queryParams["$orderby"];
                if (!string.IsNullOrEmpty(orderBy))
                {
                    string[] orderParts = orderBy.Split(' ');
                    string field = orderParts[0];
                    string direction = (orderParts.Length > 1 && orderParts[1].ToLower() == "desc") ? "DESC" : "ASC";
                    orderByClause = "[" + field + "] " + direction;
                }
            }

            if (skipValue > 0 && orderByClause == null)
            {
                orderByClause = "(SELECT 0)";
            }

            string fromSource = string.IsNullOrEmpty(customFrom)
                ? "[" + dbName + "].[dbo].[" + tableName + "]"
                : customFrom;

            var queryBuilder = new StringBuilder("SELECT " + selectClause + " FROM " + fromSource);

            if (queryParams.ContainsKey("$filter"))
            {
                string filter = queryParams["$filter"];
                if (!string.IsNullOrEmpty(filter))
                {
                    string whereClause = ParseFilter(filter);
                    queryBuilder.Append(" WHERE " + whereClause);
                }
            }

            if (skipValue > 0)
            {
                queryBuilder.Append(" ORDER BY " + orderByClause + " OFFSET " + skipValue + " ROWS FETCH NEXT " + topValue + " ROWS ONLY");
            }
            else if (orderByClause != null)
            {
                queryBuilder.Append(" ORDER BY " + orderByClause);
                queryBuilder.Replace("SELECT " + selectClause, "SELECT TOP " + topValue + " " + selectClause);
            }
            else
            {
                queryBuilder.Replace("SELECT " + selectClause, "SELECT TOP " + topValue + " " + selectClause);
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

            int skipValue = 0;
            if (queryParams.ContainsKey("$skip"))
            {
                string skip = queryParams["$skip"];
                if (!string.IsNullOrEmpty(skip) && int.TryParse(skip, out int parsedSkip) && parsedSkip > 0)
                {
                    skipValue = parsedSkip;
                }
            }

            string orderByClause = null;
            if (queryParams.ContainsKey("$orderby"))
            {
                string orderBy = queryParams["$orderby"];
                if (!string.IsNullOrEmpty(orderBy))
                {
                    string[] orderParts = orderBy.Split(' ');
                    string field = orderParts[0];
                    string direction = (orderParts.Length > 1 && orderParts[1].ToLower() == "desc") ? "DESC" : "ASC";
                    orderByClause = "[" + field + "] " + direction;
                }
            }

            if (skipValue > 0 && orderByClause == null)
            {
                orderByClause = "(SELECT 0)";
            }

            string selectPrefix = skipValue > 0 ? "SELECT " : "SELECT TOP " + topValue + " ";

            if (string.IsNullOrEmpty(existingWhere))
            {
                queryBuilder.Append(baseQuery.Replace("SELECT ", selectPrefix));
            }
            else
            {
                int whereIndex = baseQuery.IndexOf(" WHERE ", StringComparison.OrdinalIgnoreCase);
                if (whereIndex > 0)
                {
                    string selectPart = baseQuery.Substring(0, whereIndex);
                    string wherePart = baseQuery.Substring(whereIndex + 7);
                    queryBuilder.Append(selectPart.Replace("SELECT ", selectPrefix));
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
                    queryBuilder.Append(baseQuery.Replace("SELECT ", selectPrefix));
                }
            }

            if (skipValue > 0)
            {
                queryBuilder.Append(" ORDER BY " + orderByClause + " OFFSET " + skipValue + " ROWS FETCH NEXT " + topValue + " ROWS ONLY");
            }
            else if (orderByClause != null)
            {
                queryBuilder.Append(" ORDER BY " + orderByClause);
            }

            return queryBuilder.ToString();
        }

        public static string ApplyODataToDataTable(DataTable table, Nancy.Request request)
        {
            if (table == null)
                return "[]";

            int topValue = DefaultRows;
            int skipValue = 0;
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

                if (!string.IsNullOrEmpty(request.Query["$skip"]))
                {
                    if (int.TryParse(request.Query["$skip"], out int parsedSkip) && parsedSkip > 0)
                    {
                        skipValue = parsedSkip;
                    }
                }
            }

            DataTable resultTable = dv.ToTable();
            DataTable limitedTable = resultTable.Clone();
            for (int i = skipValue; i < skipValue + topValue && i < resultTable.Rows.Count; i++)
            {
                limitedTable.ImportRow(resultTable.Rows[i]);
            }
            return Utils.DataTableToJsonString(limitedTable);
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

        public static string BuildCountQuery(Nancy.Request request, string tableName, string dbName, string customFrom = null)
        {
            string fromSource = string.IsNullOrEmpty(customFrom)
                ? "[" + dbName + "].[dbo].[" + tableName + "]"
                : customFrom;

            var queryBuilder = new StringBuilder("SELECT COUNT(*) FROM " + fromSource);

            if (request != null && request.Query != null && request.Query.ContainsKey("$filter"))
            {
                string filter = request.Query["$filter"];
                if (!string.IsNullOrEmpty(filter))
                {
                    string whereClause = ParseFilter(filter);
                    queryBuilder.Append(" WHERE " + whereClause);
                }
            }

            return queryBuilder.ToString();
        }
    }
}
