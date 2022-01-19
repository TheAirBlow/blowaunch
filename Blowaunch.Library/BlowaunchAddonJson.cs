using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
                Arguments = new BlowaunchMainJson.JsonArguments(),
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
                    Path = lib.Downloads.Artifact.Path,
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
                            Path = lib.Downloads.Classifiers.NativeLinux.Path,
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
                            Path = lib.Downloads.Classifiers.NativeWindows.Path,
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
                            Path = lib.Downloads.Classifiers.NativeMacOs.Path,
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
                            Path = lib.Downloads.Classifiers.NativeOsx.Path,
                            Size = lib.Downloads.Classifiers.NativeOsx.Size,
                            ShaHash = lib.Downloads.Classifiers.NativeOsx.ShaHash,
                            Url = lib.Downloads.Classifiers.NativeOsx.Url
                        };
                        libraries.Add(newlib);
                    }
                }
            }
            
            var gameArguments = new List<BlowaunchMainJson.JsonArgument>();
            var jvmArguments = new List<BlowaunchMainJson.JsonArgument>();
            foreach (var obj in mojang.Arguments.Game) {
                var arg = new BlowaunchMainJson.JsonArgument {
                    Allow = Array.Empty<string>(),
                    Disallow = Array.Empty<string>(),
                    ValueList = Array.Empty<string>(),
                    Value = ""
                };
                if (obj is JObject a) {
                    var nonstring = JsonConvert.DeserializeObject<MojangMainJson.JsonNonStringArgument>(a.ToString());
                    if (nonstring.Value is JArray o) {
                        var collection = JsonConvert.DeserializeObject<string[]>(o.ToString());
                        if (collection != null) arg.ValueList = collection;
                    } else arg.Value = (string) nonstring.Value;
                    var list1 = new List<string>();
                    var list2 = new List<string>();
                    foreach (var rule in nonstring.Rules) {
                        switch (rule.Action) {
                            case MojangMainJson.JsonAction.allow:
                                if (rule.Os != null) {
                                    if (rule.Os.Name != null)
                                        list1.Add($"os-name:{rule.Os.Name}");
                                    if (rule.Os.Version != null)
                                        list1.Add($"os-version:{rule.Os.Version}");
                                }
                                if (rule.Features != null)
                                    foreach (var pair in rule.Features)
                                        list1.Add(pair.Key);
                                break;
                            case MojangMainJson.JsonAction.disallow:
                                if (rule.Os != null) {
                                    if (rule.Os.Name != null)
                                        list2.Add($"os-name:{rule.Os.Name}");
                                    if (rule.Os.Version != null)
                                        list2.Add($"os-version:{rule.Os.Version}");
                                }
                                if (rule.Features != null)
                                    foreach (var pair in rule.Features)
                                        list2.Add(pair.Key);
                                break;
                        }
                    }
                    arg.Allow = list1.ToArray();
                    arg.Disallow = list2.ToArray();
                } else arg.Value = (string)obj;
                gameArguments.Add(arg);
            }
            
            foreach (var obj in mojang.Arguments.Java) {
                var arg = new BlowaunchMainJson.JsonArgument {
                    Allow = Array.Empty<string>(),
                    Disallow = Array.Empty<string>(),
                    ValueList = Array.Empty<string>(),
                    Value = ""
                };
                if (obj is JObject a) {
                    var nonstring = JsonConvert.DeserializeObject<MojangMainJson.JsonNonStringArgument>(a.ToString());
                    if (nonstring.Value is JArray o) {
                        var collection = JsonConvert.DeserializeObject<string[]>(o.ToString());
                        if (collection != null) arg.ValueList = collection;
                    } else arg.Value = (string) nonstring.Value;
                    var list1 = new List<string>();
                    var list2 = new List<string>();
                    foreach (var rule in nonstring.Rules) {
                        switch (rule.Action) {
                            case MojangMainJson.JsonAction.allow:
                                if (rule.Os != null) {
                                    if (rule.Os.Name != null)
                                        list1.Add($"os-name:{rule.Os.Name}");
                                    if (rule.Os.Version != null)
                                        list1.Add($"os-version:{rule.Os.Version}");
                                }
                                if (rule.Features != null)
                                    foreach (var pair in rule.Features)
                                        list1.Add(pair.Key);
                                break;
                            case MojangMainJson.JsonAction.disallow:
                                if (rule.Os != null) {
                                    if (rule.Os.Name != null)
                                        list2.Add($"os-name:{rule.Os.Name}");
                                    if (rule.Os.Version != null)
                                        list2.Add($"os-version:{rule.Os.Version}");
                                }
                                if (rule.Features != null)
                                    foreach (var pair in rule.Features)
                                        list2.Add(pair.Key);
                                break;
                        }
                    }
                    arg.Allow = list1.ToArray();
                    arg.Disallow = list2.ToArray();
                } else arg.Value = (string)obj;
                jvmArguments.Add(arg);
            }

            json.Arguments.Game = gameArguments.ToArray();
            json.Arguments.Java = jvmArguments.ToArray();
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
        
        [JsonProperty("legacy")] public bool Legacy;
        [JsonProperty("baseVersion")] public string BaseVersion;
        [JsonProperty("author")] public string Author;
        [JsonProperty("info")] public string Information;
        [JsonProperty("libraries")] public BlowaunchMainJson.JsonLibrary[] Libraries;
        [JsonProperty("args")] public BlowaunchMainJson.JsonArguments Arguments;
        [JsonProperty("mainClass")] public string MainClass;
    }
}