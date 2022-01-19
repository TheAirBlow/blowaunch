using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog.Core;

namespace Blowaunch.Library
{
    /// <summary>
    /// Blowaunch - Main JSON
    ///
    /// This class became a pain in the ass:
    /// there is a lot of copy-paste.
    /// TODO: Refactor code
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
        public static BlowaunchMainJson MojangToBlowaunch(MojangMainJson mojang)
        {
            var json = new BlowaunchMainJson {
                MainClass = mojang.MainClass,
                Type = mojang.Type,
                Author = "Mojang Studios",
                Information = "Mojang JSON made to work with Blowaunch",
                JavaMajor = mojang.JavaVersion.Major,
                Arguments = new JsonArguments(),
                Downloads = new JsonDownloads {
                    Client = mojang.Downloads.Client,
                    ClientMappings = mojang.Downloads.ClientMappings,
                    Server = mojang.Downloads.Server,
                    ServerMappings = mojang.Downloads.ServerMappings,
                },
                Logging = new JsonLogging {
                    Argument = mojang.Logging.Client.Argument,
                    Download = mojang.Logging.Client.Download
                },
                Assets = new JsonAssets {
                    Id = mojang.Assets.Id,
                    AssetsSize = mojang.Assets.AssetsSize,
                    ShaHash = mojang.Assets.ShaHash,
                    Size = mojang.Assets.Size,
                    Url = mojang.Assets.Url
                },
                Version = mojang.Version,
                Legacy = false
            };
            var gameArguments = new List<JsonArgument>();
            var jvmArguments = new List<JsonArgument>();
            foreach (var obj in mojang.Arguments.Game) {
                var arg = new JsonArgument {
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
                var arg = new JsonArgument {
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

            void ProcessLibraryRules(JsonLibrary lib, MojangMainJson.JsonLibrary lib2) {
                if (lib2.Rules == null) return;
                var list1 = new List<string>();
                var list2 = new List<string>();
                foreach (var rule in lib2.Rules) {
                    switch (rule.Action) {
                        case MojangMainJson.JsonAction.allow:
                            if (rule.Os != null) {
                                if (rule.Os.Name != null)
                                    list1.Add($"os-name:{rule.Os.Name}");
                                if (rule.Os.Version != null)
                                    list1.Add($"os-version:{rule.Os.Version}");
                            }
                            break;
                        case MojangMainJson.JsonAction.disallow:
                            if (rule.Os != null) {
                                if (rule.Os.Name != null)
                                    list2.Add($"os-name:{rule.Os.Name}");
                                if (rule.Os.Version != null)
                                    list2.Add($"os-version:{rule.Os.Version}");
                            }
                            break;
                    }
                }
                lib.Allow = list1.ToArray();
                lib.Disallow = list2.ToArray();
            }

            json.Arguments.Game = gameArguments.ToArray();
            json.Arguments.Java = jvmArguments.ToArray();
            var libraries = new List<JsonLibrary>();
            foreach (var lib in mojang.Libraries) {
                var split = lib.Name.Split(':');
                var main = new JsonLibrary {
                    Allow = Array.Empty<string>(),
                    Disallow = Array.Empty<string>(),
                    Package = split[0],
                    Name = split[1],
                    Version = split[2],
                    Platform = "any",
                    Path = lib.Downloads.Artifact.Path,
                    Size = lib.Downloads.Artifact.Size,
                    ShaHash = lib.Downloads.Artifact.ShaHash,
                    Url = lib.Downloads.Artifact.Url,
                    Exclude = Array.Empty<string>(),
                    Extract = false
                };
                
                ProcessLibraryRules(main, lib);
                libraries.Add(main);
                if (lib.Downloads.Classifiers != null) {
                    if (lib.Downloads.Classifiers.NativeLinux != null) {
                        var newlib = new JsonLibrary {
                            Allow = Array.Empty<string>(),
                            Disallow = Array.Empty<string>(),
                            Package = split[0],
                            Name = split[1],
                            Version = split[2],
                            Platform = "linux",
                            Path = lib.Downloads.Classifiers.NativeLinux.Path,
                            Size = lib.Downloads.Classifiers.NativeLinux.Size,
                            ShaHash = lib.Downloads.Classifiers.NativeLinux.ShaHash,
                            Url = lib.Downloads.Classifiers.NativeLinux.Url,
                            Exclude = Array.Empty<string>(),
                            Extract = false
                        };
                        ProcessLibraryRules(newlib, lib);
                        libraries.Add(newlib);
                    } 
                    
                    if (lib.Downloads.Classifiers.NativeWindows != null) {
                        var newlib = new JsonLibrary {
                            Allow = Array.Empty<string>(),
                            Disallow = Array.Empty<string>(),
                            Package = split[0],
                            Name = split[1],
                            Version = split[2],
                            Platform = "windows",
                            Path = lib.Downloads.Classifiers.NativeWindows.Path,
                            Size = lib.Downloads.Classifiers.NativeWindows.Size,
                            ShaHash = lib.Downloads.Classifiers.NativeWindows.ShaHash,
                            Url = lib.Downloads.Classifiers.NativeWindows.Url,
                            Exclude = Array.Empty<string>(),
                            Extract = false
                        };
                        ProcessLibraryRules(newlib, lib);
                        libraries.Add(newlib);
                    }
                    
                    if (lib.Downloads.Classifiers.NativeMacOs != null) {
                        var newlib = new JsonLibrary {
                            Allow = Array.Empty<string>(),
                            Disallow = Array.Empty<string>(),
                            Package = split[0],
                            Name = split[1],
                            Version = split[2],
                            Platform = "macos",
                            Path = lib.Downloads.Classifiers.NativeMacOs.Path,
                            Size = lib.Downloads.Classifiers.NativeMacOs.Size,
                            ShaHash = lib.Downloads.Classifiers.NativeMacOs.ShaHash,
                            Url = lib.Downloads.Classifiers.NativeMacOs.Url,
                            Exclude = Array.Empty<string>(),
                            Extract = false
                        };
                        ProcessLibraryRules(newlib, lib);
                        libraries.Add(newlib);
                    } 
                    
                    if (lib.Downloads.Classifiers.NativeOsx != null) {
                        var newlib = new JsonLibrary {
                            Allow = Array.Empty<string>(),
                            Disallow = Array.Empty<string>(),
                            Package = split[0],
                            Name = split[1],
                            Version = split[2],
                            Platform = "osx",
                            Path = lib.Downloads.Classifiers.NativeOsx.Path,
                            Size = lib.Downloads.Classifiers.NativeOsx.Size,
                            ShaHash = lib.Downloads.Classifiers.NativeOsx.ShaHash,
                            Url = lib.Downloads.Classifiers.NativeOsx.Url,
                            Exclude = Array.Empty<string>(),
                            Extract = false
                        };
                        ProcessLibraryRules(newlib, lib);
                        libraries.Add(newlib);
                    }
                }
            }

            json.Libraries = libraries.ToArray();
            json = ProcessLibraries(json);
            return json;
        }
        
        /// <summary>
        /// Converts Blowaunch -> Mojang
        /// </summary>
        /// <param name="mojang">Mojang JSON</param>
        /// <returns>Blowaunch JSON</returns>
        public static BlowaunchMainJson MojangToBlowaunchPartial(MojangMainJson mojang)
        {
            var json = new BlowaunchMainJson {
                MainClass = mojang.MainClass,
                Type = mojang.Type,
                Author = "Mojang Studios",
                Information = "Mojang JSON made to work with Blowaunch",
                Arguments = new JsonArguments(),
                Version = mojang.Version,
                Legacy = false
            };
            var gameArguments = new List<JsonArgument>();
            var jvmArguments = new List<JsonArgument>();
            foreach (var obj in mojang.Arguments.Game) {
                var arg = new JsonArgument {
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
                var arg = new JsonArgument {
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

            void ProcessLibraryRules(JsonLibrary lib, MojangMainJson.JsonLibrary lib2) {
                if (lib2.Rules == null) return;
                var list1 = new List<string>();
                var list2 = new List<string>();
                foreach (var rule in lib2.Rules) {
                    switch (rule.Action) {
                        case MojangMainJson.JsonAction.allow:
                            if (rule.Os != null) {
                                if (rule.Os.Name != null)
                                    list1.Add($"os-name:{rule.Os.Name}");
                                if (rule.Os.Version != null)
                                    list1.Add($"os-version:{rule.Os.Version}");
                            }
                            break;
                        case MojangMainJson.JsonAction.disallow:
                            if (rule.Os != null) {
                                if (rule.Os.Name != null)
                                    list2.Add($"os-name:{rule.Os.Name}");
                                if (rule.Os.Version != null)
                                    list2.Add($"os-version:{rule.Os.Version}");
                            }
                            break;
                    }
                }
                lib.Allow = list1.ToArray();
                lib.Disallow = list2.ToArray();
            }

            json.Arguments.Game = gameArguments.ToArray();
            json.Arguments.Java = jvmArguments.ToArray();
            var libraries = new List<JsonLibrary>();
            foreach (var lib in mojang.Libraries) {
                var split = lib.Name.Split(':');
                var main = new JsonLibrary {
                    Allow = Array.Empty<string>(),
                    Disallow = Array.Empty<string>(),
                    Package = split[0],
                    Name = split[1],
                    Version = split[2],
                    Platform = "any",
                    Path = lib.Downloads.Artifact.Path,
                    Size = lib.Downloads.Artifact.Size,
                    ShaHash = lib.Downloads.Artifact.ShaHash,
                    Url = lib.Downloads.Artifact.Url,
                    Exclude = Array.Empty<string>(),
                    Extract = false
                };
                
                ProcessLibraryRules(main, lib);
                libraries.Add(main);
                if (lib.Downloads.Classifiers != null) {
                    if (lib.Downloads.Classifiers.NativeLinux != null) {
                        var newlib = new JsonLibrary {
                            Allow = Array.Empty<string>(),
                            Disallow = Array.Empty<string>(),
                            Package = split[0],
                            Name = split[1],
                            Version = split[2],
                            Platform = "linux",
                            Path = lib.Downloads.Classifiers.NativeLinux.Path,
                            Size = lib.Downloads.Classifiers.NativeLinux.Size,
                            ShaHash = lib.Downloads.Classifiers.NativeLinux.ShaHash,
                            Url = lib.Downloads.Classifiers.NativeLinux.Url,
                            Exclude = Array.Empty<string>(),
                            Extract = false
                        };
                        ProcessLibraryRules(newlib, lib);
                        libraries.Add(newlib);
                    } 
                    
                    if (lib.Downloads.Classifiers.NativeWindows != null) {
                        var newlib = new JsonLibrary {
                            Allow = Array.Empty<string>(),
                            Disallow = Array.Empty<string>(),
                            Package = split[0],
                            Name = split[1],
                            Version = split[2],
                            Platform = "windows",
                            Path = lib.Downloads.Classifiers.NativeWindows.Path,
                            Size = lib.Downloads.Classifiers.NativeWindows.Size,
                            ShaHash = lib.Downloads.Classifiers.NativeWindows.ShaHash,
                            Url = lib.Downloads.Classifiers.NativeWindows.Url,
                            Exclude = Array.Empty<string>(),
                            Extract = false
                        };
                        ProcessLibraryRules(newlib, lib);
                        libraries.Add(newlib);
                    }
                    
                    if (lib.Downloads.Classifiers.NativeMacOs != null) {
                        var newlib = new JsonLibrary {
                            Allow = Array.Empty<string>(),
                            Disallow = Array.Empty<string>(),
                            Package = split[0],
                            Name = split[1],
                            Version = split[2],
                            Platform = "macos",
                            Path = lib.Downloads.Classifiers.NativeMacOs.Path,
                            Size = lib.Downloads.Classifiers.NativeMacOs.Size,
                            ShaHash = lib.Downloads.Classifiers.NativeMacOs.ShaHash,
                            Url = lib.Downloads.Classifiers.NativeMacOs.Url,
                            Exclude = Array.Empty<string>(),
                            Extract = false
                        };
                        ProcessLibraryRules(newlib, lib);
                        libraries.Add(newlib);
                    } 
                    
                    if (lib.Downloads.Classifiers.NativeOsx != null) {
                        var newlib = new JsonLibrary {
                            Allow = Array.Empty<string>(),
                            Disallow = Array.Empty<string>(),
                            Package = split[0],
                            Name = split[1],
                            Version = split[2],
                            Platform = "osx",
                            Path = lib.Downloads.Classifiers.NativeOsx.Path,
                            Size = lib.Downloads.Classifiers.NativeOsx.Size,
                            ShaHash = lib.Downloads.Classifiers.NativeOsx.ShaHash,
                            Url = lib.Downloads.Classifiers.NativeOsx.Url,
                            Exclude = Array.Empty<string>(),
                            Extract = false
                        };
                        ProcessLibraryRules(newlib, lib);
                        libraries.Add(newlib);
                    }
                }
            }

            json.Libraries = libraries.ToArray();
            json = ProcessLibraries(json);
            return json;
        }
        
        /// <summary>
        /// Converts Blowaunch -> Mojang
        /// </summary>
        /// <param name="mojang">Legacy Mojang JSON</param>
        /// <returns>Blowaunch JSON</returns>
        public static BlowaunchMainJson MojangToBlowaunch(MojangLegacyMainJson mojang)
        {
            var json = new BlowaunchMainJson {
                MainClass = mojang.MainClass,
                Type = mojang.Type,
                Author = "Mojang Studios",
                Information = "Mojang Legacy JSON made to work with Blowaunch",
                JavaMajor = mojang.JavaVersion.Major,
                Arguments = new JsonArguments {
                    Java = new[] { new JsonArgument {
                        Allow = Array.Empty<string>(),
                        Disallow = Array.Empty<string>(),
                        ValueList = Array.Empty<string>(),
                        Value = "-cp"
                    }, new JsonArgument {
                        Allow = Array.Empty<string>(),
                        Disallow = Array.Empty<string>(),
                        ValueList = Array.Empty<string>(),
                        Value = "${classpath}"
                    }, new JsonArgument {
                        Allow = Array.Empty<string>(),
                        Disallow = Array.Empty<string>(),
                        ValueList = Array.Empty<string>(),
                        Value = "-Djava.library.path=${natives_directory}"
                    }},
                    Game = new[] { new JsonArgument {
                        Allow = Array.Empty<string>(),
                        Disallow = Array.Empty<string>(),
                        ValueList = Array.Empty<string>(),
                        Value = mojang.Arguments
                    }}
                },
                Downloads = new JsonDownloads {
                    Client = mojang.Downloads.Client,
                    ClientMappings = mojang.Downloads.ClientMappings,
                    Server = mojang.Downloads.Server,
                    ServerMappings = mojang.Downloads.ServerMappings,
                },
                Logging = new JsonLogging {
                    Argument = mojang.Logging.Client.Argument,
                    Download = mojang.Logging.Client.Download
                },
                Assets = new JsonAssets {
                    Id = mojang.Assets.Id,
                    AssetsSize = mojang.Assets.AssetsSize,
                    ShaHash = mojang.Assets.ShaHash,
                    Size = mojang.Assets.Size,
                    Url = mojang.Assets.Url
                },
                Version = mojang.Version,
                Legacy = true
            };

            void ProcessLibraryRules(JsonLibrary lib, MojangLegacyMainJson.JsonLibrary lib2) {
                if (lib2.Rules == null) return;
                var list1 = new List<string>();
                var list2 = new List<string>();
                foreach (var rule in lib2.Rules) {
                    switch (rule.Action) {
                        case MojangLegacyMainJson.JsonAction.allow:
                            if (rule.Os != null) {
                                if (rule.Os.Name != null)
                                    list1.Add($"os-name:{rule.Os.Name}");
                                if (rule.Os.Version != null)
                                    list1.Add($"os-version:{rule.Os.Version}");
                            }
                            break;
                        case MojangLegacyMainJson.JsonAction.disallow:
                            if (rule.Os != null) {
                                if (rule.Os.Name != null)
                                    list2.Add($"os-name:{rule.Os.Name}");
                                if (rule.Os.Version != null)
                                    list2.Add($"os-version:{rule.Os.Version}");
                            }
                            break;
                    }
                }
                lib.Allow = list1.ToArray();
                lib.Disallow = list2.ToArray();
            }
            
            var libraries = new List<JsonLibrary>();
            foreach (var lib in mojang.Libraries) {
                var split = lib.Name.Split(':');
                var main = new JsonLibrary {
                    Allow = Array.Empty<string>(),
                    Disallow = Array.Empty<string>(),
                    Package = split[0],
                    Name = split[1],
                    Version = split[2],
                    Platform = "any",
                    Path = lib.Downloads.Artifact.Path,
                    Exclude = Array.Empty<string>(),
                    Extract = false
                };

                if (lib.Downloads.Artifact != null) {
                    main.Size = lib.Downloads.Artifact.Size;
                    main.ShaHash = lib.Downloads.Artifact.ShaHash;
                    main.Url = lib.Downloads.Artifact.Url;
                }
                
                ProcessLibraryRules(main, lib);
                libraries.Add(main);
                if (lib.Downloads.Classifiers != null) {
                    if (lib.Downloads.Classifiers.NativeLinux != null) {
                        var newlib = new JsonLibrary {
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

                        if (lib.Extract != null) {
                            newlib.Exclude = lib.Extract.Exclude;
                            newlib.Extract = true;
                        }
                        ProcessLibraryRules(newlib, lib);
                        libraries.Add(newlib);
                    } 
                    
                    if (lib.Downloads.Classifiers.NativeWindows != null) {
                        var newlib = new JsonLibrary {
                            Allow = Array.Empty<string>(),
                            Disallow = Array.Empty<string>(),
                            Package = split[0],
                            Name = split[1],
                            Version = split[2],
                            Platform = "windows",
                            Path = lib.Downloads.Classifiers.NativeWindows.Path,
                            Size = lib.Downloads.Classifiers.NativeWindows.Size,
                            ShaHash = lib.Downloads.Classifiers.NativeWindows.ShaHash,
                            Url = lib.Downloads.Classifiers.NativeWindows.Url,
                            Exclude = Array.Empty<string>(),
                            Extract = false
                        };
                        
                        if (lib.Extract != null) {
                            newlib.Exclude = lib.Extract.Exclude;
                            newlib.Extract = true;
                        }
                        ProcessLibraryRules(newlib, lib);
                        libraries.Add(newlib);
                    }
                    
                    if (lib.Downloads.Classifiers.NativeMacOs != null) {
                        var newlib = new JsonLibrary {
                            Allow = Array.Empty<string>(),
                            Disallow = Array.Empty<string>(),
                            Package = split[0],
                            Name = split[1],
                            Version = split[2],
                            Platform = "macos",
                            Path = lib.Downloads.Classifiers.NativeMacOs.Path,
                            Size = lib.Downloads.Classifiers.NativeMacOs.Size,
                            ShaHash = lib.Downloads.Classifiers.NativeMacOs.ShaHash,
                            Url = lib.Downloads.Classifiers.NativeMacOs.Url,
                            Exclude = Array.Empty<string>(),
                            Extract = false
                        };
                        
                        if (lib.Extract != null) {
                            newlib.Exclude = lib.Extract.Exclude;
                            newlib.Extract = true;
                        }
                        ProcessLibraryRules(newlib, lib);
                        libraries.Add(newlib);
                    } 
                    
                    if (lib.Downloads.Classifiers.NativeOsx != null) {
                        var newlib = new JsonLibrary {
                            Allow = Array.Empty<string>(),
                            Disallow = Array.Empty<string>(),
                            Package = split[0],
                            Name = split[1],
                            Version = split[2],
                            Platform = "osx",
                            Path = lib.Downloads.Classifiers.NativeOsx.Path,
                            Size = lib.Downloads.Classifiers.NativeOsx.Size,
                            ShaHash = lib.Downloads.Classifiers.NativeOsx.ShaHash,
                            Url = lib.Downloads.Classifiers.NativeOsx.Url,
                            Exclude = Array.Empty<string>(),
                            Extract = false
                        };
                        
                        if (lib.Extract != null) {
                            newlib.Exclude = lib.Extract.Exclude;
                            newlib.Extract = true;
                        }
                        ProcessLibraryRules(newlib, lib);
                        libraries.Add(newlib);
                    }
                }
            }

            json.Libraries = libraries.ToArray();
            json = ProcessLibraries(json);
            return json;
        }
        
        /// <summary>
        /// Converts Blowaunch -> Mojang
        /// </summary>
        /// <param name="mojang">Legacy Mojang JSON</param>
        /// <returns>Blowaunch JSON</returns>
        public static BlowaunchMainJson MojangToBlowaunchPartial(MojangLegacyMainJson mojang)
        {
            var json = new BlowaunchMainJson {
                MainClass = mojang.MainClass,
                Author = "Mojang Studios",
                Information = "Mojang Legacy JSON made to work with Blowaunch",
                Arguments = new JsonArguments {
                    Game = new[] { new JsonArgument {
                        Allow = Array.Empty<string>(),
                        Disallow = Array.Empty<string>(),
                        ValueList = Array.Empty<string>(),
                        Value = mojang.Arguments
                    }}
                },
                Version = mojang.Version,
                Legacy = true
            };

            void ProcessLibraryRules(JsonLibrary lib, MojangLegacyMainJson.JsonLibrary lib2) {
                if (lib2.Rules == null) return;
                var list1 = new List<string>();
                var list2 = new List<string>();
                foreach (var rule in lib2.Rules) {
                    switch (rule.Action) {
                        case MojangLegacyMainJson.JsonAction.allow:
                            if (rule.Os != null) {
                                if (rule.Os.Name != null)
                                    list1.Add($"os-name:{rule.Os.Name}");
                                if (rule.Os.Version != null)
                                    list1.Add($"os-version:{rule.Os.Version}");
                            }
                            break;
                        case MojangLegacyMainJson.JsonAction.disallow:
                            if (rule.Os != null) {
                                if (rule.Os.Name != null)
                                    list2.Add($"os-name:{rule.Os.Name}");
                                if (rule.Os.Version != null)
                                    list2.Add($"os-version:{rule.Os.Version}");
                            }
                            break;
                    }
                }
                lib.Allow = list1.ToArray();
                lib.Disallow = list2.ToArray();
            }
            
            var libraries = new List<JsonLibrary>();
            foreach (var lib in mojang.Libraries) {
                var split = lib.Name.Split(':');
                var main = new JsonLibrary {
                    Allow = Array.Empty<string>(),
                    Disallow = Array.Empty<string>(),
                    Package = split[0],
                    Name = split[1],
                    Version = split[2],
                    Platform = "any",
                    Exclude = Array.Empty<string>(),
                    Extract = false
                };

                if (lib.Downloads.Artifact != null) {
                    main.Size = lib.Downloads.Artifact.Size;
                    main.ShaHash = lib.Downloads.Artifact.ShaHash;
                    main.Url = lib.Downloads.Artifact.Url;
                    main.Path = lib.Downloads.Artifact.Path;
                }
                
                ProcessLibraryRules(main, lib);
                libraries.Add(main);
                if (lib.Downloads.Classifiers != null) {
                    if (lib.Downloads.Classifiers.NativeLinux != null) {
                        var newlib = new JsonLibrary {
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

                        if (lib.Extract != null) {
                            newlib.Exclude = lib.Extract.Exclude;
                            newlib.Extract = true;
                        }
                        ProcessLibraryRules(newlib, lib);
                        libraries.Add(newlib);
                    } 
                    
                    if (lib.Downloads.Classifiers.NativeWindows != null) {
                        var newlib = new JsonLibrary {
                            Allow = Array.Empty<string>(),
                            Disallow = Array.Empty<string>(),
                            Package = split[0],
                            Name = split[1],
                            Version = split[2],
                            Platform = "windows",
                            Size = lib.Downloads.Classifiers.NativeWindows.Size,
                            ShaHash = lib.Downloads.Classifiers.NativeWindows.ShaHash,
                            Url = lib.Downloads.Classifiers.NativeWindows.Url,
                            Exclude = Array.Empty<string>(),
                            Extract = false
                        };
                        
                        if (lib.Extract != null) {
                            newlib.Exclude = lib.Extract.Exclude;
                            newlib.Extract = true;
                        }
                        ProcessLibraryRules(newlib, lib);
                        libraries.Add(newlib);
                    }
                    
                    if (lib.Downloads.Classifiers.NativeMacOs != null) {
                        var newlib = new JsonLibrary {
                            Allow = Array.Empty<string>(),
                            Disallow = Array.Empty<string>(),
                            Package = split[0],
                            Name = split[1],
                            Version = split[2],
                            Platform = "macos",
                            Size = lib.Downloads.Classifiers.NativeMacOs.Size,
                            ShaHash = lib.Downloads.Classifiers.NativeMacOs.ShaHash,
                            Url = lib.Downloads.Classifiers.NativeMacOs.Url,
                            Exclude = Array.Empty<string>(),
                            Extract = false
                        };
                        
                        if (lib.Extract != null) {
                            newlib.Exclude = lib.Extract.Exclude;
                            newlib.Extract = true;
                        }
                        ProcessLibraryRules(newlib, lib);
                        libraries.Add(newlib);
                    } 
                    
                    if (lib.Downloads.Classifiers.NativeOsx != null) {
                        var newlib = new JsonLibrary {
                            Allow = Array.Empty<string>(),
                            Disallow = Array.Empty<string>(),
                            Package = split[0],
                            Name = split[1],
                            Version = split[2],
                            Platform = "osx",
                            Size = lib.Downloads.Classifiers.NativeOsx.Size,
                            ShaHash = lib.Downloads.Classifiers.NativeOsx.ShaHash,
                            Url = lib.Downloads.Classifiers.NativeOsx.Url,
                            Exclude = Array.Empty<string>(),
                            Extract = false
                        };
                        
                        if (lib.Extract != null) {
                            newlib.Exclude = lib.Extract.Exclude;
                            newlib.Extract = true;
                        }
                        ProcessLibraryRules(newlib, lib);
                        libraries.Add(newlib);
                    }
                }
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
            [JsonProperty("size")] public int? Size;
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
            [JsonProperty("path")] public string Path;
            [JsonProperty("version")] public string Version;
            [JsonProperty("sha1")] public string ShaHash;
            [JsonProperty("size")] public int? Size;
            [JsonProperty("url")] public string Url;
            [JsonProperty("allow")] public string[] Allow;
            [JsonProperty("disallow")] public string[] Disallow;
            [JsonProperty("extract")] public bool Extract;
            [JsonProperty("exclude")] public string[] Exclude;
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
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public enum JsonType
        {
            snapshot,
            release,
            old_alpha,
            old_beta
        }
        
        [JsonProperty("version")] public string Version;
        [JsonProperty("author")] public string Author;
        [JsonProperty("info")] public string Information;
        [JsonProperty("java")] public int JavaMajor;
        [JsonProperty("legacy")] public bool Legacy;
        [JsonProperty("args")] public JsonArguments Arguments;
        [JsonProperty("assets")] public JsonAssets Assets;
        [JsonProperty("libraries")] public JsonLibrary[] Libraries;
        [JsonProperty("downloads")] public JsonDownloads Downloads;
        [JsonProperty("logging")] public JsonLogging Logging;
        [JsonProperty("mainClass")] public string MainClass;
        [JsonProperty("type")] public JsonType Type;
    }
}