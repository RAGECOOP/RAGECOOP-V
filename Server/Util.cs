global using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Xml;
using System.Xml.Serialization;
using Lidgren.Network;

namespace RageCoop.Server;

internal static class Util
{
    public static string DownloadString(string url)
    {
        try
        {
            // TLS only
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls13 | SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            HttpClient client = new();
            HttpRequestMessage request = new(HttpMethod.Get, url);
            var response = client.Send(request);
            using var reader = new StreamReader(response.Content.ReadAsStream());
            var responseBody = reader.ReadToEnd();

            return responseBody;
        }
        catch
        {
            return "";
        }
    }

    public static List<NetConnection> Exclude(this IEnumerable<NetConnection> connections, NetConnection toExclude)
    {
        return new List<NetConnection>(connections.Where(e => e != toExclude));
    }

    public static T Read<T>(string file) where T : new()
    {
        XmlSerializer ser = new(typeof(T));

        XmlWriterSettings settings = new()
        {
            Indent = true,
            IndentChars = "\t",
            OmitXmlDeclaration = true
        };

        var path = AppContext.BaseDirectory + file;
        T data;

        if (File.Exists(path))
            try
            {
                using (var stream = XmlReader.Create(path))
                {
                    data = (T)ser.Deserialize(stream);
                }

                using (var stream = XmlWriter.Create(path, settings))
                {
                    ser.Serialize(stream, data);
                }
            }
            catch
            {
                using (var stream = XmlWriter.Create(path, settings))
                {
                    ser.Serialize(stream, data = new T());
                }
            }
        else
            using (var stream = XmlWriter.Create(path, settings))
            {
                ser.Serialize(stream, data = new T());
            }

        return data;
    }

    public static T Next<T>(this T[] values)
    {
        return values[new Random().Next(values.Length - 1)];
    }

    public static string GetFinalRedirect(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return url;

        var maxRedirCount = 8; // prevent infinite loops
        var newUrl = url;
        do
        {
            try
            {
                HttpClientHandler handler = new()
                {
                    AllowAutoRedirect = false
                };
                HttpClient client = new(handler);
                HttpRequestMessage request = new(HttpMethod.Head, url);
                var response = client.Send(request);

                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        return newUrl;
                    case HttpStatusCode.Redirect:
                    case HttpStatusCode.MovedPermanently:
                    case HttpStatusCode.RedirectKeepVerb:
                    case HttpStatusCode.RedirectMethod:
                        newUrl = response.Headers.Location.ToString();
                        if (newUrl == null)
                            return url;

                        var newUrlString = newUrl;

                        if (!newUrlString.Contains("://"))
                        {
                            // Doesn't have a URL Schema, meaning it's a relative or absolute URL
                            var u = new Uri(new Uri(url), newUrl);
                            newUrl = u.ToString();
                        }

                        break;
                    default:
                        return newUrl;
                }

                url = newUrl;
            }
            catch (WebException)
            {
                // Return the last known good URL
                return newUrl;
            }
            catch
            {
                return null;
            }
        } while (maxRedirCount-- > 0);

        return newUrl;
    }
}