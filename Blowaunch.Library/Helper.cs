// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;

namespace Blowaunch.Library;

/// <summary>
/// Helper class
/// </summary>
public static class Helper
{
    /// <summary>
    /// Send a GET request
    /// </summary>
    /// <param name="server">Server URI</param>
    /// <param name="endpoint">Endpoint</param>
    /// <param name="headers">HTTP Headers</param>
    /// <returns>Response Message</returns>
    public static HttpResponseMessage Get(string server, string endpoint,
        Dictionary<string, string> headers) 
    {
        var client = new HttpClient();
        client.BaseAddress = new Uri(server);
        foreach (var i in headers)
            client.DefaultRequestHeaders.Add(i.Key, i.Value);
        return client.GetAsync(endpoint).Result;
    }

    /// <summary>
    /// Send a POST request
    /// </summary>
    /// <param name="server">Server URI</param>
    /// <param name="endpoint">Endpoint</param>
    /// <param name="headers">HTTP Headers</param>
    /// <param name="body">Request Body</param>
    /// <returns>Response Message</returns>
    public static HttpResponseMessage Post(string server, string endpoint,
        Dictionary<string, string> headers, string body = "",
        MediaTypeHeaderValue contentType = null)
    {
        var client = new HttpClient();
        client.BaseAddress = new Uri(server);
        foreach (var i in headers)
            client.DefaultRequestHeaders.Add(i.Key, i.Value);
        var content = new StringContent(body);
        if (contentType != null)
            content.Headers.ContentType = contentType;
        return client.PostAsync(endpoint, content).Result;
    }
        
    /// <summary>
    /// Send a PUT request
    /// </summary>
    /// <param name="server">Server URI</param>
    /// <param name="endpoint">Endpoint</param>
    /// <param name="headers">HTTP Headers</param>
    /// <param name="body">Request Body</param>
    /// <returns>Response Message</returns>
    public static HttpResponseMessage Put(string server, string endpoint,
        Dictionary<string, string> headers, string body = "",
        MediaTypeHeaderValue contentType = null) 
    {
        var client = new HttpClient();
        client.BaseAddress = new Uri(server);
        foreach (var i in headers)
            client.DefaultRequestHeaders.Add(i.Key, i.Value);
        var content = new StringContent(body);
        if (contentType != null)
            content.Headers.ContentType = contentType;
        return client.PutAsync(endpoint, content).Result;
    }
        
    /// <summary>
    /// Send a DELETE request
    /// </summary>
    /// <param name="server">Server URI</param>
    /// <param name="endpoint">Endpoint</param>
    /// <param name="headers">HTTP Headers</param>
    /// <returns>Response Message</returns>
    public static HttpResponseMessage Delete(string server, string endpoint,
        Dictionary<string, string> headers) 
    {
        var client = new HttpClient();
        client.BaseAddress = new Uri(server);
        foreach (var i in headers)
            client.DefaultRequestHeaders.Add(i.Key, i.Value);
        return client.DeleteAsync(endpoint).Result;
    }

    /// <summary>
    /// Get dynamic object of the response JSON
    /// </summary>
    /// <param name="response">Response Message</param>
    /// <returns>Dynamic object of the response JSON</returns>
    public static dynamic GetDynamic(this HttpResponseMessage response)
        => JObject.Parse(response.GetContent());

    /// <summary>
    /// Get raw response body
    /// </summary>
    /// <param name="response">Response Message</param>
    /// <returns>Raw response body</returns>
    public static string GetContent(this HttpResponseMessage response)
        => response.Content.ReadAsStringAsync().Result;
        
    /// <summary>
    /// Does dynamic have a property?
    /// </summary>
    /// <param name="d">Dynamic</param>
    /// <param name="property">Name</param>
    /// <returns>Boolean value</returns>
    public static bool HasProperty(dynamic d, string property)
    {
        Type type = d.GetType();
        return type.GetProperties().Any(p 
            => p.Name.Equals(property));
    }
}