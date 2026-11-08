using System;
using UnityEngine;

namespace OpenFrontier.IP.SecurePlayerPrefs
{
	public static class SPlayerPrefs
	{
		private const int Iterations = 555;

		private static string encryptKey;

		private static string encryptSalt;

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

		public static int GetInt(string key, int defaultValue)
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
			string encryptedValue = GetEncryptedValue(key);
			if (encryptedValue != string.Empty)
			{
				return Decrypt(encryptedValue);
			}
			return string.Empty;
		}

		public static string GetEncryptedValue(string key)
		{
			ThrowIfNoKeySet();
			return PlayerPrefs.GetString(Encrypt(key), string.Empty);
		}

		public static bool HasKey(string key)
		{
			ThrowIfNoKeySet();
			return PlayerPrefs.HasKey(Encrypt(key));
		}

		public static void Save()
		{
			ThrowIfNoKeySet();
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
			ThrowIfNoKeySet();
			string key2 = Encrypt(key);
			string value2 = Encrypt(value);
			PlayerPrefs.SetString(key2, value2);
		}

		public static void Initialize(string newKey, string newSalt)
		{
			encryptKey = newKey;
			encryptSalt = newSalt;
		}

		private static void ThrowIfNoKeySet()
		{
			if (string.IsNullOrEmpty(encryptKey))
			{
				throw new ApplicationException("Key has not been set");
			}
			if (string.IsNullOrEmpty(encryptSalt))
			{
				throw new ApplicationException("Salt has not been set");
			}
		}

		private static string Encrypt(string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				throw new NullReferenceException("text is null or empty");
			}
			throw new NotSupportedException("SPlayerPrefs not supported on this platform");
		}

		private static string Decrypt(string text)
		{
			if (text == null)
			{
				throw new NullReferenceException("text");
			}
			throw new NotSupportedException("SPlayerPrefs not supported on this platform");
		}
	}
}
