using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Web.Script.Serialization;

namespace AlteryxGalleryAPIWrapper
{
	class Serialization
	{
		public static string ToJSON(object obj)
		{
			var serializer = new JavaScriptSerializer();
			return serializer.Serialize(obj);
		}

		public static T ToObject<T>(string content)
		{
			var serializer = new JavaScriptSerializer();
			return serializer.Deserialize<T>(content);
		}
	}
}
