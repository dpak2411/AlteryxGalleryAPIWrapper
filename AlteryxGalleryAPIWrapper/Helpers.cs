using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

namespace AlteryxGalleryAPIWrapper
{
    internal interface IHelpers
    {
        IDictionary<string, object> CallServer(Uri url, IDictionary<string, object> parameters, ICredentials netCreds);
        HttpWebResponse GetWebResponse(Uri uri, ICredentials netCreds = null);
        HttpWebRequest GetWebRequest(Uri uri, ICredentials netCreds = null);
    }

    class Helpers : IHelpers
    {
        /// <summary>
        /// Utility method to post data to the server.  Lifted from Gary Schwartz's HandlerSamples.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="parameters"></param>
        /// <param name="netCreds"> </param>
        /// <returns></returns>
        public IDictionary<string, object> CallServer(Uri url, IDictionary<string, object> parameters, ICredentials netCreds)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            if (netCreds != null) request.Credentials = netCreds;
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";

            //append the parameters to a string
            var sb = new StringBuilder();
            if (parameters != null)
            {
                var first = true;
                foreach (var key in parameters.Keys)
                {
                    if (!first) sb.Append("&");
                    sb.Append(key);
                    sb.Append("=");
                    sb.Append(parameters[key].ToString());
                    first = false;
                }
            }
            var postData = sb.ToString();
            request.ContentLength = postData.Length;

            //write the parameter string into the request
            var sw = new StreamWriter(request.GetRequestStream());
            sw.Write(postData);
            sw.Close();

            //call the server and retrieve the response
            var response = request.GetResponse();
            var sr = new StreamReader(response.GetResponseStream());
            var json = sr.ReadToEnd();
            sr.Close();

            //deserialize the response and return the results
            var serializer = new JavaScriptSerializer();
            var start = json.IndexOf('{');
            var end = json.LastIndexOf('}');
            json = json.Substring(start, end - start + 1);
            return (IDictionary<string, object>)serializer.DeserializeObject(json);
        }

        /// <summary>
        /// Performs a simple get request
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="netCreds"> </param>
        /// <returns>An HttpWebResponse object.</returns>
        public HttpWebResponse GetWebResponse(Uri uri, ICredentials netCreds = null)
        {
            HttpWebResponse response = null;
            var request = GetWebRequest(uri, netCreds);
            if (request != null)
                response = request.GetResponse() as HttpWebResponse;
            return response;
        }

        public HttpWebRequest GetWebRequest(Uri uri, ICredentials netCreds = null)
        {
            var request = WebRequest.Create(uri) as HttpWebRequest;
            request.Credentials = netCreds ?? CredentialCache.DefaultNetworkCredentials;
            request.Proxy = WebRequest.DefaultWebProxy;
            if (request.Proxy != null)
                request.Proxy.Credentials = netCreds ?? CredentialCache.DefaultNetworkCredentials;
            request.CookieContainer = new CookieContainer();
            return request;
        }

        /// <summary>
        /// A generic SHA1 hasher used for hashing the password.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        internal static string GetHash(string s)
        {
            var hashAlgorithm = SHA1.Create();
            var data = hashAlgorithm.ComputeHash(Encoding.ASCII.GetBytes(s));
            return BitConverter.ToString(data).Replace("-", "").ToLower();
        }

        internal static class EventLogger
        {
            private const string LogSource = "Alteryx Web AppPublishMediator";

            public static void LogError(string message)
            {
                if (!EventLog.SourceExists(LogSource))
                {
                    EventLog.CreateEventSource(LogSource, "Application");
                    Thread.Sleep(1000);
                }
                var myLog = new EventLog { Source = LogSource };
                myLog.WriteEntry(message, EventLogEntryType.Error);
            }
        }

        internal static NetworkCredential WindowsCredential(string username, string password)
        {
            if (username.Contains("\\"))
            {
                var credPair = username.Split('\\');
                return new NetworkCredential
                {
                    Domain = credPair.Length > 1 ? credPair[0] : string.Empty,
                    UserName = credPair.Length > 1 ? credPair[1] : username,
                    Password = password
                };
            }

            return new NetworkCredential
            {
                UserName = username,
                Password = password
            };
        }
    }

    public class UploadEventArgs : EventArgs
    {
        public string FileId { get; set; }
        public string ServerGuid { get; set; }
        public long PercentComplete { get; set; }
        public string Message { get; set; }
        public bool Cancelled { get; set; }

        public UploadEventArgs(string fileId, string serverGuid, long percentComplete, string messageData)
        {
            FileId = fileId;
            ServerGuid = serverGuid;
            PercentComplete = percentComplete;
            Message = messageData;
            Cancelled = false;
        }
    }

    public class AuthenticationFailureException : Exception
    {
        public AuthenticationFailureException() { }
        public AuthenticationFailureException(string message) : base(message) { }
    }

    public class ResetPasswordException : Exception
    {
        public ResetPasswordException() { }
        public ResetPasswordException(string message) : base(message) { }
    }
}
