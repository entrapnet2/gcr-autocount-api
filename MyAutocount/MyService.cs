using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using Nancy.Hosting.Self;
using System.Net;

namespace GCR_autocount_api
{
    class MyService
    {
        NancyHost nancyHost;

        public bool Start(string username = "KENNY", string password = "1111")
        {
            try
            {
                HostConfiguration hostConfigs = new HostConfiguration()
                {
                    UrlReservations = new UrlReservations() { CreateAutomatically = true }
                };

                Auth.Init(username, password);

                SwaggerModule.PreloadSwaggerFiles();

                nancyHost = new NancyHost(hostConfigs, new Uri($"http://{Auth.ipAddress}:{Auth.port}"));
                nancyHost.Start();

                Utils.Log("Service started");
                Utils.Log($"Running on http://{Auth.ipAddress}:{Auth.port}");
                Utils.Log($"Swagger UI: http://{Auth.ipAddress}:{Auth.port}/swagger");

                return true;
            }
            catch (Exception ex)
            {
                Utils.Log($"Start error: {ex.Message}");
                return false;
            }
        }

        public bool Stop()
        {
            if (nancyHost != null)
            {
                nancyHost.Stop();
                Utils.Log("Service stopped");
            }
            return true;
        }
    }
}
