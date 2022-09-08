global using System.Collections.Generic;
using Lidgren.Network;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Xml;
using System.Xml.Serialization;
namespace RageCoop.Server
{
    internal static partial class Util
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
                HttpResponseMessage response = client.Send(request);
                using var reader = new StreamReader(response.Content.ReadAsStream());
                string responseBody = reader.ReadToEnd();

                return responseBody;
            }
            catch
            {
                return "";
            }
        }
        public static List<NetConnection> Exclude(this IEnumerable<NetConnection> connections, NetConnection toExclude)
        {
            return new(connections.Where(e => e != toExclude));
        }

        public static T Read<T>(string file) where T : new()
        {
            XmlSerializer ser = new(typeof(T));

            XmlWriterSettings settings = new()
            {
                Indent = true,
                IndentChars = ("\t"),
                OmitXmlDeclaration = true
            };

            string path = AppContext.BaseDirectory + file;
            T data;

            if (File.Exists(path))
            {
                try
                {
                    using (XmlReader stream = XmlReader.Create(path))
                    {
                        data = (T)ser.Deserialize(stream);
                    }

                    using (XmlWriter stream = XmlWriter.Create(path, settings))
                    {
                        ser.Serialize(stream, data);
                    }
                }
                catch
                {
                    using (XmlWriter stream = XmlWriter.Create(path, settings))
                    {
                        ser.Serialize(stream, data = new T());
                    }
                }
            }
            else
            {
                using (XmlWriter stream = XmlWriter.Create(path, settings))
                {
                    ser.Serialize(stream, data = new T());
                }
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

            int maxRedirCount = 8;  // prevent infinite loops
            string newUrl = url;
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
                    HttpResponseMessage response = client.Send(request);

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

                            string newUrlString = newUrl;

                            if (!newUrlString.Contains("://"))
                            {
                                // Doesn't have a URL Schema, meaning it's a relative or absolute URL
                                Uri u = new Uri(new Uri(url), newUrl);
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
}
