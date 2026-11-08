using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.UI
{
	public class ActiveUnitPreviewerUI : MonoBehaviour
	{
		public Vector3 CamPositionOffset = Vector3.zero;

		public Vector3 DefaultCamAngle = new Vector3(20f, 0f, 0f);

		public Vector3 DefaultShipRotation = new Vector3(0f, 135f, 0f);

		private float lastTime;

		public float MinUnitDist = 30f;

		public ActiveUnit ShipPreviewActiveUnit;

		public Camera ShipPreviewCamera;

		public string ShipPreviewLayerName = "";

		public Vector3 ShipPreviewRotationSpeed = new Vector3(0f, 10f, 0f);

		public Vector3 ShipRotation = Vector3.zero;

		private float unitRadius;

		public float UnitRadiusMultiplier = 3.6f;

		public void ChangeUnitClass(UnitClass unitClass)
		{
			DestroyPreviewUnit();
			if (unitClass != null)
			{
				ActiveUnit activeUnit = Object.Instantiate(EngineASX.LoadActiveUnit(unitClass.ActiveUnitClassName));
				activeUnit.transform.SetParent(transform);
				ShipPreviewActiveUnit = activeUnit.GetComponent<ActiveUnit>();
				ShipPreviewActiveUnit.transform.position = Vector3.zero;
				ShipPreviewActiveUnit.transform.rotation = Quaternion.Euler(DefaultShipRotation);
				unitRadius = unitClass.ShieldRingRadius;
				SetCamPosition();
				int layer = LayerMask.NameToLayer(ShipPreviewLayerName);
				UnityObjectHelper.ChangeLayerAndChildren(ShipPreviewActiveUnit.gameObject, layer);
			}
		}

		public void Update()
		{
			if (ShipPreviewActiveUnit != null)
			{
				float num = Time.realtimeSinceStartup - lastTime;
				ShipRotation += ShipPreviewRotationSpeed * num;
				lastTime = Time.realtimeSinceStartup;
				ShipPreviewActiveUnit.transform.localRotation = Quaternion.Euler(ShipRotation);
			}
		}

		private void OnEnable()
		{
			ShipRotation = DefaultShipRotation;
			lastTime = Time.realtimeSinceStartup;
		}

		private void SetCamPosition()
		{
			Quaternion quaternion = Quaternion.Euler(DefaultCamAngle);
			ShipPreviewCamera.transform.rotation = quaternion;
			float num = Mathf.Max(unitRadius * UnitRadiusMultiplier, MinUnitDist);
			ShipPreviewCamera.transform.position = quaternion * new Vector3(0f, 0f, 0f - num) + CamPositionOffset;
		}

		private void DestroyPreviewUnit()
		{
			if (ShipPreviewActiveUnit != null)
			{
				Object.Destroy(ShipPreviewActiveUnit.gameObject);
				ShipPreviewActiveUnit = null;
			}
		}
	}
}
