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
            Post("/login", _ =>
            {
                try
                {
                    string username = this.Request.Form["username"];
                    string password = this.Request.Form["password"];

                    // Fallback to query string just in case Swagger sends it there
                    if (string.IsNullOrEmpty(username)) username = this.Request.Query["username"];
                    if (string.IsNullOrEmpty(password)) password = this.Request.Query["password"];

                    // Read body manually to be absolutely certain
                    if (string.IsNullOrEmpty(username))
                    {
                        try
                        {
                            this.Request.Body.Position = 0;
                            string bodyString = Nancy.IO.RequestStream.FromStream(this.Request.Body).AsString();
                            if (!string.IsNullOrEmpty(bodyString))
                            {
                                // Try JSON
                                if (bodyString.Trim().StartsWith("{"))
                                {
                                    dynamic jsonData = JsonConvert.DeserializeObject(bodyString);
                                    username = jsonData.username?.ToString();
                                    password = jsonData.password?.ToString();
                                }
                                // Try raw form URL encoded manually
                                else if (bodyString.Contains("username=") && bodyString.Contains("password="))
                                {
                                    var queryParams = HttpUtility.ParseQueryString(bodyString);
                                    username = queryParams["username"];
                                    password = queryParams["password"];
                                }
                            }
                        }
                        catch { }
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

                    // Try to login to AutoCount
                    if (Auth.Login(Auth.userSession, username, password))
                    {
                        // Generate JWT token
                        string token = JwtHelper.GenerateToken(username);

                        var response = new
                        {
                            success = true,
                            token = token,
                            message = "Login successful"
                        };

                        Log("Login successful: " + username);
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
    }
}
