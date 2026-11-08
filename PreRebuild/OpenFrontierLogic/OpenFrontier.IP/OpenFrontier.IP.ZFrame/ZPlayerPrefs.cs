using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace OpenFrontier.IP.ZFrame
{
	public static class ZPlayerPrefs
	{
		public static bool useSecure = true;

		private const int Iterations = 555;

		private static string strPassword = "IamZETOwow!123";

		private static string strSalt = "IvmD123A12";

		private static bool hasSetPassword = false;

		public static void DeleteAll()
		{
			PlayerPrefs.DeleteAll();
		}

		public static void DeleteKey(string key)
		{
			PlayerPrefs.DeleteKey(key);
		}

		public static float GetFloat(string key)
		{
			return GetFloat(key, 0f);
		}

		public static float GetFloat(string key, float defaultValue, bool isDecrypt = true)
		{
			float result = defaultValue;
			if (float.TryParse(GetString(key), out result))
			{
				return result;
			}
			return defaultValue;
		}

		public static int GetInt(string key)
		{
			return GetInt(key, 0);
		}

		public static int GetInt(string key, int defaultValue, bool isDecrypt = true)
		{
			int result = defaultValue;
			if (int.TryParse(GetString(key), out result))
			{
				return result;
			}
			return defaultValue;
		}

		public static string GetString(string key)
		{
			string rowString = GetRowString(key);
			if (!string.IsNullOrEmpty(rowString))
			{
				return Decrypt(rowString, strPassword);
			}
			return rowString;
		}

		public static string GetRowString(string key)
		{
			CheckPasswordSet();
			return PlayerPrefs.GetString(Encrypt(key, strPassword));
		}

		public static string GetString(string key, string defaultValue)
		{
			return Decrypt(GetRowString(key, defaultValue), strPassword);
		}

		public static string GetRowString(string key, string defaultValue)
		{
			CheckPasswordSet();
			string key2 = Encrypt(key, strPassword);
			string defaultValue2 = Encrypt(defaultValue, strPassword);
			return PlayerPrefs.GetString(key2, defaultValue2);
		}

		public static bool HasKey(string key)
		{
			CheckPasswordSet();
			return PlayerPrefs.HasKey(Encrypt(key, strPassword));
		}

		public static void Save()
		{
			CheckPasswordSet();
			PlayerPrefs.Save();
		}

		public static void SetFloat(string key, float value)
		{
			string value2 = Convert.ToString(value);
			SetString(key, value2);
		}

		public static void SetInt(string key, int value)
		{
			string value2 = Convert.ToString(value);
			SetString(key, value2);
		}

		public static void SetString(string key, string value)
		{
			CheckPasswordSet();
			PlayerPrefs.SetString(Encrypt(key, strPassword), Encrypt(value, strPassword));
		}

		public static void Initialize(string newPassword, string newSalt)
		{
			strPassword = newPassword;
			strSalt = newSalt;
			hasSetPassword = true;
		}

		private static void CheckPasswordSet()
		{
			if (!hasSetPassword)
			{
				Debug.LogWarning("Set Your Own Password & Salt!!!");
			}
		}

		private static byte[] GetIV()
		{
			return Encoding.UTF8.GetBytes(strSalt);
		}

		private static string Encrypt(string strPlain, string password)
		{
			if (!useSecure)
			{
				return strPlain;
			}
			try
			{
				DESCryptoServiceProvider dESCryptoServiceProvider = new DESCryptoServiceProvider();
				byte[] bytes = new Rfc2898DeriveBytes(password, GetIV(), 555).GetBytes(8);
				using MemoryStream memoryStream = new MemoryStream();
				using CryptoStream cryptoStream = new CryptoStream(memoryStream, dESCryptoServiceProvider.CreateEncryptor(bytes, GetIV()), CryptoStreamMode.Write);
				memoryStream.Write(GetIV(), 0, GetIV().Length);
				byte[] bytes2 = Encoding.UTF8.GetBytes(strPlain);
				cryptoStream.Write(bytes2, 0, bytes2.Length);
				cryptoStream.FlushFinalBlock();
				return Convert.ToBase64String(memoryStream.ToArray());
			}
			catch (Exception ex)
			{
				Debug.LogWarning("Encrypt Exception: " + ex);
				return strPlain;
			}
		}

		private static string Decrypt(string strEncript, string password)
		{
			if (!useSecure)
			{
				return strEncript;
			}
			try
			{
				using MemoryStream memoryStream = new MemoryStream(Convert.FromBase64String(strEncript));
				DESCryptoServiceProvider dESCryptoServiceProvider = new DESCryptoServiceProvider();
				byte[] iV = GetIV();
				memoryStream.Read(iV, 0, iV.Length);
				byte[] bytes = new Rfc2898DeriveBytes(password, iV, 555).GetBytes(8);
				using CryptoStream stream = new CryptoStream(memoryStream, dESCryptoServiceProvider.CreateDecryptor(bytes, iV), CryptoStreamMode.Read);
				using StreamReader streamReader = new StreamReader(stream);
				return streamReader.ReadToEnd();
			}
			catch (Exception ex)
			{
				Debug.LogWarning("Decrypt Exception: " + ex);
				return strEncript;
			}
		}
	}
}
