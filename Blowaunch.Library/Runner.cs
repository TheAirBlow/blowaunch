using System;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using Blowaunch.Library.Downloader;
using Newtonsoft.Json;
using Serilog.Core;

namespace Blowaunch.Library
{
    /// <summary>
    /// Blowaunch Runner
    /// </summary>
    public static class Runner
    {
        /// <summary>
        /// Configuration class
        /// </summary>
        public class Configuration
        {
            /// <summary>
            /// Authentication
            /// </summary>
            public class AuthClass
            {
                /// <summary>
                /// Authentication type
                /// </summary>
                public enum AuthType
                {
                    [JsonProperty("microsoft")] Microsoft,
                    [JsonProperty("mojang")] Mojang,
                    [JsonProperty("none")] None
                }
                
                [JsonProperty("type")] public AuthType Type;
                [JsonProperty("xuid")] public string Xuid;
                [JsonProperty("clientid")] public string ClientId;
                [JsonProperty("uuid")] public string Uuid;
                [JsonProperty("token")] public string Token;
            }
            
            [JsonProperty("maxRam")] public string RamMax;
            [JsonProperty("jvmArgs")] public string JvmArgs;
            [JsonProperty("gameArgs")] public string GameArgs;
            [JsonProperty("username")] public string UserName;
            [JsonProperty("customResolution")] public bool CustomWindowSize;
            [JsonProperty("windowSize")] public Vector2 WindowSize;
            [JsonProperty("isDemo")] public bool DemoUser;
            [JsonProperty("auth")] public AuthClass Auth = new AuthClass();
        }

        /// <summary>
        /// Generate run command
        /// </summary>
        /// <param name="main">Blowaunch Main JSON</param>
        /// <param name="logger">Serilog Logger</param>
        /// <param name="config">Configuration</param>
        /// <returns>Generated command</returns>
        public static string GenerateCommand(BlowaunchMainJson main, Configuration config, Logger logger)
        {
            var sb = new StringBuilder();
            sb.Append(string.IsNullOrEmpty(config.JvmArgs)
                ? $"-Xms{config.RamMax} -Xmx{config.RamMax} "
                : $"-Xms{config.RamMax} -Xmx{config.RamMax} {config.JvmArgs} ");
            foreach (BlowaunchMainJson.JsonArgument arg in main.Arguments.Java)
            {
                bool process = true;
                foreach (string str in arg.Disallow)
                {
                    if (process == false) continue;
                    process = !CheckBool(config, str);
                }
                if (process == false) continue;
                foreach (string str in arg.Allow)
                {
                    if (process == false) continue;
                    process = CheckBool(config, str);
                }
                if (process == false) continue;
                if (arg.ValueList.Length != 0)
                {
                    foreach (string str in arg.ValueList)
                        sb.Append($"{ReplacerJava(main, str)} ");
                    continue;
                }

                sb.Append($"{ReplacerJava(main, arg.Value)} ");
            }
            foreach (BlowaunchMainJson.JsonArgument arg in main.Arguments.Game)
            {
                bool process = true;
                foreach (string str in arg.Disallow)
                {
                    if (process == false) continue;
                    process = !CheckBool(config, str);
                }
                if (process == false) continue;
                foreach (string str in arg.Allow)
                {
                    if (process == false) continue;
                    process = CheckBool(config, str);
                }
                if (process == false) continue;
                if (arg.ValueList.Length != 0)
                {
                    foreach (string str in arg.ValueList)
                        sb.Append($"{ReplacerGame(config, str, main)} ");
                    continue;
                }

                sb.Append($"{ReplacerGame(config, arg.Value, main)} ");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Generates classpath string
        /// </summary>
        /// <param name="main">Blowaunch Main JSON</param>
        /// <returns>Classpath string</returns>
        private static string GenerateClasspath(BlowaunchMainJson main)
        {
            var sb = new StringBuilder();
            for (var index = 0; index < main.Libraries.Length; index++)
            {
                BlowaunchMainJson.JsonLibrary lib = main.Libraries[index];
                if (index == main.Libraries.Length - 1) sb.Append($"{FilesManager.GetLibraryPath(lib)}");
                else sb.Append($"{FilesManager.GetLibraryPath(lib)}:");
            }
            return sb.ToString();
        } 
        
        /// <summary>
        /// Replaces arguments with required values
        /// </summary>
        /// <param name="main">Blowaunch Main JSON</param>
        /// <param name="str">String</param>
        /// <returns></returns>
        private static string ReplacerJava(BlowaunchMainJson main, string str)
        {
            return str.Replace("-Djava.library.path=${natives_directory}", "")
                .Replace("${launcher_name}", "Blowaunch")
                .Replace("${launcher_version}", Assembly.GetExecutingAssembly().GetName().FullName)
                .Replace("${classpath}", GenerateClasspath(main));
        }
        
        /// <summary>
        /// Replaces arguments with required values
        /// </summary>
        /// <param name="main">Blowaunch Main JSON</param>
        /// <param name="config">Configuration</param>
        /// <param name="str">String</param>
        /// <returns></returns>
        private static string ReplacerGame(Configuration config, string str, BlowaunchMainJson main)
        {
            string newstr = str;
            newstr = newstr.Replace("${auth_player_name}", config.UserName)
                .Replace("${assets_root}", FilesManager.Directories.AssetsRoot)
                .Replace("${game_directory}", FilesManager.Directories.Root)
                .Replace("${version_type}", main.Type.ToString().ToLower())
                .Replace("${assets_index_name}", main.Assets.Id)
                .Replace("${version_name}", main.Version);
            if (config.Auth.Type == Configuration.AuthClass.AuthType.None)
                newstr = newstr.Replace("${clientid}", "noauth")
                    .Replace("${auth_access_token}", "noauth")
                    .Replace("${user_type}", "noauth")
                    .Replace("${auth_xuid}", "noauth")
                    .Replace("${auth_uuid}", "noauth");
            else {
                newstr = newstr.Replace("${user_type}", config.Auth.Type.ToString().ToLower());
                switch (config.Auth.Type) {
                    case Configuration.AuthClass.AuthType.Microsoft:
                        newstr = newstr.Replace("${clientid}", config.Auth.ClientId)
                            .Replace("${auth_access_token}", "noauth")
                            .Replace("${auth_xuid}", config.Auth.Xuid)
                            .Replace("${auth_uuid}", "noauth");
                        break;
                    case Configuration.AuthClass.AuthType.Mojang:
                        newstr = newstr.Replace("${clientid}", "noauth")
                            .Replace("${auth_access_token}", config.Auth.Token)
                            .Replace("${auth_xuid}", "noauth")
                            .Replace("${auth_uuid}", config.Auth.Uuid);
                        break;
                }
            }
            return newstr;
        }

        /// <summary>
        /// Check Allow/Disallow value
        /// </summary>
        /// <param name="config">Configuration</param>
        /// <param name="str">String</param>
        /// <returns></returns>
        private static bool CheckBool(Configuration config, string str)
        {
            switch (str)
            {
                case "is_demo_user":
                    return config.DemoUser;
                case "has_custom_resolution":
                    return config.CustomWindowSize;
                default:
                    if (str.StartsWith("os-name:"))
                        return Environment.OSVersion.Platform.ToString().ToLower() == str.Substring(8);
                    if (str.StartsWith("os-version:"))
                        return Environment.OSVersion.Version.ToString().ToLower() == str.Substring(11);
                    return false;
            }
        }

        /// <summary>
        /// Generate run command
        /// </summary>
        /// <param name="main">Blowaunch Main JSON</param>
        /// <param name="logger">Serilog Logger</param>
        /// <param name="addon">Blowaunch Addon JSON</param>
        /// <param name="config">Configuration</param>
        /// <returns>Generated command</returns>
        public static string GenerateCommand(BlowaunchMainJson main, BlowaunchAddonJson addon, Configuration config, Logger logger)
        {
            var newlibs = main.Libraries.ToList();
            newlibs.AddRange(addon.Libraries);
            main.Libraries = newlibs.ToArray();
            main.MainClass = addon.MainClass;
            return GenerateCommand(main, config, logger);
        }
    }
}