using System;
using Nancy;
using Nancy.Extensions;
using Newtonsoft.Json;
using static GCR_autocount_api.Utils;

namespace GCR_autocount_api.Doctypes.MasterData
{
        public class StockLocation : AuthenticatedModule
    {
        const string DoctypeName = "StockLocation";
        const string DatabaseTable = "Location";
        const string PrimaryKey = "Location";

        AutoCount.Data.DBSetting dbSetting;
        AutoCount.Authentication.UserSession userSession;

        public StockLocation()
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
                    return GetAll(this.Request);
                }
                catch (Exception ex)
                {
                    Log(ex.ToString());
                    Response response = ex.Message;
                    response.StatusCode = HttpStatusCode.InternalServerError;
                    return response;
                }
            });

            Get($"/{DoctypeName}/getSingle/{{locationCode}}", args =>
            {
                try
                {
                    return GetSingle(args.locationCode);
                }
                catch (Exception ex)
                {
                    Log(ex.ToString());
                    Response response = ex.Message;
                    response.StatusCode = HttpStatusCode.InternalServerError;
                    return response;
                }
            });

            Post($"/{DoctypeName}/add", _ =>
            {
                try
                {
                    dynamic jsonData = Utils.ParseRequest(this.Request);
                    return Add(jsonData);
                }
                catch (Exception ex) { Log(ex.ToString()); return CreateErrorResponse(ex.Message); }
            });

            Put($"/{DoctypeName}/edit", _ =>
            {
                try
                {
                    dynamic jsonData = Utils.ParseRequest(this.Request);
                    return Edit(jsonData);
                }
                catch (Exception ex) { Log(ex.ToString()); return CreateErrorResponse(ex.Message); }
            });

            Delete($"/{DoctypeName}/delete/{{locationCode}}", args =>
            {
                try
                {
                    return Delete(args.locationCode);
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

        private string GetAll(Request request = null)
        {
            return Sql.GetAllFromSql(userSession, DatabaseTable, request);
        }

        private string GetSingle(string locationCode)
        {
            return Sql.GetSingleFromSql(userSession, DatabaseTable, PrimaryKey, locationCode);
        }

        private string Add(dynamic data)
        {
            if (Auth.Login(userSession))
            {
                try
                {
                    string locationCode = data[StockLocationConstants.LocationCode];

                    AutoCount.Stock.Location.LocationMaintenance cmd =
                        AutoCount.Stock.Location.LocationMaintenance.CreateLocationMaint(userSession, userSession.DBSetting);

                    AutoCount.Stock.Location.LocationEntity entity = cmd.NewLocation();

                    entity.Location = locationCode;
                    entity.Description = data[StockLocationConstants.Description];

                    if (data[StockLocationConstants.Description2] != null)
                        entity.Desc2 = data[StockLocationConstants.Description2].ToString();

                    if (data[StockLocationConstants.Address1] != null)
                        entity.Address1 = data[StockLocationConstants.Address1].ToString();

                    if (data[StockLocationConstants.Address2] != null)
                        entity.Address2 = data[StockLocationConstants.Address2].ToString();

                    if (data[StockLocationConstants.Address3] != null)
                        entity.Address3 = data[StockLocationConstants.Address3].ToString();

                    if (data[StockLocationConstants.Address4] != null)
                        entity.Address4 = data[StockLocationConstants.Address4].ToString();

                    if (data[StockLocationConstants.Phone] != null)
                        entity.Phone1 = data[StockLocationConstants.Phone].ToString();

                    if (data[StockLocationConstants.Fax] != null)
                        entity.Fax1 = data[StockLocationConstants.Fax].ToString();

                    if (data[StockLocationConstants.Contact] != null)
                        entity.Contact = data[StockLocationConstants.Contact].ToString();

                    cmd.SaveLocation(entity);
                    Log($"{DoctypeName} added: {locationCode}");

                    return $"{DoctypeName} added: {locationCode}";
                }
                catch (Exception ex)
                {
                    Log($"{DoctypeName} add error: {ex.Message}");
                    if (ex.InnerException != null)
                        Log($"Inner exception: {ex.InnerException.Message}");
                    throw;
                }
            }
            Log($"{DoctypeName} add error: Login failed");
            return $"{DoctypeName} add error: Login failed";
        }

        private string Edit(dynamic data)
        {
            if (Auth.Login(userSession))
            {
                string locationCode = data[StockLocationConstants.LocationCode];

                AutoCount.Stock.Location.LocationMaintenance cmd =
                    AutoCount.Stock.Location.LocationMaintenance.CreateLocationMaint(userSession, userSession.DBSetting);

                AutoCount.Stock.Location.LocationEntity entity = cmd.GetLocation(locationCode);

                if (data[StockLocationConstants.Description] != null)
                    entity.Description = data[StockLocationConstants.Description].ToString();

                if (data[StockLocationConstants.Description2] != null)
                    entity.Desc2 = data[StockLocationConstants.Description2].ToString();

                if (data[StockLocationConstants.Address1] != null)
                    entity.Address1 = data[StockLocationConstants.Address1].ToString();

                if (data[StockLocationConstants.Address2] != null)
                    entity.Address2 = data[StockLocationConstants.Address2].ToString();

                if (data[StockLocationConstants.Address3] != null)
                    entity.Address3 = data[StockLocationConstants.Address3].ToString();

                if (data[StockLocationConstants.Address4] != null)
                    entity.Address4 = data[StockLocationConstants.Address4].ToString();

                if (data[StockLocationConstants.Phone] != null)
                    entity.Phone1 = data[StockLocationConstants.Phone].ToString();

                if (data[StockLocationConstants.Fax] != null)
                    entity.Fax1 = data[StockLocationConstants.Fax].ToString();

                if (data[StockLocationConstants.Contact] != null)
                    entity.Contact = data[StockLocationConstants.Contact].ToString();

                cmd.SaveLocation(entity);
                Log($"{DoctypeName} edited: {locationCode}");

                return $"{DoctypeName} edited: {locationCode}";
            }
            Log($"{DoctypeName} edit error: Login failed");
            return $"{DoctypeName} edit error: Login failed";
        }

        private string Delete(string locationCode)
        {
            if (Auth.Login(userSession))
            {
                AutoCount.Stock.Location.LocationMaintenance cmd =
                    AutoCount.Stock.Location.LocationMaintenance.CreateLocationMaint(userSession, userSession.DBSetting);

                cmd.DeleteLocation(locationCode);
                Log($"{DoctypeName} deleted: {locationCode}");

                return $"{DoctypeName} deleted: {locationCode}";
            }
            return $"{DoctypeName} delete error: Login failed";
        }

        private Response CreateErrorResponse(string message)
        {
            return Utils.CreateErrorResponse(message);
        }
    }

    internal static class StockLocationConstants
    {
        internal static string LocationCode { get; } = "locationCode";
        internal static string Description { get; } = "description";
        internal static string Description2 { get; } = "description2";
        internal static string Address1 { get; } = "address1";
        internal static string Address2 { get; } = "address2";
        internal static string Address3 { get; } = "address3";
        internal static string Address4 { get; } = "address4";
        internal static string Phone { get; } = "phone";
        internal static string Fax { get; } = "fax";
        internal static string Contact { get; } = "contact";
    }
}
