using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Blowaunch.Library
{
    /// <summary>
    /// Mojang - Main JSON
    /// </summary>
    public class MojangLegacyMainJson
    {
        /// <summary>
        /// Mojang Main JSON - Rule Action
        /// </summary>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public enum JsonAction
        {
            allow,
            disallow
        }
        
        /// <summary>
        /// Mojang Main JSON - Rule
        /// </summary>
        public class JsonRule
        {
            [JsonProperty("action")] public JsonAction Action;
            [JsonProperty("features")] public Dictionary<string, bool> Features;
            [JsonProperty("os")] public JsonLibraryRuleOs Os;
        }

        /// <summary>
        /// Mojang Main JSON - Assets
        /// </summary>
        public class JsonAssets
        {
            [JsonProperty("id")] public string Id;
            [JsonProperty("sha1")] public string ShaHash;
            [JsonProperty("size")] public int Size;
            [JsonProperty("totalSize")] public int AssetsSize;
            [JsonProperty("url")] public string Url;
        }

        /// <summary>
        /// Mojang Main JSON - Library download
        /// </summary>
        public class JsonLibraryDownload
        {
            [JsonProperty("path")] public string Path;
            [JsonProperty("sha1")] public string ShaHash;
            [JsonProperty("size")] public int Size;
            [JsonProperty("url")] public string Url;
        }

        /// <summary>
        /// Mojang Main JSON - Library classifiers
        /// </summary>
        public class JsonClassifiers
        {
            [JsonProperty("javadoc")] public JsonLibraryDownload JavaDoc;
            [JsonProperty("natives-linux")] public JsonLibraryDownload NativeLinux;
            [JsonProperty("natives-osx")] public JsonLibraryDownload NativeOsx;
            [JsonProperty("natives-macos")] public JsonLibraryDownload NativeMacOs;
            [JsonProperty("natives-windows")] public JsonLibraryDownload NativeWindows;
            [JsonProperty("sources")] public JsonLibraryDownload Sources;
        }

        /// <summary>
        /// Mojang Main JSON - Library downloads
        /// </summary>
        public class JsonLibraryDownloads
        {
            [JsonProperty("classifiers")] public JsonClassifiers Classifiers;
            [JsonProperty("artifact")] public JsonLibraryDownload Artifact;
        }

        /// <summary>
        /// Mojang Main JSON - Library rule OS
        /// </summary>
        public class JsonLibraryRuleOs
        {
            [JsonProperty("name")] public string Name;
            [JsonProperty("version")] public string Version;
        }

        /// <summary>
        /// Mojang Main JSON - Library rule
        /// </summary>
        public class JsonLibraryRule
        {
            [JsonProperty("action")] public JsonAction Action;
            [JsonProperty("os")] public JsonLibraryRuleOs Os;
        }

        /// <summary>
        /// Mojang Main JSON - Extract JAR
        /// </summary>
        public class JsonExtract
        {
            [JsonProperty("exclude")] public string[] Exclude;
        }

        /// <summary>
        /// Mojang Main JSON - Library
        /// </summary>
        public class JsonLibrary
        {
            [JsonProperty("downloads")] public JsonLibraryDownloads Downloads;
            [JsonProperty("extract")] public JsonExtract Extract;
            [JsonProperty("rules")] public JsonLibraryRule[] Rules;
            [JsonProperty("natives")] public Dictionary<string, string> Natives;
            [JsonProperty("name")] public string Name;
        }

        /// <summary>
        /// Mojang Main JSON - Downloads
        /// </summary>
        public class JsonDownloads
        {
            [JsonProperty("client")] public BlowaunchMainJson.JsonDownload Client;
            [JsonProperty("client-mappings")] public BlowaunchMainJson.JsonDownload ClientMappings;
            [JsonProperty("server")] public BlowaunchMainJson.JsonDownload Server;
            [JsonProperty("server-mappings")] public BlowaunchMainJson.JsonDownload ServerMappings;
        }

        /// <summary>
        /// Mojang Main JSON - Java version
        /// </summary>
        public class JsonJava
        {
            [JsonProperty("component")] public string Component;
            [JsonProperty("majorVersion")] public int Major;
        }

        /// <summary>
        /// Mojang Main JSON - Logging root
        /// </summary>
        public class JsonLogging
        {
            [JsonProperty("client")] public ActualJsonLogging Client;
        }

        /// <summary>
        /// Mojang Main JSON - Client/Server logging
        /// </summary>
        public class ActualJsonLogging
        {
            [JsonProperty("argument")] public string Argument;
            [JsonProperty("file")] public BlowaunchMainJson.JsonDownload Download;
        }
        
        [JsonProperty("type")] public BlowaunchMainJson.JsonType Type;
        [JsonProperty("libraries")] public JsonLibrary[] Libraries;
        [JsonProperty("downloads")] public JsonDownloads Downloads;
        [JsonProperty("javaVersion")] public JsonJava JavaVersion;
        [JsonProperty("minecraftArguments")] public string Arguments;
        [JsonProperty("assetIndex")] public JsonAssets Assets;
        [JsonProperty("logging")] public JsonLogging Logging;
        [JsonProperty("mainClass")] public string MainClass;
        [JsonProperty("id")] public string Version;

        /// <summary>
        /// Is a mojang JSON a legacy one?
        /// </summary>
        /// <param name="json">JSON string</param>
        /// <returns>Boolean value</returns>
        public static bool IsLegacyJson(string json)
        {
            try {
                var main = JsonConvert.DeserializeObject<MojangLegacyMainJson>(json);
                return main?.Arguments != null;
            } catch { return false; }
        }
    }
}