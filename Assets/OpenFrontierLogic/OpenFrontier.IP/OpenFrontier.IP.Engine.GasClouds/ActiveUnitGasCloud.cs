using System;
using OpenFrontier.IP.Engine.ActiveUnitFx;
using OpenFrontier.IP.Misc;
using OpenFrontier.Unity.Utils;
using UnityEngine;
using Random = System.Random;

namespace OpenFrontier.IP.Engine.GasClouds
{
	public class ActiveUnitGasCloud : MonoBehaviour
	{
		public ActiveGasCloud ActiveGasCloudPrefab;

		public Transform Bilboard;

		private Unit unit;

		public Unit Unit => unit;

		private void Awake()
		{
			Unit unit = (this.unit = GetComponentInParent<Unit>());
			ActiveUnitFadeOutMeshController componentInChildren = GetComponentInChildren<ActiveUnitFadeOutMeshController>();
			if (componentInChildren != null)
			{
				componentInChildren.UpperDistance = unit.Radius + 50f;
				componentInChildren.LowerDistance = unit.Radius;
			}
			OffsetLocalPositionTowardsCamera componentInChildren2 = GetComponentInChildren<OffsetLocalPositionTowardsCamera>();
			if (componentInChildren2 != null)
			{
				componentInChildren2.Distance = unit.Radius * 0.95f;
			}
			GasCloudBilboardController componentInChildren3 = GetComponentInChildren<GasCloudBilboardController>();
			if (componentInChildren3 != null)
			{
				componentInChildren3.Distance = unit.Radius * 0.95f;
			}
			if (Bilboard != null)
			{
				System.Random random = new System.Random(this.unit.Seed + 1000);
				Bilboard.transform.localRotation = Quaternion.Euler(0f, 0f, random.NextFloat(0f, 360f));
				float num = this.unit.Radius / GameController.Instance.GameSettings.GasCloudSettings.BilboardMaxScaleReferenceRadius * GameController.Instance.GameSettings.GasCloudSettings.BilboardMaxScale;
				Bilboard.localScale = new Vector3(num, num, num);
			}
		}
	}
}
