using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Web.Script.Serialization;

namespace AlteryxGalleryAPIWrapper
{
    public class Client
    {
        private string url;
        private string sessionId;
        public Client(string url)
        {
            this.url = url;
            this.sessionId = "";
        }

        public AuthenticationHandler.Response Authenticate(string user, string password)
        {
            AuthenticationHandler authHandler = new AuthenticationHandler(this.url);
            AuthenticationHandler.Response response = authHandler.Authenticate(user, password);

            this.sessionId = response.sessionId;
            return response;
        }

        public string SearchApps(string appName)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(this.url + "/apps/studio/?search=" + appName + "&limit=20&offset=0");
            req.ContentType = "text/json";
            req.Headers.Add("Authorization", string.Format("SPECIAL {0}", this.sessionId));

            var response = HTTPClient.DispatchRequest(req);

            return response.stream;
        }
        public string GetAppInterface(string appPackageId)
        {
            // api/apps/{APPPACKAGEID}/interface
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(this.url + "/apps/" + appPackageId + "/interface/");
            req.ContentType = "text/json";
            req.Headers.Add("Authorization", string.Format("SPECIAL {0}", this.sessionId));

            var response = HTTPClient.DispatchRequest(req);

            return response.stream;
        }
        public string QueueJob(string jsonInParamsForApp)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(this.url + "/apps/jobs/");
            req.ContentType = "text/json";
            req.Method = "POST";
            req.Headers.Add("Authorization", string.Format("SPECIAL {0}", this.sessionId));
            var response = HTTPClient.DispatchRequest(req, jsonInParamsForApp, "POST");
            return response.stream;
        }
        public string GetJobStatus(string jobId)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(this.url + "/apps/jobs/" + jobId + "/");
            req.ContentType = "text/json";
            req.Headers.Add("Authorization", string.Format("SPECIAL {0}", this.sessionId));
            var response = HTTPClient.DispatchRequest(req);
            return response.stream;
        }
        public string WaitForJobCompletion(string jobId)
        {
            throw new System.Exception("write more code");
        }
        public string GetOutputMetadata(string jobId)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(this.url + "/apps/jobs/" + jobId + "/output/");
            req.ContentType = "text/json";
            req.Headers.Add("Authorization", string.Format("SPECIAL {0}", this.sessionId));
            var response = HTTPClient.DispatchRequest(req);
            return response.stream;
        }
        public string GetJobOutput(string jobId, string outputFileId, string format)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(this.url + "/apps/jobs/" + jobId + "/output/" + outputFileId + "/?FORMAT=" + format + "/");
            req.ContentType = "text/json";
            req.Headers.Add("Authorization", string.Format("SPECIAL {0}", this.sessionId));
            var response = HTTPClient.DispatchRequest(req);
            return response.stream;
        }
        public publishResponse SendAppAndGetId(string path, Action<long> onPercentCompleteCallback = null)
        {
            var request = GetRequest(Url("apps/"));
            var boundary = Guid.NewGuid().ToString("N");
            request.ContentType = "multipart/form-data; boundary=--" + boundary;
            request.Headers.Add("Authorization", string.Format("SPECIAL {0}", this.sessionId));
            request.Method = "POST";
            request.Timeout = int.MaxValue;
            request.ReadWriteTimeout = int.MaxValue;

            WriteFileAndData(request, path, sessionId, boundary, onPercentCompleteCallback);

            using (var response = (HttpWebResponse)request.GetResponse())
            using (var s = response.GetResponseStream())
            using (var reader = new StreamReader(s))
            {
                var rs = reader.ReadToEnd();

                CheckResponseForError(rs, response);

                var responseObject = ToObject<publishResponse>(rs);
                return responseObject;
            }
        }

        public dynamic DeleteApp(string appId)
        {

            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(this.url + "/apps/" + appId + "/");
            req.ContentType = "text/json";
            req.Method = "DELETE";
            req.Headers.Add("Authorization", string.Format("SPECIAL {0}", this.sessionId));
            //var response = HTTPClient.DispatchRequest(req,appId,"DELETE");
            HttpWebResponse res = (HttpWebResponse)req.GetResponse();
            return res;
        }
        //todo: published app must be validated

        //all following private methods are in support of PublishReponse harvested from Extras/httpClient.cs
        private void WriteFileAndData(HttpWebRequest request, string path, string sessionId, string boundary, Action<long> onPercentCompleteCallback = null)
        {
            //send the file
            var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            var formDataBytes = Encoding.UTF8.GetBytes(GetMultiPartFormContent(path, sessionId, boundary));
            var terminatorBytes = Encoding.UTF8.GetBytes(GetMultiPartFormTerminator(boundary));
            var postLength = formDataBytes.Length + fs.Length + terminatorBytes.Length;
            request.ContentLength = postLength;

            using (var requestStream = request.GetRequestStream())
            {
                requestStream.Write(formDataBytes, 0, formDataBytes.Length);

                WriteFileToStream(fs, requestStream, onPercentCompleteCallback);

                requestStream.Write(terminatorBytes, 0, terminatorBytes.Length);
            }
        }
        private string GetMultiPartFormContent(string path, string sessionId, string boundary)
        {
            var sb = new StringBuilder();
            sb.Append("\r\n");
            sb.AppendFormat("----{0}\r\n", boundary);
            sb.Append("Content-Disposition: form-data; name=\"sessionId\"\r\n\r\n");
            sb.AppendFormat("{0}\r\n", sessionId);
            sb.AppendFormat("----{0}\r\n", boundary);
            sb.AppendFormat("Content-Disposition: file; name=\"package\"; filename=\"{0}\"\r\n", Path.GetFileName(path));
            sb.Append("Content-Type: application/octet-stream\r\n\r\n");

            return sb.ToString();
        }
        private string GetMultiPartFormTerminator(string boundary)
        {
            var sb = new StringBuilder();
            sb.Append("\r\n");
            sb.AppendFormat("----{0}--\r\n", boundary);

            return sb.ToString();
        }
        private void WriteFileToStream(FileStream fileStream, Stream stream, Action<long> onPercentCompleteCallback = null)
        {
            var buffer = new byte[4096];

            int bytesRead;
            var length = fileStream.Length;

            var totalBytesWritten = 0;
            long lastPercentComplete = 0;
            while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
            {
                stream.Write(buffer, 0, bytesRead);

                var currentPercentComplete = ((double)(totalBytesWritten += bytesRead) / length) * 100.0;

                if (lastPercentComplete != (long)currentPercentComplete)
                {
                    if (onPercentCompleteCallback != null)
                        onPercentCompleteCallback((long)currentPercentComplete);
                    lastPercentComplete = (long)currentPercentComplete;
                }
            }
        }

        private void CheckResponseForError(string responseString, HttpWebResponse response)
        {
            if (response.StatusCode != HttpStatusCode.OK)
            {
                var ex = ToObject<webException>(responseString);
                throw new Exception(ex.message);
            }
        }
        private static T ToObject<T>(string content)
        {
            var serializer = new JavaScriptSerializer();
            return serializer.Deserialize<T>(content);
        }
        public string Url(string urlTail)
        {
            return string.Format("{0}/{1}", this.url, urlTail);
        }
        private HttpWebRequest GetRequest(string url)
        {
            var helpers = new Helpers();
            var request = helpers.GetWebRequest(new Uri(url));
            request.ContentType = "text/json";
            request.Timeout = 15000;
            return request;
        }
        public class publishResponse
        {
            public string id { get; set; }
            public publishValidation validation { get; set; }
            public IEnumerable<application> applications { get; set; }
        }

        public class application
        {
            public string filename { get; set; }
            public publishValidation validation { get; set; }
        }
        public class publishValidation
        {
            public bool isValid { get; set; }
            public bool requiresPrivateData { get; set; }
            public IEnumerable<string> messages { get; set; }
            public IEnumerable<string> datasetMessages { get; set; }
            public validationDisposition disposition { get; set; }
            public string validationId { get; set; }

            public string GetPrivateDataMessages()
            {
                return string.Join("\r\n", datasetMessages.ToArray());
            }

            public string GetValidationMessages()
            {
                return string.Join("\r\n", messages.ToArray());
            }
        }
        public enum validationDisposition
        {
            Valid,              // app is good and can stay
            RequiresApproval,   // app uses restricted tools - it can stay, but can't be executed without curator approval
            Invalid,            // app has other problems and will be deleted
            UnValidated         // app has not been validated
        }
        public class webException
        {
            public string exceptionName { get; set; }
            public string message { get; set; }
        }
        public class validationStatus
        {
            public string status { get; set; }
            public string disposition { get; set; }
        }

        // validate a published app can be run 
        // two step process. First, GetValidationStatus which indicates when validation disposition is available. 
        // Second, GetValidation, which gives actual status Valid, UnValid, etc.
        public validationStatus GetValidationStatus(string validationId)
        {
            var request = GetRequest(Url(string.Format("apps/jobs/{0}/", validationId)));
            request.Headers.Add("Authorization", "SPECIAL " + this.sessionId);
            using (var response = request.GetResponse())
            using (var s = response.GetResponseStream())
            {
                var reader = new StreamReader(s);
                var rs = reader.ReadToEnd();
                var responseObject = ToObject<validationStatus>(rs);
                return responseObject;
            }
        }

        public publishResponse GetValidation(string appId, string validationId)
        {
            var request = GetRequest(Url(string.Format("apps/{0}/validation/", appId)));
            request.Headers.Add("Authorization", "SPECIAL " + this.sessionId);
            try
            {
                using (var response = request.GetResponse())
                    return ParseValidationResponse(response);
            }
            catch (WebException e)
            {
                if (e.Response != null)
                {
                    if (((HttpWebResponse)e.Response).StatusCode == HttpStatusCode.MethodNotAllowed)
                    {
                        // try getting the validation the old way - for backwards compatibility
                        return GetValidationDeprecated(appId, validationId, sessionId);
                    }
                }

                throw;
            }
        }

        private publishResponse GetValidationDeprecated(string appId, string validationId, string sessionId)
        {
            var request = GetRequest(Url(string.Format("apps/{0}/validation/{1}/", appId, validationId)));
            request.Headers.Add("Authorization", "SPECIAL " + sessionId);

            using (var response = request.GetResponse())
                return ParseValidationResponse(response);
        }
        private static publishResponse ParseValidationResponse(WebResponse response)
        {
            using (var s = response.GetResponseStream())
            {
                var reader = new StreamReader(s);
                var rs = reader.ReadToEnd();
                var responseObject = ToObject<publishResponse>(rs);
                return responseObject;
            }
        }
    }
}
