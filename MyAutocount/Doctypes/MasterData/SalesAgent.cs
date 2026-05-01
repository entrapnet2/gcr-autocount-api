using System;
using Nancy;
using Nancy.Extensions;
using Newtonsoft.Json;
using static GCR_autocount_api.Utils;

namespace GCR_autocount_api.Doctypes.MasterData
{
        public class SalesAgent : AuthenticatedModule
    {
        const string DoctypeName = "SalesAgent";
        const string DatabaseTable = "vSalesAgent";

        AutoCount.Data.DBSetting dbSetting;
        AutoCount.Authentication.UserSession userSession;

        public SalesAgent()
        {
            dbSetting = Auth.dbSetting;
            userSession = Auth.userSession;
            Run();
        }

        public static void Test()
        {
            SalesAgent agent = new SalesAgent();
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

            Get($"/{DoctypeName}/getSingle/{{agentCode}}", args =>
            {
                try
                {
                    return GetSingle(args.agentCode);
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

            Delete($"/{DoctypeName}/delete/{{agentCode}}", args =>
            {
                try
                {
                    return Delete(args.agentCode);
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

        private string GetSingle(string agentCode)
        {
            return Sql.GetSingleFromSql(userSession, DatabaseTable, "SalesAgent", agentCode);
        }

        private string Add(dynamic data)
        {
            if (Auth.Login(userSession))
            {
                try
                {
                    string agentName = data.agentName;
                    string agentCode = data.agentCode;

                    AutoCount.GeneralMaint.SalesAgent.SalesAgentCommand cmd =
                        AutoCount.GeneralMaint.SalesAgent.SalesAgentCommand.Create(userSession, userSession.DBSetting);

                    AutoCount.GeneralMaint.SalesAgent.SalesAgentEntity entity = cmd.NewSalesAgent();

                    entity.SalesAgent = agentCode;
                    entity.Description = agentName;

                    entity.Save();
                    Log($"{DoctypeName} added: {agentName}");
                    PublishEvent("master.salesagent", "created", agentCode, new { agentCode, agentName });
                    return $"{DoctypeName} added: {agentName}";
                }
                catch (Exception ex)
                {
                    Log($"{DoctypeName} add error: {ex.Message}");
                    Log($"Stack trace: {ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        Log($"Inner exception: {ex.InnerException.Message}");
                    }
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
                string agentCode = data[SalesAgentConstants.AgentCode].ToString();

                AutoCount.GeneralMaint.SalesAgent.SalesAgentCommand cmd =
                    AutoCount.GeneralMaint.SalesAgent.SalesAgentCommand.Create(userSession, userSession.DBSetting);

                AutoCount.GeneralMaint.SalesAgent.SalesAgentEntity entity = cmd.GetSalesAgent(agentCode);

entity.Description = data[SalesAgentConstants.AgentName];

                entity.Save();
                Log($"{DoctypeName} edited: {agentCode}");
                PublishEvent("master.salesagent", "updated", agentCode, new { agentCode, agentName = entity.Description });
                return $"{DoctypeName} edited: {agentCode}";
            }
            Log($"{DoctypeName} edit error: Login failed");
            return $"{DoctypeName} edit error: Login failed";
        }

        private string Delete(string agentCode)
        {
            if (Auth.Login(userSession))
            {
                AutoCount.GeneralMaint.SalesAgent.SalesAgentCommand cmd =
                    AutoCount.GeneralMaint.SalesAgent.SalesAgentCommand.Create(userSession, userSession.DBSetting);

                cmd.DeleteSalesAgent(agentCode);
                Log($"{DoctypeName} deleted: {agentCode}");
                PublishEvent("master.salesagent", "deleted", agentCode);
                return $"{DoctypeName} deleted: {agentCode}";
            }
            return $"{DoctypeName} delete error: Login failed";
        }

        private Response CreateErrorResponse(string message)
        {
            return Utils.CreateErrorResponse(message);
        }
    }

    internal static class SalesAgentConstants
    {
        internal static string AgentCode { get; } = "agentCode";
        internal static string AgentName { get; } = "agentName";
    }
}
