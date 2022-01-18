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
            var toDelete = new List<BlowaunchMainJson.JsonLibrary>();
            foreach (var lib in json.Libraries) {
                /* Seems to cause problems
                if (lib.Url == null) {
                    lib.Url = lib.Platform == "any" ? new StringBuilder().AppendFormat(Fetcher.MojangEndpoints.Library, string.Join("/", 
                                lib.Package.Split('.')), lib.Name, lib.Version, string.Empty).ToString()
                        : new StringBuilder().AppendFormat(Fetcher.MojangEndpoints.Library, string.Join("/", 
                                lib.Package.Split('.')), lib.Name, lib.Version, $"-natives-{lib.Platform}").ToString();
                }*/

                if (lib.Url == null) {
                    toDelete.Add(lib);
                    continue;
                }

                if (lib.Url.Contains("maven") && !lib.Url.EndsWith(".jar")) {
                    lib.Url = lib.Platform == "any" ? $"{lib.Url}{string.Join("/", lib.Package.Split('.'))}/{lib.Name}/{lib.Version}/" +
                                                      $"{lib.Name}-{lib.Version}.jar"
                        : new StringBuilder().AppendFormat(Fetcher.MojangEndpoints.Library, string.Join("/", 
                            lib.Package.Split('.')), lib.Name, lib.Version, $"-natives-{lib.Platform}").ToString();
                }
                
                lib.ShaHash ??= Fetcher.Fetch($"{lib.Url}.sha1");
            }

            var result = json.Libraries.ToList();
            foreach (var i in toDelete) result.Remove(i);
            json.Libraries = result.ToArray();
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
                Author = "Mojang Studios",
                Information = "Mojang JSON made to work with Blowaunch",
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
        
        /// <summary>
        /// Converts Blowaunch -> Fabric
        /// </summary>
        /// <param name="fabric">Fabric JSON</param>
        /// <returns>Blowaunch JSON</returns>
        public static BlowaunchAddonJson MojangToBlowaunch(FabricJson fabric)
        {
            var json = new BlowaunchAddonJson {
                MainClass = fabric.MainClass,
                Author = "FabricMC Contributors",
                Information = "Fabric JSON made to work with Blowaunch",
                BaseVersion = fabric.BaseVersion,
            };
            
            var libraries = new List<BlowaunchMainJson.JsonLibrary>();
            foreach (var lib in fabric.Libraries) {
                var split = lib.Name.Split(':');
                var main = new BlowaunchMainJson.JsonLibrary {
                    Allow = Array.Empty<string>(),
                    Disallow = Array.Empty<string>(),
                    Package = split[0],
                    Name = split[1],
                    Version = split[2],
                    Platform = "any",
                    Url = lib.Url
                };
                
                libraries.Add(main);
            }

            var game = new List<BlowaunchMainJson.JsonArgument>();
            var java = new List<BlowaunchMainJson.JsonArgument>();
            foreach (var i in fabric.Arguments.Game)
                game.Add(new BlowaunchMainJson.JsonArgument {
                    Allow = Array.Empty<string>(),
                    Disallow = Array.Empty<string>(),
                    ValueList = Array.Empty<string>(),
                    Value = i
                });
            foreach (var i in fabric.Arguments.Java)
                java.Add(new BlowaunchMainJson.JsonArgument {
                    Allow = Array.Empty<string>(),
                    Disallow = Array.Empty<string>(),
                    ValueList = Array.Empty<string>(),
                    Value = i
                });
            json.Arguments = new BlowaunchMainJson.JsonArguments {
                Game = game.ToArray(),
                Java = java.ToArray()
            };
            json.Libraries = libraries.ToArray();
            json = ProcessLibraries(json);
            return json;
        }
        
        [JsonProperty("baseVersion")] public string BaseVersion;
        [JsonProperty("author")] public string Author;
        [JsonProperty("info")] public string Information;
        [JsonProperty("libraries")] public BlowaunchMainJson.JsonLibrary[] Libraries;
        [JsonProperty("args")] public BlowaunchMainJson.JsonArguments Arguments;
        [JsonProperty("mainClass")] public string MainClass;
    }
}