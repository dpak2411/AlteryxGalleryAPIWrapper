using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;

namespace AlteryxGalleryAPIWrapper
{
	class HTTPClient
	{
		public static ResponseObject DispatchRequest(HttpWebRequest request, object data = null, string method = "GET")
		{
			request.Method = method;

			if (data != null)
			{
				using (var body = new StreamWriter(request.GetRequestStream()))
				{
					body.Write(Serialization.ToJSON(data));
				}
			}

			using (var response = (HttpWebResponse)request.GetResponse())
			using (var stream = response.GetResponseStream())
			using (var reader = new StreamReader(stream))
			{
				return new ResponseObject { response = response, stream = reader.ReadToEnd() };
			}
		}

        public static ResponseObject DispatchRequest(HttpWebRequest request, string rawData, string method = "GET")
        {
            
            request.Method = method;

            using (var body = new StreamWriter(request.GetRequestStream()))
            {
                    body.Write(rawData);
            }

            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                return new ResponseObject { response = response, stream = reader.ReadToEnd() };
            }
        }
		public struct ResponseObject
		{
			public HttpWebResponse response;
			public string stream;
		}

	}
}
