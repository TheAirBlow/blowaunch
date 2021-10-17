using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Blowaunch.Library
{
    /// <summary>
    /// Blowaunch - Main JSON
    /// </summary>
    public class BlowaunchMainJson
    {
        /// <summary>
        /// Processes BlowaunchMainJson Libraries
        /// </summary>
        /// <param name="json">Original instance</param>
        /// <returns>Processed instance</returns>
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        private static BlowaunchMainJson ProcessLibraries(BlowaunchMainJson json)
        {
            foreach (JsonLibrary lib in json.Libraries)
            {
                if (lib.Url == null) {
                    lib.Url = lib.Platform == "any" ? new StringBuilder().AppendFormat(Fetcher.MojangEndpoints.Library, string.Join("\\", lib.Package.Split('.')), 
                            lib.Name, lib.Version, string.Empty).ToString()
                        : new StringBuilder().AppendFormat(Fetcher.MojangEndpoints.Library, lib.Package, lib.Name, lib.Version, $"-natives-{lib.Platform}").ToString();
                } else if (lib.Url.Contains("maven")) {
                    lib.Url = lib.Platform == "any" ? new StringBuilder().AppendFormat(lib.Url, string.Join("\\", lib.Package.Split('.')), 
                            lib.Name, lib.Version, string.Empty).ToString()
                        : new StringBuilder().AppendFormat(Fetcher.MojangEndpoints.Library, lib.Package, lib.Name, lib.Version, $"-natives-{lib.Platform}").ToString();
                }

                lib.ShaHash ??= Fetcher.Fetch(lib.Url.Replace(".jar", ".sha1"));
            }
            return json;
        }
        
        /// <summary>
        /// Converts Blowaunch -> Mojang
        /// </summary>
        /// <param name="mojang"></param>
        /// <returns></returns>
        public static BlowaunchMainJson MojangToBlowaunch(MojangMainJson mojang)
        {
            var json = new BlowaunchMainJson {
                MainClass = mojang.MainClass,
                Type = mojang.Type,
                Author = "TheAirBlow",
                Information = "Blowaunch -> Mojang",
                JavaMajor = mojang.JavaVersion.Major,
                Arguments = new JsonArguments(),
                Downloads = new JsonDownloads {
                    Client = mojang.Downloads.Client,
                    ClientMappings = mojang.Downloads.ClientMappings,
                    Server = mojang.Downloads.Server,
                    ServerMappings = mojang.Downloads.ServerMappings,
                },
                Logging = new JsonLogging {
                    Argument = mojang.Logging.Argument,
                    Download = mojang.Logging.Download
                },
                Assets = new JsonAssets(),
                Version = mojang.Version
            };
            var gameArguments = new List<JsonArgument>();
            var jvmArguments = new List<JsonArgument>();
            foreach (var obj in mojang.Arguments.Game)
            {
                var arg = new JsonArgument();
                var nonstring = JsonConvert.DeserializeObject<MojangMainJson.JsonNonStringArgument>((string) obj);
                if (nonstring == null)
                {
                    arg.Value = (string) obj;
                    gameArguments.Add(arg);
                    continue;
                }

                var collection = JsonConvert.DeserializeObject<List<string>>((string) obj);
                if (collection == null) arg.Value = (string) nonstring.Value;
                else arg.ValueList = collection.ToArray();
                var list1 = new List<string>();
                var list2 = new List<string>();
                foreach (var rule in nonstring.Rules)
                {
                    switch (rule.Action)
                    {
                        case MojangMainJson.JsonAction.Allow:
                            foreach (var pair in rule.Features)
                                list1.Add(pair.Key);
                            break;
                        case MojangMainJson.JsonAction.Disallow:
                            foreach (var pair in rule.Features)
                                list2.Add(pair.Key);
                            break;
                    }
                }
                arg.Allow = list1.ToArray();
                arg.Disallow = list2.ToArray();
                gameArguments.Add(arg);
            }
            
            foreach (var obj in mojang.Arguments.Java)
            {
                var arg = new JsonArgument();
                var nonstring = JsonConvert.DeserializeObject<MojangMainJson.JsonNonStringArgument>((string) obj);
                if (nonstring == null)
                {
                    arg.Value = (string) obj;
                    jvmArguments.Add(arg);
                    continue;
                }

                var collection = JsonConvert.DeserializeObject<List<string>>((string) obj);
                if (collection == null) arg.Value = (string) nonstring.Value;
                else arg.ValueList = collection.ToArray();
                var list1 = new List<string>();
                var list2 = new List<string>();
                foreach (var rule in nonstring.Rules)
                {
                    switch (rule.Action)
                    {
                        case MojangMainJson.JsonAction.Allow:
                            foreach (var pair in rule.Features)
                                list1.Add(pair.Key);
                            if (!string.IsNullOrEmpty(rule.Os.Name))
                                list1.Add($"os-name:{rule.Os.Name}");
                            if (!string.IsNullOrEmpty(rule.Os.Version))
                                list1.Add($"os-version:{rule.Os.Version}");
                            break;
                        case MojangMainJson.JsonAction.Disallow:
                            foreach (var pair in rule.Features)
                                list2.Add(pair.Key);
                            if (!string.IsNullOrEmpty(rule.Os.Name))
                                list2.Add($"os-name:{rule.Os.Name}");
                            if (!string.IsNullOrEmpty(rule.Os.Version))
                                list2.Add($"os-version:{rule.Os.Version}");
                            break;
                    }
                }
                arg.Allow = list1.ToArray();
                arg.Disallow = list2.ToArray();
                jvmArguments.Add(arg);
            }

            json.Arguments.Game = gameArguments.ToArray();
            json.Arguments.Java = jvmArguments.ToArray();
            var libraries = new List<JsonLibrary>();
            foreach (var lib in mojang.Libraries)
            {
                var newlibs = new List<JsonLibrary>();
                var newlib = new JsonLibrary();
                string[] split = lib.Name.Split(':');
                newlib.Package = split[0];
                newlib.Name = split[1];
                newlib.Version = split[2];
                var main = JsonConvert.DeserializeObject<JsonLibrary>(JsonConvert.SerializeObject(newlib));
                main.Platform = "any";
                main.Size = lib.Downloads.Artifact.Size;
                main.ShaHash = lib.Downloads.Artifact.ShaHash;
                main.Url = lib.Downloads.Artifact.Url;
                libraries.Add(main);
                if (lib.Natives != null) {
                    foreach (var native in lib.Natives)
                    {
                        var clone = JsonConvert.DeserializeObject<JsonLibrary>(JsonConvert.SerializeObject(newlib));
                        switch (native)
                        {
                            case "natives-linux":
                                clone.Size = lib.Downloads.Classifiers.NativeLinux.Size;
                                clone.ShaHash = lib.Downloads.Classifiers.NativeLinux.ShaHash;
                                clone.Url = lib.Downloads.Classifiers.NativeLinux.Url;
                                break;
                            case "natives-windows":
                                clone.Size = lib.Downloads.Classifiers.NativeWindows.Size;
                                clone.ShaHash = lib.Downloads.Classifiers.NativeWindows.ShaHash;
                                clone.Url = lib.Downloads.Classifiers.NativeWindows.Url;
                                break;
                            case "natives-macos":
                                clone.Size = lib.Downloads.Classifiers.NativeMacOs.Size;
                                clone.ShaHash = lib.Downloads.Classifiers.NativeMacOs.ShaHash;
                                clone.Url = lib.Downloads.Classifiers.NativeMacOs.Url;
                                break;
                            case "natives-osx":
                                clone.Size = lib.Downloads.Classifiers.NativeOsx.Size;
                                clone.ShaHash = lib.Downloads.Classifiers.NativeOsx.ShaHash;
                                clone.Url = lib.Downloads.Classifiers.NativeOsx.Url;
                                break;
                        }
                        libraries.Add(clone);
                    }
                }
                libraries.Add(newlib);
            }

            json.Libraries = libraries.ToArray();
            json = ProcessLibraries(json);
            return json;
        }
        
        /// <summary>
        /// Blowaunch Main JSON - Argument
        /// </summary>
        public class JsonArgument
        {
            [JsonProperty("value")] public string Value;
            [JsonProperty("valueList")] public string[] ValueList;
            [JsonProperty("allow")] public string[] Allow;
            [JsonProperty("disallow")] public string[] Disallow;
        }

        /// <summary>
        /// Blowaunch Main JSON - Arguments
        /// </summary>
        public class JsonArguments
        {
            [JsonProperty("game")] public JsonArgument[] Game;
            [JsonProperty("jvm")] public JsonArgument[] Java;
        }

        /// <summary>
        /// Blowaunch Main JSON - Assets
        /// </summary>
        public class JsonAssets
        {
            [JsonProperty("id")] public string Id;
            [JsonProperty("sha1")] public string ShaHash;
            [JsonProperty("size")] public int Size;
            [JsonProperty("sizeAssets")] public int AssetsSize;
            [JsonProperty("url")] public string Url;
        }

        /// <summary>
        /// Blowaunch Main JSON - Library
        /// </summary>
        public class JsonLibrary
        {
            [JsonProperty("platform")] public string Platform;
            [JsonProperty("package")] public string Package;
            [JsonProperty("name")] public string Name;
            [JsonProperty("version")] public string Version;
            [JsonProperty("sha1")] public string ShaHash;
            [JsonProperty("size")] public int Size;
            [JsonProperty("url")] public string Url;
        }
        
        /// <summary>
        /// Blowaunch Main JSON - Download
        /// </summary>
        public class JsonDownload
        {
            [JsonProperty("sha1")] public string ShaHash;
            [JsonProperty("size")] public int Size;
            [JsonProperty("url")] public string Url;
        }

        /// <summary>
        /// Blowaunch Main JSON - Downloads
        /// </summary>
        public class JsonDownloads
        {
            [JsonProperty("client")] public JsonDownload Client;
            [JsonProperty("client-mappings")] public JsonDownload ClientMappings;
            [JsonProperty("server")] public JsonDownload Server;
            [JsonProperty("server-mappings")] public JsonDownload ServerMappings;
        }
        
        /// <summary>
        /// Blowaunch Main JSON - Logging
        /// </summary>
        public class JsonLogging
        {
            [JsonProperty("argument")] public string Argument;
            [JsonProperty("file")] public JsonDownload Download;
        }
        
        /// <summary>
        /// Blowaunch Main JSON - Type
        /// </summary>
        public enum JsonType
        {
            [JsonProperty("snapshot")] Snapshot,
            [JsonProperty("release")] Release
        }
        
        [JsonProperty("version")] public string Version;
        [JsonProperty("author")] public string Author;
        [JsonProperty("info")] public string Information;
        [JsonProperty("java")] public int JavaMajor;
        [JsonProperty("args")] public JsonArguments Arguments;
        [JsonProperty("assets")] public JsonAssets Assets;
        [JsonProperty("libraries")] public JsonLibrary[] Libraries;
        [JsonProperty("downloads")] public JsonDownloads Downloads;
        [JsonProperty("logging")] public JsonLogging Logging;
        [JsonProperty("mainClass")] public string MainClass;
        [JsonProperty("type")] public JsonType Type;
    }
}