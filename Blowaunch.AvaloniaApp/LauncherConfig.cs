// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.Numerics;
using Blowaunch.Library.Authentication;
using Newtonsoft.Json;

namespace Blowaunch.AvaloniaApp;

/// <summary>
/// Launcher configuration
/// </summary>
public class LauncherConfig
{
    public class VersionClass 
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("id")] public string Id { get; set; }
    }
    
    [JsonProperty("windowSize")] public Vector2 WindowSize = new(200, 200); // [+]
    [JsonProperty("selectedAccountId")] public string SelectedAccountId = "";   // [*]
    [JsonProperty("accounts")] public List<Account> Accounts = new();           // [*]
    [JsonProperty("customResolution")] public bool CustomWindowSize;            // [+]
    [JsonProperty("showSnapshots")] public bool ShowSnapshots;                  // [+]
    [JsonProperty("version")] public VersionClass Version;                      // [*]
    [JsonProperty("gameArgs")] public string GameArgs = "";                     // [+]
    [JsonProperty("jvmArgs")] public string JvmArgs = "";                       // [+]
    [JsonProperty("maxRam")] public string RamMax = "2048";                     // [+]
    [JsonProperty("forceOffline")] public bool ForceOffline;                    // [ ]
    [JsonProperty("showAlpha")] public bool ShowAlpha;                          // [+]
    [JsonProperty("showBeta")] public bool ShowBeta;                            // [+]
    [JsonProperty("isDemo")] public bool DemoUser;                              // [ ]
}