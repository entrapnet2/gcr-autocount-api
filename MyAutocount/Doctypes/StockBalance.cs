using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using Nancy.Extensions;
using Newtonsoft.Json;
using static GCR_autocount_api.Utils;

namespace GCR_autocount_api.Doctypes
{
    public class StockBalance : AuthenticatedModule
    {
        AutoCount.Data.DBSetting dbSetting;
        AutoCount.Authentication.UserSession userSession;

        public StockBalance()
        {
            dbSetting = Auth.dbSetting;
            userSession = Auth.userSession;
            Run();
        }

        private void Run()
        {
            Get("/StockBalance/getAll", _ =>
            {
                try
                {
                    Response response = GetAll(this.Request);
                    return response;
                }
                catch (Exception ex)
                {
                    Log(ex.ToString());
                    Response response = ex.Message;
                    response.StatusCode = HttpStatusCode.InternalServerError;
                    return response;
                }
            });

            Get("/StockBalance/getByItem/{itemCode}", args =>
            {
                try
                {
                    Response response = GetByItemCode(args.itemCode, this.Request);
                    return response;
                }
                catch (Exception ex)
                {
                    Log(ex.ToString());
                    Response response = ex.Message;
                    response.StatusCode = HttpStatusCode.InternalServerError;
                    return response;
                }
            });

            Get("/StockBalance/getOnDate", _ =>
            {
                try
                {
                    Response response = GetOnDate(this.Request);
                    return response;
                }
                catch (Exception ex)
                {
                    Log(ex.ToString());
                    Response response = ex.Message;
                    response.StatusCode = HttpStatusCode.InternalServerError;
                    return response;
                }
            });

            Get("/StockBalance/getByLocation", _ =>
            {
                try
                {
                    Response response = GetByLocation(this.Request);
                    return response;
                }
                catch (Exception ex)
                {
                    Log(ex.ToString());
                    Response response = ex.Message;
                    response.StatusCode = HttpStatusCode.InternalServerError;
                    return response;
                }
            });

            Get("/StockBalance/getSerialNumbers/{itemCode}", args =>
            {
                try
                {
                    Response response = GetSerialNumbers(args.itemCode, this.Request);
                    return response;
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

        private string GetAll(Request request)
        {
            DateTime onDate = DateTime.Today;

            AutoCount.Stock.StockBalance.StockBalanceHelper sbHelper = new AutoCount.Stock.StockBalance.StockBalanceHelper(userSession);
            sbHelper.Inquire(onDate);

            DataTable result = sbHelper.ResultTable;
            return ODataHelper.ApplyODataToDataTable(result, request);
        }

        private string GetByItemCode(string itemCode, Request request)
        {
            DateTime onDate = DateTime.Today;

            AutoCount.Stock.StockBalance.StockBalanceHelper sbHelper = new AutoCount.Stock.StockBalance.StockBalanceHelper(userSession);
            sbHelper.Inquire(onDate);

            DataTable result = sbHelper.ResultTable;
            
            DataView dv = result.DefaultView;
            dv.RowFilter = $"ItemCode = '{itemCode}'";
            DataTable filteredTable = dv.ToTable();
            
            return ODataHelper.ApplyODataToDataTable(filteredTable, request);
        }

        private string GetOnDate(Request request)
        {
            string onDateStr = request.Query["onDate"];
            string locationFrom = request.Query["locationFrom"];
            string locationTo = request.Query["locationTo"];

            if (string.IsNullOrEmpty(onDateStr))
            {
                onDateStr = DateTime.Today.ToString("yyyy-MM-dd");
            }

            DateTime onDate = DateTime.Parse(onDateStr);

            AutoCount.Stock.StockBalance.StockBalanceHelper sbHelper = new AutoCount.Stock.StockBalance.StockBalanceHelper(userSession);

            if (!string.IsNullOrEmpty(locationFrom) && !string.IsNullOrEmpty(locationTo))
            {
                sbHelper.Criteria.LocationFilter.Type = AutoCount.SearchFilter.FilterType.ByRange;
                sbHelper.Criteria.LocationFilter.From = locationFrom;
                sbHelper.Criteria.LocationFilter.To = locationTo;
            }

            sbHelper.Inquire(onDate);

            DataTable result = sbHelper.ResultTable;
            return ODataHelper.ApplyODataToDataTable(result, request);
        }

        private string GetByLocation(Request request)
        {
            string locationFrom = request.Query["locationFrom"];
            string locationTo = request.Query["locationTo"];
            string onDateStr = request.Query["onDate"];

            if (string.IsNullOrEmpty(onDateStr))
            {
                onDateStr = DateTime.Today.ToString("yyyy-MM-dd");
            }

            DateTime onDate = DateTime.Parse(onDateStr);

            AutoCount.Stock.StockBalance.StockBalanceHelper sbHelper = new AutoCount.Stock.StockBalance.StockBalanceHelper(userSession);

            if (!string.IsNullOrEmpty(locationFrom) && !string.IsNullOrEmpty(locationTo))
            {
                sbHelper.Criteria.LocationFilter.Type = AutoCount.SearchFilter.FilterType.ByRange;
                sbHelper.Criteria.LocationFilter.From = locationFrom;
                sbHelper.Criteria.LocationFilter.To = locationTo;
            }

            sbHelper.Inquire(onDate);

            DataTable result = sbHelper.ResultTable;
            return ODataHelper.ApplyODataToDataTable(result, request);
        }

        private string GetSerialNumbers(string itemCode, Request request)
        {
            DateTime onDate = DateTime.Today;

            AutoCount.Stock.StockBalance.StockBalanceHelper sbHelper = new AutoCount.Stock.StockBalance.StockBalanceHelper(userSession);
            sbHelper.Inquire(onDate);

            DataSet ds = sbHelper.ResultDataSet;
            DataTable serialTable = ds.Tables["ItemSerialNo"];

            if (serialTable != null)
            {
                DataView dv = serialTable.DefaultView;
                dv.RowFilter = $"ItemCode = '{itemCode}'";
                DataTable filteredTable = dv.ToTable();
                return ODataHelper.ApplyODataToDataTable(filteredTable, request);
            }

            return "[]";
        }
    }
}
