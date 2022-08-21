using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Threading;
using System.IO;

namespace RageCoop.Core
{
    internal static class HttpHelper
    {
        public static void DownloadFile(string url,string destination,Action<int> progressCallback)
        {
            if (File.Exists(destination)) { File.Delete(destination); }
            AutoResetEvent ae=new AutoResetEvent(false);
            WebClient client = new WebClient();

            // TLS only
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            client.DownloadProgressChanged += (s, e1) => progressCallback?.Invoke(e1.ProgressPercentage);
            client.DownloadFileCompleted += (s, e2) =>
            {
                ae.Set();
            };
            client.DownloadFileAsync(new Uri(url), destination);
            ae.WaitOne();
        }
        public static string DownloadString(string url)
        {
            // TLS only
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            WebClient client = new WebClient();
            return client.DownloadString(url);
        }
    }
}
