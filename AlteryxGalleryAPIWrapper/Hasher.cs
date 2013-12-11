using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace AlteryxGalleryAPIWrapper
{
	class Hasher
	{
		public static string GetHmacHash(string content, string salt)
		{
			var hmacsha256 = new HMACSHA256(Encoding.UTF8.GetBytes(salt ?? ""));
			return BytesToString(hmacsha256.ComputeHash(Encoding.UTF8.GetBytes(content))).ToLower();
		}

		private static string BytesToString(byte[] buff)
		{
			var sb = new System.Text.StringBuilder();

			for (var i = 0; i < buff.Length; i++)
				sb.Append(buff[i].ToString("X2"));

			return sb.ToString();
		}

		public static string GetBcryptHash(string content, string salt)
		{
			if (string.IsNullOrEmpty(content))
				return content;
			return BCrypt.Net.BCrypt.HashPassword(content, salt);
		}
	}
}
