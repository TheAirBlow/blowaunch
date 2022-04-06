// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using Blowaunch.Library.Exceptions;
using Microsoft.CSharp.RuntimeBinder;

namespace Blowaunch.Library.Authentication;

/// <summary>
/// Mojang account authentication
/// </summary>
public static class Mojang
{
    /// <summary>
    /// Login into Mojang account
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="password">Password</param>
    /// <returns>Newly created Account instance</returns>
    public static Account Login(string username, string password)
    {
        var account = new Account {
            ClientToken = Guid.NewGuid().ToString(),
            Type = Account.AuthType.Mojang
        };

        var response = Helper.Post(Endpoints.MojangServer,
            Endpoints.Mojang.Login, 
            new Dictionary<string, string>(), 
            $@"
{{
    ""agent"": {{
        ""name"": ""Minecraft"",
        ""version"": 1
    }},
    ""username"": ""{username}"",
    ""password"": ""{password}"",
    ""clientToken"": ""{account.ClientToken}""
}}
", new MediaTypeHeaderValue("application/json"));
        var json = response.GetDynamic();
        HandleErrors(json);
        account.Name = json.selectedProfile.name;
        account.Uuid = json.selectedProfile.id;
        account.AccessToken = json.accessToken;
        return account;
    }

    /// <summary>
    /// Refresh access token
    /// </summary>
    /// <param name="account">Account</param>
    public static void Refresh(ref Account account)
    {
        if (account.Type != Account.AuthType.Mojang)
            throw new InvalidOperationException(
                "Invalid account type!");
        
        var response = Helper.Post(Endpoints.MojangServer,
            Endpoints.Mojang.Refresh, 
            new Dictionary<string, string>(), 
            $@"
{{
    ""accessToken"": ""{account.AccessToken}"",
    ""clientToken"": ""{account.ClientToken}"",
    ""selectedProfile"": {{
        ""id"": ""{account.Uuid}"",
        ""name"": ""{account.Name}""
    }}
}}
", new MediaTypeHeaderValue("application/json"));
        var json = response.GetDynamic(); 
        HandleErrors(json);
        account.AccessToken = json.accessToken;
    }
    
    /// <summary>
    /// Validate access token
    /// </summary>
    /// <param name="account">Account</param>
    public static bool Validate(Account account)
    {
        if (account.Type != Account.AuthType.Mojang)
            throw new InvalidOperationException(
                "Invalid account type!");
        
        var response = Helper.Post(Endpoints.MojangServer,
            Endpoints.Mojang.Validate, 
            new Dictionary<string, string>(),
            $@"
{{
    ""accessToken"": ""{account.AccessToken}"",
    ""clientToken"": ""{account.ClientToken}""
}}
", new MediaTypeHeaderValue("application/json"));
        var json = response.GetDynamic(); 
        try { HandleErrors(json); }
        catch { return false; }

        return response.StatusCode == HttpStatusCode.NoContent;
    }
    
    /// <summary>
    /// Invalidate access token
    /// </summary>
    /// <param name="account">Account</param>
    public static bool Invalidate(Account account)
    {
        if (account.Type != Account.AuthType.Mojang)
            throw new InvalidOperationException(
                "Invalid account type!");
        
        var response = Helper.Post(Endpoints.MojangServer,
            Endpoints.Mojang.Invalidate, 
            new Dictionary<string, string>(), 
            $@"
{{
    ""accessToken"": ""{account.AccessToken}"",
    ""clientToken"": ""{account.ClientToken}""
}}
", new MediaTypeHeaderValue("application/json"));
        
        return string.IsNullOrEmpty(response.GetContent());
    }

    /// <summary>
    /// Handle errors
    /// </summary>
    /// <param name="json">Dynamic JSON</param>
    private static void HandleErrors(dynamic json)
    {
        try {
            switch ((string)json.error) {
                case "ForbiddenOperationException":
                    throw (json.errorMessage as string) switch {
                        "Token does not exist." => new SessionInvalidException(
                            "Access token is no longer " +
                            "valid and cannot be refreshed!"),
                        "Forbidden" => new AuthenticationException(
                            "Username or password are empty " +
                            "or the password is less than 3 chars."),
                        _ => new AuthenticationException((string)json.errorMessage)
                    };
                case "ResourceException":
                case "GoneException":
                    throw new AuthenticationException(
                        "Account got migrated to Microsoft!");
                default:
                    throw new AuthenticationException(
                        $"An unknown error occured: {json.error}");
            }
        } catch (RuntimeBinderException ex) { Console.WriteLine(ex); }
    }
}