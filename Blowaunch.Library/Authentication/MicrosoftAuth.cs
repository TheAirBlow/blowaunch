using System;
using System.Diagnostics;
using System.Text;
using Newtonsoft.Json;

namespace Blowaunch.Library.Authentication
{
    /// <summary>
    /// Microsoft Authentication
    /// </summary>
    public static class MicrosoftAuth
    {
        /// <summary>
        /// Microsoft Response JSON
        /// </summary>
        public class MicrosoftTokenJson
        {
            [JsonProperty("refresh_token")] public string RefreshToken;
            [JsonProperty("access_token")] public string AccessToken;
            [JsonProperty("expires_in")] public int ExpiresInSeconds;
        }
        
        /// <summary>
        /// Minecraft Account JSON
        /// </summary>
        public class AccountJson
        {
            [JsonProperty("id")] public string Id;
            [JsonProperty("name")] public string UserName;
            [JsonProperty("error")] public string Error;
        }
        
        /// <summary>
        /// Minecraft Response JSON
        /// </summary>
        public class MinecraftTokenJson
        {
            [JsonProperty("access_token")] public string AccessToken;
            [JsonProperty("expires_in")] public int ExpiresInSeconds;
            [JsonProperty("username")] public string Xuid;
        }

        /// <summary>
        /// Xbox Response JSON
        /// </summary>
        public class XboxJson
        {
            public class JsonXui { [JsonProperty("uhs")] public string UserHash; }
            public class DisplayClaims { [JsonProperty("xui")] public JsonXui[] Xuis; }
            [JsonProperty("DisplayClaims")] public DisplayClaims Claims;
            [JsonProperty("Token")] public string Token;
            [JsonProperty("XErr")] public int Error;
        }
        
        /// <summary>
        /// Opens Microsoft OAuth 2.0 in browser
        /// </summary>
        public static void OpenAuth()
            => Process.Start(Endpoints.Microsoft.LoginBrowser);
        
        /// <summary>
        /// Authenticates with microsoft response JSON
        /// </summary>
        /// <param name="microsoftJson">Response JSON</param>
        /// <param name="config">Configuration</param>
        public static bool Authenticate(string microsoftJson, ref Runner.Configuration config)
        {
            if (config.Auth.Type != Runner.Configuration.AuthClass.AuthType.Microsoft &&
                config.Auth.Type != Runner.Configuration.AuthClass.AuthType.PretendMicrosoft)
                return false;
            var responseJson = JsonConvert.DeserializeObject<MicrosoftTokenJson>(microsoftJson);
            if (responseJson == null || string.IsNullOrEmpty(responseJson.AccessToken)) return false;
            config.Auth.ValidUntil = DateTime.Now + TimeSpan.FromSeconds(responseJson.ExpiresInSeconds);
            return AuthMain(responseJson, ref config);
        }

        /// <summary>
        /// Check authentication status
        /// </summary>
        /// <param name="config">Configuration</param>
        public static bool CheckAuth(ref Runner.Configuration config)
        {
            if (config.Auth.ValidUntil > DateTime.Now) return true;
            var responseJson = JsonConvert.DeserializeObject<MicrosoftTokenJson>(
                Fetcher.Fetch(new StringBuilder().AppendFormat(Endpoints.Microsoft.Refresh, 
                    config.Auth.RefreshToken).ToString()));
            if (responseJson == null || string.IsNullOrEmpty(responseJson.AccessToken)) return false;
            config.Auth.ValidUntil = DateTime.Now + TimeSpan.FromSeconds(responseJson.ExpiresInSeconds);
            return AuthMain(responseJson, ref config);
        }

        /// <summary>
        /// Main authentication method
        /// </summary>
        /// <param name="json">Response JSON</param>
        /// <param name="config">Configuration</param>
        private static bool AuthMain(MicrosoftTokenJson json, ref Runner.Configuration config)
        {
            // Exception here - Authentication error
            var xboxJson = JsonConvert.DeserializeObject<XboxJson>(
                Fetcher.Fetch(new StringBuilder().AppendFormat(Endpoints.Microsoft.XboxLogin,
                    json.AccessToken).ToString()));
            if (xboxJson == null || string.IsNullOrEmpty(xboxJson.Token)) return false;
            // Exception here - Authentication error, XErr code
            var xstsJson = JsonConvert.DeserializeObject<XboxJson>(
                Fetcher.Fetch(new StringBuilder().AppendFormat(Endpoints.Microsoft.XboxXsts,
                    xboxJson.Token, xboxJson.Claims.Xuis[0].UserHash).ToString()));
            if (xstsJson == null || string.IsNullOrEmpty(xstsJson.Token)) return false;
            // Exception here - Unexpected error
            var minecraftJson = JsonConvert.DeserializeObject<MinecraftTokenJson>(
                Fetcher.Fetch(new StringBuilder().AppendFormat(Endpoints.Microsoft.MinecraftLogin,
                    xboxJson.Token, xboxJson.Claims.Xuis[0].UserHash).ToString()));
            if (minecraftJson == null || string.IsNullOrEmpty(minecraftJson.AccessToken)) return false;
            config.Auth.Token = minecraftJson.AccessToken;
            config.Auth.Xuid = minecraftJson.Xuid;
            if (config.Auth.Type != Runner.Configuration.AuthClass.AuthType.PretendMicrosoft) {
                if (Fetcher.Fetch(new StringBuilder().AppendFormat(Endpoints.Microsoft.Ownership,
                    minecraftJson.AccessToken).ToString()) != "ownership-confirmed") return false;
                // Exception here - Unexpected error
                var profileJson = JsonConvert.DeserializeObject<AccountJson>(
                    Fetcher.Fetch(new StringBuilder().AppendFormat(Endpoints.Microsoft.Profile,
                        minecraftJson.AccessToken).ToString()));
                if (profileJson == null || string.IsNullOrEmpty(profileJson.Id)) return false;
                config.UserName = profileJson.UserName;
                config.Auth.Uuid = profileJson.Id;
            }
            return true;
        }
    }
}