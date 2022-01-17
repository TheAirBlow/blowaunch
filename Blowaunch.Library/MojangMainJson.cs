using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Blowaunch.Library
{
    /// <summary>
    /// Mojang - Main JSON
    /// </summary>
    public class MojangMainJson
    {
        /// <summary>
        /// Mojang Main JSON - Rule Action
        /// </summary>
        public enum JsonAction
        {
            [JsonProperty("allow")] Allow,
            [JsonProperty("disallow")] Disallow
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
        /// Mojang Main JSON - Non-string Argument
        /// </summary>
        public class JsonNonStringArgument
        {
            // Mojang, I hate you for this shitty JSON.
            // Could you just make different names for array and string?
            [JsonProperty("value")] public object Value;
            [JsonProperty("rules")] public JsonRule[] Rules;
        }

        /// <summary>
        /// Mojang Main JSON - Arguments
        /// </summary>
        public class JsonArguments
        {
            // Mojang, fuck off.
            // Just use different names!
            [JsonProperty("game")] public object[] Game;
            [JsonProperty("jvm")] public object[] Java;
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
        /// Mojang Main JSON - Library
        /// </summary>
        public class JsonLibrary
        {
            [JsonProperty("downloads")] public JsonLibraryDownloads Downloads;
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
        /// Mojang Main JSON - Logging
        /// </summary>
        public class JsonLogging
        {
            [JsonProperty("argument")] public string Argument;
            [JsonProperty("file")] public BlowaunchMainJson.JsonDownload Download;
        }
        
        [JsonProperty("type")] public BlowaunchMainJson.JsonType Type;
        [JsonProperty("libraries")] public JsonLibrary[] Libraries;
        [JsonProperty("downloads")] public JsonDownloads Downloads;
        [JsonProperty("javaVersion")] public JsonJava JavaVersion;
        [JsonProperty("arguments")] public JsonArguments Arguments;
        [JsonProperty("assetIndex")] public JsonAssets Assets;
        [JsonProperty("logging")] public JsonLogging Logging;
        [JsonProperty("mainClass")] public string MainClass;
        [JsonProperty("version")] public string Version;
    }
}