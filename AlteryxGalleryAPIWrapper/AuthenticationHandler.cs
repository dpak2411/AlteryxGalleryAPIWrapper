using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace AlteryxGalleryAPIWrapper
{
	public class AuthenticationHandler
	{
		private string url;
		public AuthenticationHandler(string url)
		{
			this.url = url;
		}

		public Response Authenticate(string user, string password)
		{
			AuthenticationHandler.AuthParameters authParams = PreAuthenticate(user);

			string hmacKey = authParams.Parameters["hmacKey"];
			string salt = authParams.Parameters["salt"];
			string nonce = authParams.Parameters["nonce"];

			var hash = Hasher.GetHmacHash(password, hmacKey);
			hash = Hasher.GetBcryptHash(hash, salt);
			hash = Hasher.GetHmacHash(nonce + "_" + hash, hmacKey);

			AuthenticationHandler.Response response = CreateSession(user, hash, nonce);

			return response;
		}

		private AuthParameters PreAuthenticate(string user)
		{
			HttpWebRequest req = (HttpWebRequest)WebRequest.Create(this.url + "/auth/preauth/");
			req.ContentType = "text/json";

			var response = HTTPClient.DispatchRequest(
				req,
				new
				{
					scheme = "alteryx",
					parameters = new[] { new { name = "email", value = user } }
				},
				"POST"
			);

			if (!string.IsNullOrEmpty(response.stream))
				return AuthenticationHandler.ParseAuthParameters(response.stream);

			return new AuthenticationHandler.AuthParameters();
		}

		private Response CreateSession(string user, string hash, string nonce)
		{

			HttpWebRequest req = (HttpWebRequest)WebRequest.Create(this.url + "/auth/sessions/");
			req.ContentType = "text/json";

			var response = HTTPClient.DispatchRequest(
				req,
				new
				{
					scheme = "alteryx",
					parameters = new[]
					{
						new {name = "email", value = user},
						new {name = "password", value = hash},
						new {name = "nonce", value = nonce},
						new {name = "keepAlive", value = "true" }
					}
				},
				"POST"
			);

			if (!string.IsNullOrEmpty(response.stream))
				return Serialization.ToObject<AuthenticationHandler.Response>(response.stream);

			return new AuthenticationHandler.Response();
		}

		public static AuthParameters ParseAuthParameters(string response)
		{
			var obj = Serialization.ToObject<PreAuthObject>(response);

			return new AuthParameters
			{
				Scheme = obj.scheme,
				Parameters = obj.parameters.ToDictionary(o => o.name, o => o.value)
			};
		}

		public struct AuthParameters
		{
			public string Scheme { get; set; }
			public Dictionary<string, string> Parameters { get; set; }
		}

		public struct PreAuthObject
		{
			public string scheme { get; set; }
			public IList<Parameter> parameters { get; set; }

			public class Parameter
			{
				public string name { get; set; }
				public string value { get; set; }
			}
		}

		public struct Response
		{
			public string sessionId { get; set; }
		}
	}
}
