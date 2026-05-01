using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NATS.Client;
using static GCR_autocount_api.Utils;

namespace GCR_autocount_api.NATS
{
    public class NatsService : IDisposable
    {
        private IConnection _connection;
        private NatsConfig _config;
        private bool _disposed;
        private readonly List<IAsyncSubscription> _subscriptions = new List<IAsyncSubscription>();

        public bool IsConnected => _connection != null && _connection.State == ConnState.CONNECTED;
        public NatsConfig Config => _config;

        public bool Connect(NatsConfig config)
        {
            _config = config;
            
            if (!config.Enabled)
            {
                Log("NATS: Disabled in configuration");
                return false;
            }

            try
            {
                var opts = ConnectionFactory.GetDefaultOptions();
                opts.Url = config.Url;

                if (!string.IsNullOrEmpty(config.CredsFile))
                {
                    opts.SetUserCredentials(config.CredsFile);
                    Log($"NATS: Using creds file: {config.CredsFile}");
                }
                else if (!string.IsNullOrEmpty(config.UserJwt) && !string.IsNullOrEmpty(config.UserSeed))
                {
                    opts.SetUserCredentialsFromStrings(config.UserJwt, config.UserSeed);
                    Log("NATS: Using JWT authentication");
                }

                _connection = new ConnectionFactory().CreateConnection(opts);
                
                Log($"NATS: Connected to {config.Url}");
                return true;
            }
            catch (Exception ex)
            {
                Log($"NATS: Connection failed - {ex.Message}");
                _connection = null;
                return false;
            }
        }

        public void Publish(string subject, string data)
        {
            if (_connection == null)
                return;

            try
            {
                var fullSubject = $"{_config.SubjectPrefix}.{subject}";
                _connection.Publish(fullSubject, System.Text.Encoding.UTF8.GetBytes(data));
                Log($"NATS: Published to {fullSubject}");
            }
            catch (Exception ex)
            {
                Log($"NATS: Publish error - {ex.Message}");
            }
        }

        public void PublishEvent(string entity, string action, string docNo, object data = null)
        {
            if (_connection == null)
                return;

            var subject = $"{entity}.{action}";
            var eventData = new
            {
                entity,
                action,
                docNo,
                data,
                timestamp = DateTime.UtcNow.ToString("o")
            };

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(eventData);
            Publish(subject, json);
        }

        public void Subscribe(string subject, EventHandler<MsgHandlerEventArgs> handler)
        {
            if (_connection == null)
                return;

            try
            {
                var fullSubject = $"{_config.SubjectPrefix}.{subject}";
                var subscription = _connection.SubscribeAsync(fullSubject, handler);
                _subscriptions.Add(subscription);
                Log($"NATS: Subscribed to {fullSubject}");
            }
            catch (Exception ex)
            {
                Log($"NATS: Subscribe error - {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            foreach (var sub in _subscriptions)
            {
                try
                {
                    sub.Unsubscribe();
                }
                catch { }
            }
            _subscriptions.Clear();

            if (_connection != null)
            {
                try
                {
                    _connection.Close();
                    _connection.Dispose();
                }
                catch { }
                _connection = null;
            }

            _disposed = true;
            Log("NATS: Disconnected");
        }
    }
}
