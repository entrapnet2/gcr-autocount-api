using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace GCR_autocount_api
{
    public class JwtHelper
    {
        private static readonly string SecretKey = "MyAutocountSecretKey2024!@#$%"; // Should be in settings.json

        public static string GenerateToken(string username, int expireMinutes = 60)
        {
            var header = new { alg = "HS256", typ = "JWT" };
            var now = DateTime.UtcNow;
            var payload = new
            {
                sub = username,
                iat = ToUnixTime(now),
                exp = ToUnixTime(now.AddMinutes(expireMinutes))
            };

            string headerJson = Newtonsoft.Json.JsonConvert.SerializeObject(header);
            string payloadJson = Newtonsoft.Json.JsonConvert.SerializeObject(payload);

            string headerBase64 = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
            string payloadBase64 = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));

            string signatureInput = $"{headerBase64}.{payloadBase64}";
            string signature = Base64UrlEncode(HashHMACSHA256(signatureInput, SecretKey));

            return $"{signatureInput}.{signature}";
        }

        public static bool ValidateToken(string token)
        {
            try
            {
                var parts = token.Split('.');
                if (parts.Length != 3) return false;

                string signatureInput = $"{parts[0]}.{parts[1]}";
                string expectedSignature = Base64UrlEncode(HashHMACSHA256(signatureInput, SecretKey));

                if (parts[2] != expectedSignature) return false;

                string payloadJson = Base64UrlDecode(parts[1]);
                dynamic payload = Newtonsoft.Json.JsonConvert.DeserializeObject(payloadJson);
                long exp = payload.exp;

                return ToUnixTime(DateTime.UtcNow) < exp;
            }
            catch
            {
                return false;
            }
        }

        public static string GetUsernameFromToken(string token)
        {
            try
            {
                var parts = token.Split('.');
                if (parts.Length != 3) return null;

                string payloadJson = Base64UrlDecode(parts[1]);
                dynamic payload = Newtonsoft.Json.JsonConvert.DeserializeObject(payloadJson);
                return payload.sub;
            }
            catch
            {
                return null;
            }
        }

        private static byte[] HashHMACSHA256(string input, string key)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key)))
            {
                return hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
            }
        }

        private static string Base64UrlEncode(byte[] input)
        {
            return Convert.ToBase64String(input).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        private static string Base64UrlDecode(string input)
        {
            string base64 = input.Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            byte[] bytes = Convert.FromBase64String(base64);
            return Encoding.UTF8.GetString(bytes);
        }

        private static long ToUnixTime(DateTime date)
        {
            return (long)(date.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
    }
}
