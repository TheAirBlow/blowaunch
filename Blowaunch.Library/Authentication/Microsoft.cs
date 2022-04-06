// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using Blowaunch.Library.Exceptions;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json.Linq;

namespace Blowaunch.Library.Authentication;

/// <summary>
/// Microsoft account authentication
/// </summary>
public static class Microsoft
{
    /// <summary>
    /// HTTP server
    /// </summary>
    private static HttpListener _listener;

    /// <summary>
    /// Account created
    /// </summary>
    private static Account _account;

    /// <summary>
    /// Open login page
    /// </summary>
    public static void OpenLoginPage()
        => Process.Start(new ProcessStartInfo {
            FileName = Endpoints.BlowaunchServer + "/"
                       + Endpoints.Microsoft.LoginBrowser,
            UseShellExecute = true
        });

    public static void StopListener() 
    {
        
    }

    /// <summary>
    /// Start listener
    /// </summary>
    public static void StartListener(Action<Account> success, 
        Action<Exception> exception, Action<string, int> progress)
    {
        progress("Waiting for you to log in...", -1);
        _listener = new HttpListener();
        _listener.Prefixes.Add("http://localhost:5897/");
        _listener.Start();
        // ReSharper disable once AsyncVoidLambda
        new Thread(async () => {
            while (true) {
                var ctx = await _listener.GetContextAsync();
                var req = ctx.Request;
                var resp = ctx.Response;
                var data = Encoding.UTF8.GetBytes(
                    "<b>Open Blowauncher!</b>");
                resp.ContentType = "text/html";
                resp.ContentEncoding = Encoding.UTF8;
                resp.ContentLength64 = data.LongLength;
                await resp.OutputStream.WriteAsync(data, 0, data.Length);
                if (req.QueryString.AllKeys.Any(x => x == "data")) {
                    try {
                        var decoded = Convert.FromBase64String(req.QueryString["data"]!);
                        var utf8 = Encoding.UTF8.GetString(decoded);
                        dynamic json = JObject.Parse(utf8);
                        Login((string)json.access_token, (string)json.refresh_token,
                            DateTime.Now + TimeSpan.FromSeconds(
                                (int)json.expires_in), progress);
                        success(_account);
                    } catch (Exception e) {
                        exception(e);
                    }
                }
                resp.Close(); _listener.Stop(); break;
            }
        }).Start();
    }
    
    /// <summary>
    /// Refresh access token
    /// </summary>
    /// <param name="account">Account</param>
    public static void Refresh(ref Account account)
    {
        if (account.Type != Account.AuthType.Microsoft)
            throw new InvalidOperationException(
                "Invalid account type!");
        
        var response = Helper.Post(Endpoints.BlowaunchServer,
            Endpoints.Microsoft.Refresh +
            $"?token={account.RefreshToken}", 
            new Dictionary<string, string>());
        var json = response.GetDynamic();
        account.AccessToken = json.accessToken;
    }

    /// <summary>
    /// Do the login process
    /// </summary>
    /// <param name="json">Dynamic JSON</param>
    private static void Login(string accessToken, 
        string refreshToken, DateTime validUntil,
        Action<string, int> progress)
    {
        _account = new Account {
            Type = Account.AuthType.Microsoft,
            RefreshToken = refreshToken,
            ValidUntil = validUntil
        };
        
        progress("Logging in into Xbox Live...", 0);
        var xboxResponse = Helper.Post(Endpoints.XboxAuthServer,
            Endpoints.Microsoft.XboxLiveAuth, 
            new Dictionary<string, string> {
                {"Accept", "application/json"}
            }, $@"
{{
    ""Properties"": {{
        ""AuthMethod"": ""RPS"",
        ""SiteName"": ""user.auth.xboxlive.com"",
        ""RpsTicket"": ""d={accessToken}""
    }},
    ""RelyingParty"": ""http://auth.xboxlive.com"",
    ""TokenType"": ""JWT""
}}
", new MediaTypeHeaderValue("application/json"));
        var xboxJson = xboxResponse.GetDynamic();
        
        progress("Logging in into XSTS...", 1);
        var xstsResponse = Helper.Post(Endpoints.XboxXstsServer,
            Endpoints.Microsoft.XboxXstsAuth, 
            new Dictionary<string, string> {
                {"Accept", "application/json"}
            }, $@"
{{
    ""Properties"": {{
        ""SandboxId"": ""RETAIL"",
        ""UserTokens"": [ ""{xboxJson.Token}"" ]
    }},
    ""RelyingParty"": ""rp://api.minecraftservices.com/"",
    ""TokenType"": ""JWT""
}}
", new MediaTypeHeaderValue("application/json"));
        var xstsJson = xstsResponse.GetDynamic();
        try {
            switch ((uint)xstsJson.XErr) {
                case 2148916233:
                    throw new AuthenticationException(
                        "You don't have an Xbox Live account.");
                case 2148916235:
                    throw new AuthenticationException(
                        "Xbox Live is not available in your region/country.");
                case 2148916236:
                case 2148916237:
                    throw new AuthenticationException(
                        "You need to pass adult verification.");
                case 2148916238:
                    throw new AuthenticationException(
                        "You need to be added to a Family " +
                        "by an adult, because you're under 18.");
                default:
                    throw new AuthenticationException(
                        $"Unknown error occured: {xstsJson.XErr}!");
            }
        } catch (RuntimeBinderException) { }
        if (xboxJson.DisplayClaims.xui[0].uhs !=
            xstsJson.DisplayClaims.xui[0].uhs)
            throw new AuthenticationException(
                "Userhashes do not match!");
        
        progress("Logging in into Minecraft...", 2);
        var minecraftResponse = Helper.Post(Endpoints.MinecraftServer,
            Endpoints.Microsoft.MinecraftAuth, 
            new Dictionary<string, string> {
                {"Accept", "application/json"}
            }, $@"
{{
    ""identityToken"": ""XBL3.0 x={xboxJson.DisplayClaims.xui[0].uhs};{xstsJson.Token}""
}}
", new MediaTypeHeaderValue("application/json"));
        if (!minecraftResponse.IsSuccessStatusCode)
            throw new AuthenticationException("Unable to login into Minecraft: " +
                                              $"{(int)minecraftResponse.StatusCode} " +
                                              $"status code");
        var minecraftJson = minecraftResponse.GetDynamic();
        _account.AccessToken = minecraftJson.access_token;
        _account.Xuid = minecraftJson.username;
        
        progress("Getting profile information...", 3);
        var profileResponse = Helper.Get(Endpoints.MinecraftServer,
            Endpoints.Microsoft.MinecraftProfile, 
            new Dictionary<string, string> {
                {"Authorization", $"Bearer {_account.AccessToken}"},
                {"Accept", "application/json"}
            });
        if (profileResponse.StatusCode == HttpStatusCode.NotFound)
            throw new AuthenticationException(
                "You do not own a Minecraft copy!");
        if (!profileResponse.IsSuccessStatusCode)
            throw new AuthenticationException("Unable to get profile: " +
                                              $"{(int)profileResponse.StatusCode} " +
                                              $"status code");
        var profileJson = profileResponse.GetDynamic();
        _account.Name = profileJson.name;
        _account.Uuid = profileJson.id;
        
        progress("Finished!", 4);
    }
}