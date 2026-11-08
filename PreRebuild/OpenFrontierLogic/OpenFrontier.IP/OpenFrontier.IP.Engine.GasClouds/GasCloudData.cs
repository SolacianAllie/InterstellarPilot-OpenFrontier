using UnityEngine;

namespace OpenFrontier.IP.Engine.GasClouds
{
	public class GasCloudData : MonoBehaviour
	{
		public Color AmbientLightColor = Color.black;

		public Color DirectionLightColor = Color.black;

		public Color SkyTintColor = Color.gray;

		public float SkyExposure = 0.8f;

		public bool FogOn = true;

		public float FogStartDistance = 50f;

		public float FogEndDistance = 300f;

		public Color FogColor = Color.grey;
	}
}
