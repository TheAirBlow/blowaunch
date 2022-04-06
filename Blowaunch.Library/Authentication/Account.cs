// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using Newtonsoft.Json;

namespace Blowaunch.Library.Authentication;

/// <summary>
/// An account
/// </summary>
public class Account
{
    /// <summary>
    /// Authentication type
    /// </summary>
    public enum AuthType
    {
        Microsoft = 0,
        Mojang = 1,
        None = 2
    }

    [JsonProperty("validUntil")] public DateTime ValidUntil;
    [JsonProperty("refreshToken")] public string RefreshToken = "";
    [JsonProperty("type")] public AuthType Type = AuthType.None;
    [JsonProperty("accessToken")] public string AccessToken = "";
    [JsonProperty("clientToken")] public string ClientToken = "";
    [JsonProperty("xuid")] public string Xuid = "";
    [JsonProperty("name")] public string Name { get; set; }
    [JsonProperty("uuid")] public string Uuid = "";
    [JsonProperty("id")] public string Id = "";
}