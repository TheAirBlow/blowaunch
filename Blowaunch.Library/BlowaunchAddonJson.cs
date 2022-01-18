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
            foreach (BlowaunchMainJson.JsonLibrary lib in json.Libraries) {
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
        /// <param name="mojang">Mojang JSON</param>
        /// <returns>Blowaunch JSON</returns>
        public static BlowaunchAddonJson MojangToBlowaunch(MojangMainJson mojang)
        {
            var json = new BlowaunchAddonJson {
                MainClass = mojang.MainClass,
                Author = "TheAirBlow",
                Information = "Blowaunch -> Mojang",
                BaseVersion = mojang.Version
            };
            
            var libraries = new List<BlowaunchMainJson.JsonLibrary>();
            foreach (var lib in mojang.Libraries) {
                var split = lib.Name.Split(':');
                var main = new BlowaunchMainJson.JsonLibrary {
                    Allow = Array.Empty<string>(),
                    Disallow = Array.Empty<string>(),
                    Package = split[0],
                    Name = split[1],
                    Version = split[2],
                    Platform = "any",
                    Size = lib.Downloads.Artifact.Size,
                    ShaHash = lib.Downloads.Artifact.ShaHash,
                    Url = lib.Downloads.Artifact.Url
                };
                
                libraries.Add(main);
                if (lib.Downloads.Classifiers != null) {
                    if (lib.Downloads.Classifiers.NativeLinux != null) {
                        var newlib = new BlowaunchMainJson.JsonLibrary {
                            Allow = Array.Empty<string>(),
                            Disallow = Array.Empty<string>(),
                            Package = split[0],
                            Name = split[1],
                            Version = split[2],
                            Platform = "linux",
                            Size = lib.Downloads.Classifiers.NativeLinux.Size,
                            ShaHash = lib.Downloads.Classifiers.NativeLinux.ShaHash,
                            Url = lib.Downloads.Classifiers.NativeLinux.Url
                        };
                        libraries.Add(newlib);
                    } 
                    
                    if (lib.Downloads.Classifiers.NativeWindows != null) {
                        var newlib = new BlowaunchMainJson.JsonLibrary {
                            Allow = Array.Empty<string>(),
                            Disallow = Array.Empty<string>(),
                            Package = split[0],
                            Name = split[1],
                            Version = split[2],
                            Platform = "windows",
                            Size = lib.Downloads.Classifiers.NativeWindows.Size,
                            ShaHash = lib.Downloads.Classifiers.NativeWindows.ShaHash,
                            Url = lib.Downloads.Classifiers.NativeWindows.Url
                        };
                        libraries.Add(newlib);
                    }
                    
                    if (lib.Downloads.Classifiers.NativeMacOs != null) {
                        var newlib = new BlowaunchMainJson.JsonLibrary {
                            Allow = Array.Empty<string>(),
                            Disallow = Array.Empty<string>(),
                            Package = split[0],
                            Name = split[1],
                            Version = split[2],
                            Platform = "macos",
                            Size = lib.Downloads.Classifiers.NativeMacOs.Size,
                            ShaHash = lib.Downloads.Classifiers.NativeMacOs.ShaHash,
                            Url = lib.Downloads.Classifiers.NativeMacOs.Url
                        };
                        libraries.Add(newlib);
                    } 
                    
                    if (lib.Downloads.Classifiers.NativeOsx != null) {
                        var newlib = new BlowaunchMainJson.JsonLibrary {
                            Allow = Array.Empty<string>(),
                            Disallow = Array.Empty<string>(),
                            Package = split[0],
                            Name = split[1],
                            Version = split[2],
                            Platform = "osx",
                            Size = lib.Downloads.Classifiers.NativeOsx.Size,
                            ShaHash = lib.Downloads.Classifiers.NativeOsx.ShaHash,
                            Url = lib.Downloads.Classifiers.NativeOsx.Url
                        };
                        libraries.Add(newlib);
                    }
                }
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