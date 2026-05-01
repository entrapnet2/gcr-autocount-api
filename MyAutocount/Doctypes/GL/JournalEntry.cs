using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using Nancy.Extensions;
using Newtonsoft.Json;
using static GCR_autocount_api.Utils;

namespace GCR_autocount_api.Doctypes.GL
{
    public class JournalEntry : AuthenticatedModule
    {
        const string DoctypeName = "JournalEntry";
        const string PrimaryKey = "DocNo";

        const string DatabaseTable = "vJournalEntry";
        const string DetailTable = "vJournalEntryDetail";

        AutoCount.Data.DBSetting dbSetting;
        AutoCount.Authentication.UserSession userSession;

        public JournalEntry()
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
                    Response response = Sql.GetAllFromSql(userSession, DatabaseTable);
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

            Get($"/{DoctypeName}/getSingle/{{docNo}}", args =>
            {
                try
                {
                    Response response = Sql.GetSingleFromSql(userSession, DatabaseTable, PrimaryKey, args.docNo);
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

            Get($"/{DoctypeName}/getDetail/{{docNo}}", args => {
                try
                {
                    Response response = Sql.GetSingleDetailFromSql(userSession, DatabaseTable, DetailTable, args.docNo);
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

            Post($"/{DoctypeName}/add", args =>
            {
                try
                {
                    dynamic jsonData = Utils.ParseRequest(this.Request);
                    Response response = Add(jsonData);
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

            Put($"/{DoctypeName}/edit", args =>
            {
                try
                {
                    dynamic jsonData = Utils.ParseRequest(this.Request);
                    Response response = Edit(jsonData);
                    response.StatusCode = HttpStatusCode.OK;
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

            Delete($"/{DoctypeName}/delete/{{docNo}}", args => {
                try
                {
                    Response response = Delete(args.docNo);
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

        private string Add(dynamic data)
        {
            if (Auth.Login(userSession))
            {
                string docNo = data[JournalEntryConstants.DocNo];

                AutoCount.GL.JournalEntry.JournalEntryCommand cmd =
                    AutoCount.GL.JournalEntry.JournalEntryCommand.Create(userSession, userSession.DBSetting);
                AutoCount.GL.JournalEntry.JournalEntry doc = cmd.AddNew();

                doc.DocNo = docNo;
                doc.DocDate = DateStringToDateTime(data[JournalEntryConstants.DocDate].ToString());
                if (data.Contains(JournalEntryConstants.Description) && data[JournalEntryConstants.Description] != null) doc.Description = data[JournalEntryConstants.Description];

                dynamic detailList = data[JournalEntryConstants.DetailList];

                foreach (dynamic detailObject in detailList)
                {
                    AutoCount.GL.JournalEntry.JournalEntryDetail detail = doc.AddDetail();
                    detail.AccNo = detailObject[JournalEntryConstants.AccNo].ToString();
                    if (detailObject.Contains(JournalEntryConstants.Description) && detailObject[JournalEntryConstants.Description] != null) detail.Description = detailObject[JournalEntryConstants.Description].ToString();
                    detail.DR = decimal.Parse(detailObject[JournalEntryConstants.Debit].ToString());
                    detail.CR = decimal.Parse(detailObject[JournalEntryConstants.Credit].ToString());
                }

                doc.Save();
                Log($"{DoctypeName} added: {docNo}");
                return $"{DoctypeName} added: {docNo}";
            }
            Log($"{DoctypeName} add error: Login failed");
            return $"{DoctypeName} add error: Login failed";
        }

        private string Edit(dynamic data)
        {
            if (Auth.Login(userSession))
            {
                string docNo = data[JournalEntryConstants.DocNo];

                AutoCount.GL.JournalEntry.JournalEntryCommand cmd =
                    AutoCount.GL.JournalEntry.JournalEntryCommand.Create(userSession, userSession.DBSetting);
                AutoCount.GL.JournalEntry.JournalEntry doc = cmd.Edit(docNo);

                doc.DocDate = DateStringToDateTime(data[JournalEntryConstants.DocDate].ToString());
                if (data.Contains(JournalEntryConstants.Description) && data[JournalEntryConstants.Description] != null) doc.Description = data[JournalEntryConstants.Description];

                dynamic detailList = data[JournalEntryConstants.DetailList];

                doc.ClearDetails();

                foreach (dynamic detailObject in detailList)
                {
                    AutoCount.GL.JournalEntry.JournalEntryDetail detail = doc.AddDetail();
                    detail.AccNo = detailObject[JournalEntryConstants.AccNo].ToString();
                    if (detailObject.Contains(JournalEntryConstants.Description) && detailObject[JournalEntryConstants.Description] != null) detail.Description = detailObject[JournalEntryConstants.Description].ToString();
                    detail.DR = decimal.Parse(detailObject[JournalEntryConstants.Debit].ToString());
                    detail.CR = decimal.Parse(detailObject[JournalEntryConstants.Credit].ToString());
                }

                doc.Save();
                Log($"{DoctypeName} edited: {docNo}");
                return $"{DoctypeName} edited: {docNo}";
            }
            Log($"{DoctypeName} edit error: Login failed");
            return $"{DoctypeName} edit error: Login failed";
        }

        private string Delete(string docNo)
        {
            if (Auth.Login(userSession))
            {
                AutoCount.GL.JournalEntry.JournalEntryCommand cmd =
                    AutoCount.GL.JournalEntry.JournalEntryCommand.Create(userSession, userSession.DBSetting);

                cmd.Delete(docNo);
                Log($"{DoctypeName} deleted: {docNo}");
                return $"{DoctypeName} deleted: {docNo}";
            }
            Log($"{DoctypeName} delete error: Login failed");
            return $"{DoctypeName} delete error: Login failed";
        }
    }

    internal static class JournalEntryConstants
    {
        internal static string DocNo { get; } = "docNo";
        internal static string DocDate { get; } = "docDate";
        internal static string Description { get; } = "description";
        internal static string DetailList { get; } = "detailList";
        internal static string AccNo { get; } = "accNo";
        internal static string Debit { get; } = "debit";
        internal static string Credit { get; } = "credit";
    }
}
