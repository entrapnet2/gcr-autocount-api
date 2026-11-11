using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using Nancy.Extensions;
using Nancy.Helpers;
using Newtonsoft.Json;
using static GCR_autocount_api.Utils;

namespace GCR_autocount_api
{
    public class LoginModule : NancyModule
    {
        public LoginModule()
        {
            Get("/companies", _ => GetCompanies());
            Get("/projects", _ => GetCompanies());
            Get("/databases", _ => GetCompanies());

            Post("/login", _ =>
            {
                try
                {
                    Utils.LogRequest(this.Request);
                    string username = this.Request.Form["username"];
                    string password = this.Request.Form["password"];
                    string project = this.Request.Form["project"];

                    // Fallback to query string just in case Swagger sends it there
                    if (string.IsNullOrEmpty(username)) username = this.Request.Query["username"];
                    if (string.IsNullOrEmpty(password)) password = this.Request.Query["password"];
                    if (string.IsNullOrEmpty(project)) project = this.Request.Query["project"];
                    if (string.IsNullOrEmpty(project)) project = this.Request.Query["databaseName"];
                    if (string.IsNullOrEmpty(project)) project = this.Request.Query["companyName"];
                    if (string.IsNullOrEmpty(project)) project = this.Request.Query["database"];

                    // Read body manually to be absolutely certain
                    string bodyString = null;
                    try
                    {
                        this.Request.Body.Position = 0;
                        bodyString = Nancy.IO.RequestStream.FromStream(this.Request.Body).AsString();
                    }
                    catch { }

                    if (!string.IsNullOrEmpty(bodyString))
                    {
                        // Try JSON
                        if (bodyString.Trim().StartsWith("{"))
                        {
                            try
                            {
                                dynamic jsonData = JsonConvert.DeserializeObject(bodyString);
                                if (string.IsNullOrEmpty(username)) username = jsonData.username?.ToString();
                                if (string.IsNullOrEmpty(password)) password = jsonData.password?.ToString();
                                if (string.IsNullOrEmpty(project))
                                {
                                    project = jsonData.project?.ToString()
                                        ?? jsonData.databaseName?.ToString()
                                        ?? jsonData.companyName?.ToString()
                                        ?? jsonData.database?.ToString()
                                        ?? jsonData.company?.ToString();
                                }
                            }
                            catch { }
                        }
                        // Try raw form URL encoded manually
                        else if (bodyString.Contains("username=") || bodyString.Contains("password=") || bodyString.Contains("project="))
                        {
                            try
                            {
                                var queryParams = HttpUtility.ParseQueryString(bodyString);
                                if (string.IsNullOrEmpty(username)) username = queryParams["username"];
                                if (string.IsNullOrEmpty(password)) password = queryParams["password"];
                                if (string.IsNullOrEmpty(project))
                                    project = queryParams["project"] ?? queryParams["databaseName"] ?? queryParams["companyName"] ?? queryParams["database"];
                            }
                            catch { }
                        }
                    }

                    if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                    {
                        var errorResponse = new
                        {
                            success = false,
                            message = "Username and password required. Try clearing your browser cache and refresh Swagger UI."
                        };
                        return Response.AsJson(errorResponse).WithStatusCode(HttpStatusCode.BadRequest);
                    }

                    // Switch project/database if requested
                    DatabaseInfo selectedProject = null;
                    if (!string.IsNullOrEmpty(project))
                    {
                        selectedProject = Auth.SwitchDatabase(project);
                        if (selectedProject == null)
                        {
                            var available = Auth.GetAvailableDatabases();
                            var response404 = new
                            {
                                success = false,
                                message = "Project '" + project + "' not found. Use GET /companies to list available projects.",
                                availableProjects = available.Select(d => new { d.companyName, d.databaseName, d.remark }).ToArray()
                            };
                            return Response.AsJson(response404).WithStatusCode(HttpStatusCode.BadRequest);
                        }
                    }

                    // Try to login to AutoCount
                    if (Auth.Login(Auth.userSession, username, password))
                    {
                        // Generate JWT token bound to the active project
                        string token = JwtHelper.GenerateToken(username, Auth.CurrentDatabaseName, Auth.CurrentCompanyName);

                        var response = new
                        {
                            success = true,
                            token = token,
                            databaseName = Auth.CurrentDatabaseName,
                            companyName = Auth.CurrentCompanyName,
                            serverName = Auth.CurrentServerName,
                            message = "Login successful"
                        };

                        Log("Login successful: " + username + " [" + Auth.CurrentDatabaseName + "]");
                        return Response.AsJson(response);
                    }
                    else
                    {
                        var response = new
                        {
                            success = false,
                            message = "Login failed - invalid credentials"
                        };
                        return Response.AsJson(response).WithStatusCode(HttpStatusCode.Unauthorized);
                    }
                }
                catch (Exception ex)
                {
                    Log("Login error: " + ex.Message);
                    var response = new
                    {
                        success = false,
                        message = ex.Message
                    };
                    return Response.AsJson(response).WithStatusCode(HttpStatusCode.InternalServerError);
                }
            });
        }

        private object GetCompanies()
        {
            try
            {
                Utils.LogRequest(this.Request);
                var all = Auth.GetAvailableDatabases();
                string current = Auth.CurrentDatabaseName;
                var result = all.Select(d => new
                {
                    companyName = d.companyName,
                    remark = d.remark,
                    databaseName = d.databaseName,
                    serverName = d.serverName,
                    isCurrent = string.Equals(d.databaseName, current, StringComparison.OrdinalIgnoreCase)
                }).ToArray();
                return Response.AsJson(new
                {
                    success = true,
                    currentDatabaseName = current,
                    currentCompanyName = Auth.CurrentCompanyName,
                    dmfFolder = Auth.CurrentDmfFolder,
                    dmfFiles = Auth.CurrentDmfFiles,
                    count = result.Length,
                    projects = result
                });
            }
            catch (Exception ex)
            {
                Log("GetCompanies error: " + ex.Message);
                return Response.AsJson(new { success = false, message = ex.Message })
                    .WithStatusCode(HttpStatusCode.InternalServerError);
            }
        }
    }
}
