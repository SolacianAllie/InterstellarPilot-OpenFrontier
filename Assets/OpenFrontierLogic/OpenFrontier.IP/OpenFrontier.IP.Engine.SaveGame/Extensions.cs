using OpenFrontier.IP.SavedGames.V2.Model;
using UnityEngine;

namespace OpenFrontier.IP.Engine.SaveGame
{
	public static class Extensions
	{
		public static Vector3 ToVector3(this Vec3 v3)
		{
			return new Vector3(v3.X, v3.Y, v3.Z);
		}

		public static Color ToColor(this Vec3 vector3)
		{
			return new Color(vector3.X, vector3.Y, vector3.Z);
		}

		public static Quaternion ToQuaternion(this Vec4 v4)
		{
			return new Quaternion(v4.X, v4.Y, v4.Z, v4.W);
		}

		public static Vec3 ToVec3(this Vector3 v3)
		{
			return new Vec3
			{
				X = v3.x,
				Y = v3.y,
				Z = v3.z
			};
		}

		public static Vec3 ToVec3(this Color color)
		{
			return new Vec3
			{
				X = color.r,
				Y = color.g,
				Z = color.b
			};
		}

		public static Vec4 ToVec4(this Quaternion quaternion)
		{
			return new Vec4
			{
				X = quaternion.x,
				Y = quaternion.y,
				Z = quaternion.z,
				W = quaternion.w
			};
		}
	}
}
