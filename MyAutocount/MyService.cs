using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using Nancy.Hosting.Self;
using System.Net;
using GCR_autocount_api.NATS;
 
namespace GCR_autocount_api
{
    class MyService
    {
        NancyHost nancyHost;
        NatsService natsService;

        public static NatsService Nats => Instance?.natsService;
        private static MyService Instance;

        public bool Start(string username = "KENNY", string password = "1111")
        {
            Instance = this;
            
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

                InitNats();

                return true;
            }
            catch (Exception ex)
            {
                Utils.Log($"Start error: {ex.Message}");
                return false;
            }
        }

        private void InitNats()
        {
            try
            {
                var config = NatsConfig.Load();
                natsService = new NatsService();
                
                if (natsService.Connect(config))
                {
                    Utils.Log($"NATS: Connected to {config.Url}");
                }
            }
            catch (Exception ex)
            {
                Utils.Log($"NATS: Init error - {ex.Message}");
            }
        }

        public bool Stop()
        {
            if (natsService != null)
            {
                natsService.Dispose();
                natsService = null;
            }
            
            if (nancyHost != null)
            {
                nancyHost.Stop();
                Utils.Log("Service stopped");
            }
            return true;
        }
    }
}
