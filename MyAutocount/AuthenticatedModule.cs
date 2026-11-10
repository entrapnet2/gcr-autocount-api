using System;
using System.Linq;
using Nancy;
using static GCR_autocount_api.Utils;

namespace GCR_autocount_api
{
    public abstract class AuthenticatedModule : NancyModule
    {
        protected AuthenticatedModule()
        {
            Before.AddItemToEndOfPipeline(ValidateJwtToken);
        }

        protected AuthenticatedModule(string modulePath) : base(modulePath)
        {
            Before.AddItemToEndOfPipeline(ValidateJwtToken);
        }

        private Response ValidateJwtToken(NancyContext context)
        {
            string path = context.Request.Path.ToLower();

            if (path == "/login" || path.StartsWith("/swagger")
                || path == "/companies" || path == "/projects" || path == "/databases")
            {
                return null;
            }

            string token = GetTokenFromRequest(context.Request);

            if (string.IsNullOrEmpty(token))
            {
                Log("No JWT token provided");
                return CreateUnauthorizedResponse("No token provided");
            }

            if (!JwtHelper.ValidateToken(token))
            {
                Log("Invalid or expired JWT token");
                return CreateUnauthorizedResponse("Invalid or expired token");
            }

            string username = JwtHelper.GetUsernameFromToken(token);
            Log($"Authenticated request from: {username}");

            // If token is bound to a project but server has since switched, force re-login
            // so the caller never reads/writes the wrong company by accident.
            string tokenDb = null;
            try { tokenDb = JwtHelper.GetDatabaseNameFromToken(token); } catch { }
            string activeDb = null;
            try { activeDb = Auth.CurrentDatabaseName; } catch { }
            if (!string.IsNullOrEmpty(tokenDb) && !string.IsNullOrEmpty(activeDb)
                && !string.Equals(tokenDb, activeDb, StringComparison.OrdinalIgnoreCase))
            {
                Log($"Project mismatch: token for [{tokenDb}], active [{activeDb}]");
                return CreateUnauthorizedResponse(
                    "Active project is '" + activeDb + "' but token is for '" + tokenDb + "'. Please re-login with project='" + activeDb + "' (see GET /companies).");
            }

            // Make active project visible to downstream modules
            try
            {
                context.Items["AuthUsername"] = username;
                context.Items["AuthDatabaseName"] = activeDb;
            }
            catch { }

            return null;
        }

        private string GetTokenFromRequest(Request request)
        {
            Log("Request path: " + request.Path);
            foreach (var header in request.Headers)
            {
                Log("Header: " + header.Key + " = " + string.Join(", ", header.Value));
            }

            foreach (var header in request.Headers)
            {
                if (header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
                {
                    string authHeader = header.Value.FirstOrDefault();
                    Log("Found Authorization header: " + authHeader);
                    if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        return authHeader.Substring(7).Trim();
                    }
                }
            }

            if (request.Query.ContainsKey("token"))
            {
                Log("Found token in query: " + request.Query["token"]);
                return request.Query["token"];
            }

            Log("No token found in request");
            return null;
        }

        private Response CreateUnauthorizedResponse(string message)
        {
            var response = (Response)Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                success = false,
                message = message
            });
            response.StatusCode = HttpStatusCode.Unauthorized;
            response.ContentType = "application/json";
            return response;
        }
    }
}
