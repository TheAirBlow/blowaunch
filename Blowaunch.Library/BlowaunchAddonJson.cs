using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Newtonsoft.Json;

namespace Blowaunch.Library
{
    /// <summary>
    /// Blowaunch - Addon JSON
    /// </summary>
    public class BlowaunchAddonJson
    {
        /// <summary>
        /// Processes BlowaunchAddonJson Libraries
        /// </summary>
        /// <param name="json">Original instance</param>
        /// <returns>Processed instance</returns>
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        private static BlowaunchAddonJson ProcessLibraries(BlowaunchAddonJson json)
        {
            foreach (BlowaunchMainJson.JsonLibrary lib in json.Libraries)
            {
                if (lib.Url == null) {
                    lib.Url = lib.Platform == "any" ? new StringBuilder().AppendFormat(Fetcher.MojangEndpoints.Library, lib.Package, lib.Name, lib.Version, string.Empty).ToString()
                        : new StringBuilder().AppendFormat(Fetcher.MojangEndpoints.Library, lib.Package, lib.Name, lib.Version, $"-natives-{lib.Platform}").ToString();
                } else if (lib.Url.Contains("maven")) {
                    lib.Url = lib.Platform == "any" ? new StringBuilder().AppendFormat(lib.Url, lib.Package, lib.Name, lib.Version, string.Empty).ToString()
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
        public static BlowaunchAddonJson MojangToBlowaunch(MojangMainJson mojang)
        {
            var json = new BlowaunchAddonJson {
                MainClass = mojang.MainClass,
                Author = "TheAirBlow",
                Information = "Blowaunch -> Mojang",
                BaseVersion = mojang.Version
            };
            
            var libraries = new List<BlowaunchMainJson.JsonLibrary>();
            foreach (var lib in mojang.Libraries)
            {
                var newlibs = new List<BlowaunchMainJson.JsonLibrary>();
                var newlib = new BlowaunchMainJson.JsonLibrary();
                string[] split = lib.Name.Split(':');
                newlib.Package = split[0];
                newlib.Name = split[1];
                newlib.Version = split[2];
                var main = JsonConvert.DeserializeObject<BlowaunchMainJson.JsonLibrary>(JsonConvert.SerializeObject(newlib));
                main.Platform = "any";
                main.Size = lib.Downloads.Artifact.Size;
                main.ShaHash = lib.Downloads.Artifact.ShaHash;
                main.Url = lib.Downloads.Artifact.Url;
                libraries.Add(main);
                if (lib.Natives != null) {
                    foreach (var native in lib.Natives)
                    {
                        var clone = JsonConvert.DeserializeObject<BlowaunchMainJson.JsonLibrary>(JsonConvert.SerializeObject(newlib));
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
        
        [JsonProperty("baseVersion")] public string BaseVersion;
        [JsonProperty("author")] public string Author;
        [JsonProperty("info")] public string Information;
        [JsonProperty("libraries")] public BlowaunchMainJson.JsonLibrary[] Libraries;
        [JsonProperty("mainClass")] public string MainClass;
    }
}