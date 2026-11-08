using UnityEngine;

namespace OpenFrontier.IP
{
	public static class LogWrapper
	{
		public static int VerboseLevel = 1;

		public static bool LogMsgs => false;

		public static void Log(object msg)
		{
			Log(msg, null, -1);
		}

		public static void Log(object msg, Object context, int msgVerboseLevel)
		{
			if (msgVerboseLevel == -1 || msgVerboseLevel <= VerboseLevel)
			{
				Debug.Log(msg, context);
			}
		}
	}
}
