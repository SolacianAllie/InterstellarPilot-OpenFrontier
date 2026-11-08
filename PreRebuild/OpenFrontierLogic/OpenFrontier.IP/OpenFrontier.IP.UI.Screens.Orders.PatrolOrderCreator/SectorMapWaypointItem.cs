using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions.Intel;
using OpenFrontier.IP.Engine.Pathfinding;
using OpenFrontier.IP.UI.Extensions;
using OpenFrontier.IP.UI.Screens.SectorMap;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.Orders.PatrolOrderCreator
{
	public class SectorMapWaypointItem : SectorMapItem
	{
		public Color ConnectionColor = Color.green;

		public Color LoopConnectionColor = Color.grey;

		public TextMeshProUGUI Label;

		public SectorTarget SectorTarget;

		public SectorTarget PreviousNodeSectorTarget;

		public SectorTarget NextNodeSectorTarget;

		public int NodeIndex;

		public Image PreviousNodeConnectionImage;

		public Image NextNodeConnectionImage;

		public float ConnectionImageLengthMultiplierFudge = 1f;

		public float ConnectionImageRotationZFudge;

		private SectorTarget effectivePreviousNodeSectorTarget;

		private SectorTarget effectiveNextNodeSectorTarget;

		public bool ShowSectorPositionInLabel;

		public bool IsFirstNode => NodeIndex == 0;

		public bool IsLastNode { get; set; }

		protected override Vector3 GetWorldPosition()
		{
			return SectorTarget.Sector.ToWorldPosition(SectorTarget.SectorPosition);
		}

		public override void Refresh()
		{
			base.Refresh();
			if (ShowSectorPositionInLabel)
			{
				Label.text = $"{NodeIndex + 1}. {TextFormattingHelper.FormatSectorPosition(SectorTarget.SectorPosition)}";
			}
			else
			{
				Label.text = $"{NodeIndex + 1}";
			}
			RefreshNextNodeConnectionImage();
			RefreshPreviousNodeConnectionImage();
		}

		private void RefreshPreviousNodeConnectionImage()
		{
			RefreshEffectivePreviousNodeSectorTarget();
			bool flag = effectivePreviousNodeSectorTarget != null;
			PreviousNodeConnectionImage.enabled = flag;
			if (flag)
			{
				TransformPreviousNodeConnectionImage();
				PreviousNodeConnectionImage.color = (IsFirstNode ? LoopConnectionColor : ConnectionColor);
			}
		}

		private void RefreshNextNodeConnectionImage()
		{
			RefreshEffectiveNextNodeSectorTarget();
			bool flag = effectiveNextNodeSectorTarget != null;
			NextNodeConnectionImage.enabled = flag;
			if (flag)
			{
				TransformNextNodeConnectionImage();
				NextNodeConnectionImage.color = (IsLastNode ? LoopConnectionColor : ConnectionColor);
			}
		}

		private void RefreshEffectivePreviousNodeSectorTarget()
		{
			effectivePreviousNodeSectorTarget = CalculateEffectivePreviousNodeSectorTarget();
		}

		private void RefreshEffectiveNextNodeSectorTarget()
		{
			effectiveNextNodeSectorTarget = CalculateEffectiveNextNodeSectorTarget();
		}

		private SectorTarget CalculateEffectivePreviousNodeSectorTarget()
		{
			if (PreviousNodeSectorTarget == null)
			{
				return null;
			}
			if (PreviousNodeSectorTarget.Sector == SectorTarget.Sector)
			{
				return PreviousNodeSectorTarget;
			}
			return CalculateEffectiveSectorTarget(SectorTarget, PreviousNodeSectorTarget);
		}

		private SectorTarget CalculateEffectiveNextNodeSectorTarget()
		{
			if (NextNodeSectorTarget == null)
			{
				return null;
			}
			if (NextNodeSectorTarget.Sector != SectorTarget.Sector)
			{
				return CalculateEffectiveSectorTarget(SectorTarget, NextNodeSectorTarget);
			}
			return null;
		}

		public static SectorTarget CalculateEffectiveSectorTarget(SectorTarget from, SectorTarget to)
		{
			if (EngineASX.Instance.LocalFaction == null)
			{
				return null;
			}
			UniversePath universePath = EngineASX.Instance.LocalFaction.Intel.GetUniversePath(from.Sector, to.Sector);
			if (universePath != null && universePath.Jumps >= 0 && universePath.Nodes.Count > 1)
			{
				UniversePathNode universePathNode = universePath.Nodes[1];
				if (universePathNode.Wormhole != null)
				{
					return SectorTarget.FromSectorPosition(universePathNode.Wormhole.Unit.Sector, universePathNode.Wormhole.Unit.SectorPosition);
				}
			}
			return null;
		}

		private void TransformPreviousNodeConnectionImage()
		{
			TransformNodeConnectionImage(SectorMap, PreviousNodeConnectionImage, SectorTarget.SectorPosition, effectivePreviousNodeSectorTarget.SectorPosition);
		}

		private void TransformNextNodeConnectionImage()
		{
			TransformNodeConnectionImage(SectorMap, NextNodeConnectionImage, SectorTarget.SectorPosition, effectiveNextNodeSectorTarget.SectorPosition);
		}

		private void TransformNodeConnectionImage(OpenFrontier.IP.UI.Screens.SectorMap.SectorMap sectorMap, Image image, Vector3 sectorPosition, Vector3 previousSectorPosition)
		{
			Vector3 p = SectorMap.ConvertWorldToScreen(sectorPosition);
			Vector3 p2 = SectorMap.ConvertWorldToScreen(previousSectorPosition);
			float z = Mathf.Atan2(p2.y - p.y, p2.x - p.x) * 57.29578f + ConnectionImageRotationZFudge;
			image.transform.localRotation = Quaternion.Euler(0f, 0f, z);
			Vector3 connectionScaleFromDistance = GetConnectionScaleFromDistance(p, p2);
			image.rectTransform.SetWidth(Mathf.RoundToInt(connectionScaleFromDistance.x * ConnectionImageLengthMultiplierFudge));
		}

		public Vector3 GetConnectionScaleFromDistance(Vector3 p1, Vector3 p2)
		{
			Vector3 result = new Vector3(1f, 1f, 1f);
			result.x = Vector3.Distance(p1, p2);
			return result;
		}

		private void Update()
		{
			if (EngineASX.LoadedAndReady && SectorMap != null)
			{
				if (effectivePreviousNodeSectorTarget != null)
				{
					TransformPreviousNodeConnectionImage();
				}
				if (effectiveNextNodeSectorTarget != null)
				{
					TransformNextNodeConnectionImage();
				}
			}
		}

		public override void SetActivated(bool active)
		{
			gameObject.SetActive(active);
		}
	}
}
