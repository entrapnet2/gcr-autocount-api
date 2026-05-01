using System;

namespace GCR_autocount_api.NATS
{
    public class NatsConfig
    {
        public bool Enabled { get; set; } = false;
        public string Url { get; set; } = "nats://localhost:4222";
        public string CredsFile { get; set; } = "";
        public string UserJwt { get; set; } = "";
        public string UserSeed { get; set; } = "";
        public string SubjectPrefix { get; set; } = "autocount";
        
        public static NatsConfig Load()
        {
            var config = new NatsConfig();
            
            var enabled = System.Configuration.ConfigurationManager.AppSettings["NatsEnabled"];
            if (!string.IsNullOrEmpty(enabled))
                config.Enabled = enabled.Equals("true", StringComparison.OrdinalIgnoreCase);
            
            var url = System.Configuration.ConfigurationManager.AppSettings["NatsUrl"];
            if (!string.IsNullOrEmpty(url))
                config.Url = url;
            
            var credsFile = System.Configuration.ConfigurationManager.AppSettings["NatsCredsFile"];
            if (!string.IsNullOrEmpty(credsFile))
                config.CredsFile = credsFile;
            
            var userJwt = System.Configuration.ConfigurationManager.AppSettings["NatsUserJwt"];
            if (!string.IsNullOrEmpty(userJwt))
                config.UserJwt = userJwt;
            
            var userSeed = System.Configuration.ConfigurationManager.AppSettings["NatsUserSeed"];
            if (!string.IsNullOrEmpty(userSeed))
                config.UserSeed = userSeed;
            
            var subjectPrefix = System.Configuration.ConfigurationManager.AppSettings["NatsSubjectPrefix"];
            if (!string.IsNullOrEmpty(subjectPrefix))
                config.SubjectPrefix = subjectPrefix;
            
            return config;
        }
    }
}
