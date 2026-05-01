using System;
using System.Linq;
using Nancy;
using static GCR_autocount_api.Utils;
using GCR_autocount_api.NATS;

namespace GCR_autocount_api
{
    public abstract class AuthenticatedModule : NancyModule
    {
        protected NatsService Nats => MyService.Nats;
        
        protected void PublishEvent(string entity, string action, string docNo, object data = null)
        {
            if (Nats != null && Nats.IsConnected)
            {
                Nats.PublishEvent(entity, action, docNo, data);
            }
        }
        
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

            // Skip JWT validation for login and swagger endpoints
            if (path == "/login" || path.StartsWith("/swagger"))
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

            return null; // Continue with the request
        }

        private string GetTokenFromRequest(Request request)
        {
            // Debug: Log all headers
            Log("Request path: " + request.Path);
            foreach (var header in request.Headers)
            {
                Log("Header: " + header.Key + " = " + string.Join(", ", header.Value));
            }

            // Check Authorization header
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

            // Check query parameter
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
