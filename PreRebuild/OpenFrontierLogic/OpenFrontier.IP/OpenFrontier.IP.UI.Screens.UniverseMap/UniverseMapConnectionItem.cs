using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.UniverseMap
{
	public class UniverseMapConnectionItem : MonoBehaviour
	{
		public Image Image;

		public Image WaypointImage;

		public Sector Sector;

		public SectorNeighbour? Neighbour;

		public UniverseMapScreen UniverseMap;

		public bool RefreshOnUpdate = true;

		public Wormhole ConnectingGate;

		private void Awake()
		{
			WaypointImage.enabled = false;
			Image.enabled = false;
		}

		public void Build()
		{
			SetPositionAndRotation();
			Transform transform = WaypointImage.transform;
			Transform transform2 = Image.transform;
			Vector3 vector = (base.transform.localScale = Vector3.one);
			Vector3 localScale = (transform2.localScale = vector);
			transform.localScale = localScale;
			ZeroGraphicZ(Image);
			ZeroGraphicZ(WaypointImage);
			Image.enabled = true;
		}

		public void SetPositionAndRotation()
		{
			Vector3 sectorLocalPosition = UniverseMap.GetSectorLocalPosition(Sector);
			Vector3 sectorLocalPosition2 = UniverseMap.GetSectorLocalPosition(Neighbour.Value.Sector);
			base.transform.localPosition = sectorLocalPosition;
			float z = Mathf.Atan2(sectorLocalPosition2.y - sectorLocalPosition.y, sectorLocalPosition2.x - sectorLocalPosition.x) * 57.29578f + UniverseMap.ConnectionRotationZFudge;
			Transform transform = WaypointImage.transform;
			Quaternion localRotation = (Image.transform.localRotation = Quaternion.Euler(0f, 0f, z));
			transform.localRotation = localRotation;
		}

		private void ZeroGraphicZ(Graphic graphic)
		{
			Vector3 localPosition = graphic.rectTransform.localPosition;
			localPosition.z = 0f;
			graphic.rectTransform.localPosition = localPosition;
		}

		private bool IsGateConnectionOnPlayerWaypointPath(Wormhole wormhole)
		{
			if (UniverseMapScreen.IsGateConnectionOnPlayerWaypointPath(EngineASX.Instance.LocalPlayer.WaypointController.CustomPath, wormhole))
			{
				return true;
			}
			foreach (PlayerWaypointPath missionPath in EngineASX.Instance.LocalPlayer.WaypointController.MissionPaths)
			{
				if (UniverseMapScreen.IsGateConnectionOnPlayerWaypointPath(missionPath, wormhole))
				{
					return true;
				}
			}
			return false;
		}

		public void Refresh()
		{
			bool flag = IsGateConnectionOnPlayerWaypointPath(Neighbour.Value.ConnectingGate);
			WaypointImage.enabled = flag;
			if (flag)
			{
				WaypointImage.color = EngineASX.Instance.WaypointController.CustomPath.WaypointColor;
			}
			bool flag2 = !ConnectingGate.IsUnstable;
			if (!flag2)
			{
				Neighbour = Sector.GetNeighbour(ConnectingGate);
				if (!Neighbour.HasValue)
				{
					return;
				}
				SetPositionAndRotation();
			}
			bool flag3 = !UniverseMap.IgnoreIntel && Neighbour.Value.IsStableConnection && !EngineASX.Instance.LocalFaction.Intel.HasWormholeBeenEntered(Neighbour.Value.ConnectingGate);
			if (flag2)
			{
				Image.color = UniverseMap.GetConnectionColor(Sector, UniverseMap.UseSecurityColors, UniverseMap.DefaultSceneConnectionColor);
				Image.sprite = (flag3 ? UniverseMap.ConnectionStubSprite : UniverseMap.ConnectionSprite);
			}
			Vector3 sectorLocalPosition = UniverseMap.GetSectorLocalPosition(Sector);
			Vector3 sectorLocalPosition2 = UniverseMap.GetSectorLocalPosition(Neighbour.Value.Sector);
			float num = 30f * UniverseMap.ItemScale;
			if (num < 1f)
			{
				num = 1f;
			}
			if (flag3)
			{
				float num2 = UniverseMap.ScaleFactor / UniverseMap.DefaultScaleFactor;
				float f = UniverseMap.SceneConnectionStubLength * num2;
				Image.rectTransform.SetSize(Mathf.RoundToInt(f), num);
			}
			else
			{
				float connectionScaleFromDistance = UniverseMap.GetConnectionScaleFromDistance(sectorLocalPosition, sectorLocalPosition2);
				Image.rectTransform.SetSize(Mathf.RoundToInt(connectionScaleFromDistance * UniverseMap.StableConnectionLengthMultiplierFudge), num);
			}
			if (flag)
			{
				float connectionScaleFromDistance2 = UniverseMap.GetConnectionScaleFromDistance(sectorLocalPosition, sectorLocalPosition2);
				WaypointImage.rectTransform.SetSize(Mathf.RoundToInt(connectionScaleFromDistance2), num);
			}
		}
	}
}
