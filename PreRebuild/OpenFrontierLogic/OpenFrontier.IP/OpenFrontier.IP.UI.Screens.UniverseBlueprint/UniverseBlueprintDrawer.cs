using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.WorldGeneration.Models;
using OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers;
using OpenFrontier.IP.UI.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.UniverseBlueprint
{
	public class UniverseBlueprintDrawer : MonoBehaviour
	{
		public float ConnectionRotationZFudge;

		public Vector3 DefaultLabelOffset = new Vector3(0f, -50f, 0f);

		public Color DefaultSceneColor = Color.white;

		public Color DefaultSceneConnectionColor = Color.white;

		public float GutterSize = 20f;

		public RectTransform ItemHolder;

		public UniverseBlueprintMapItem ItemPrefab;

		private List<UniverseBlueprintMapItem> items = new List<UniverseBlueprintMapItem>();

		public ScrollRect MapScrollView;

		public GameObject SceneConnectionPrefab;

		public float SceneConnectionWidthScale = 20f;

		public float ScenePositionScaleFactor = 1f;

		public bool UseSecurityColors = true;

		private Vector3 worldCenterMapPosition = Vector3.zero;

		public CreateBlueprintSectorsSeederSettings CreateBlueprintSectorsSeederSettings;

		public Vector3 GetSectorLocalPosition(SectorBlueprint sector)
		{
			return GetLocalPosition(GetSectorMapPosition(sector));
		}

		private Vector3 GetSectorMapPosition(SectorBlueprint sector)
		{
			return sector.Position * CreateBlueprintSectorsSeederSettings.SectorMapPositionScaleFudge;
		}

		private Vector3 GetSectorMapPosition(Vector3 position)
		{
			return position * CreateBlueprintSectorsSeederSettings.SectorMapPositionScaleFudge;
		}

		public Vector3 GetLocalPosition(Vector3 universePosition)
		{
			universePosition -= worldCenterMapPosition;
			return new Vector3(universePosition.x * ScenePositionScaleFactor, universePosition.z * ScenePositionScaleFactor, 0f);
		}

		public void CenterOnMapPosition(Vector3 position)
		{
			ItemHolder.transform.localPosition = -GetLocalPosition(position);
		}

		public void Refresh(WorldBlueprint worldBlueprint)
		{
			PopulateSceneItems(worldBlueprint);
		}

		private void UpdateWorldCenter(WorldBlueprint worldBlueprint)
		{
			List<SectorBlueprint> sectorNodes = worldBlueprint.SectorNodes;
			float a = sectorNodes.Select((SectorBlueprint e) => GetSectorMapPosition(e).x).Min();
			float b = sectorNodes.Select((SectorBlueprint e) => GetSectorMapPosition(e).x).Max();
			float a2 = sectorNodes.Select((SectorBlueprint e) => GetSectorMapPosition(e).z).Min();
			float b2 = sectorNodes.Select((SectorBlueprint e) => GetSectorMapPosition(e).z).Max();
			worldCenterMapPosition = new Vector3(Mathf.Lerp(a, b, 0.5f), 0f, Mathf.Lerp(a2, b2, 0.5f));
		}

		private void Clear()
		{
			items.Clear();
			if (ItemHolder != null)
			{
				UnityObjectHelper.DestroyChildren(ItemHolder.gameObject, destroyImmediate: true);
			}
		}

		private void PopulateSceneItems(WorldBlueprint worldBlueprint)
		{
			if (items == null)
			{
				items = new List<UniverseBlueprintMapItem>();
			}
			Clear();
			if (ItemHolder != null)
			{
				UpdateWorldCenter(worldBlueprint);
				CreateSectorConnections(worldBlueprint);
				CreateSectorItems(worldBlueprint);
				UpdateItemHolderSize();
			}
		}

		private void CreateSectorConnections(WorldBlueprint worldBlueprint)
		{
			HashSet<ulong> hashSet = new HashSet<ulong>();
			for (int i = 0; i < worldBlueprint.SectorNodes.Count; i++)
			{
				SectorBlueprint sectorBlueprint = worldBlueprint.SectorNodes[i];
				foreach (SectorConnection connection in sectorBlueprint.Connections)
				{
					ulong item = Helper.PairId(i, worldBlueprint.SectorNodes.IndexOf(connection.TargetSector));
					if (!hashSet.Contains(item))
					{
						hashSet.Add(item);
						Vector3 sectorLocalPosition = GetSectorLocalPosition(sectorBlueprint);
						Vector3 sectorLocalPosition2 = GetSectorLocalPosition(connection.TargetSector);
						GameObject gameObject = Object.Instantiate(SceneConnectionPrefab);
						gameObject.transform.SetParent(ItemHolder.transform);
						gameObject.transform.localPosition = sectorLocalPosition;
						float z = Mathf.Atan2(sectorLocalPosition2.y - sectorLocalPosition.y, sectorLocalPosition2.x - sectorLocalPosition.x) * 57.29578f + ConnectionRotationZFudge;
						gameObject.transform.localRotation = Quaternion.Euler(0f, 0f, z);
						gameObject.transform.localScale = Vector3.one;
						gameObject.name = $"{GetSectorGameObjectName(sectorBlueprint)} to {GetSectorGameObjectName(connection.TargetSector)}";
						Graphic component = gameObject.GetComponent<Graphic>();
						Vector3 localPosition = component.rectTransform.localPosition;
						localPosition.z = 0f;
						component.rectTransform.localPosition = localPosition;
						SetConnectionColor(sectorBlueprint, connection, component);
						SetConnectionScale(sectorLocalPosition, sectorLocalPosition2, component);
					}
				}
			}
		}

		private string GetSectorGameObjectName(SectorBlueprint sector)
		{
			return "Sector_" + (string.IsNullOrWhiteSpace(sector.Name) ? "Unnamed" : sector.Name);
		}

		private void SetConnectionScale(Vector3 p1, Vector3 p2, Graphic graphic)
		{
			Vector3 vector = new Vector3
			{
				x = Vector3.Distance(p1, p2),
				y = SceneConnectionWidthScale
			};
			graphic.rectTransform.SetWidth(Mathf.RoundToInt(vector.x));
			graphic.rectTransform.SetHeight(Mathf.RoundToInt(vector.y));
		}

		private void SetConnectionColor(SectorBlueprint scene, SectorConnection neighbor, Graphic graphic)
		{
			float securityLevel = Mathf.Min(scene.SecurityLevel, neighbor.TargetSector.SecurityLevel);
			Color color = (UseSecurityColors ? GetConnectionColor(securityLevel) : DefaultSceneConnectionColor);
			graphic.color = color;
		}

		private void CreateSectorItems(WorldBlueprint worldBlueprint)
		{
			if (ItemPrefab != null)
			{
				foreach (SectorBlueprint sectorNode in worldBlueprint.SectorNodes)
				{
					UniverseBlueprintMapItem universeBlueprintMapItem = CreateItem(sectorNode);
					universeBlueprintMapItem.name = GetSectorGameObjectName(sectorNode);
					universeBlueprintMapItem.transform.SetParent(ItemHolder.transform, worldPositionStays: true);
					universeBlueprintMapItem.transform.localScale = Vector3.one;
					universeBlueprintMapItem.Refresh();
					Color color = (UseSecurityColors ? GetConnectionColor(sectorNode.SecurityLevel) : DefaultSceneColor);
					universeBlueprintMapItem.Sprite.color = color;
					items.Add(universeBlueprintMapItem);
				}
				return;
			}
			Debug.LogError("Cannot create scene items. ItemPrefab not assigned");
		}

		private void UpdateItemHolderSize()
		{
			float num = 0f;
			float num2 = 0f;
			foreach (UniverseBlueprintMapItem item in items)
			{
				num = Mathf.Max(num, Mathf.Abs(item.transform.localPosition.x) * 2f);
				num2 = Mathf.Max(num2, Mathf.Abs(item.transform.localPosition.y) * 2f);
			}
			ItemHolder.SetWidth(num + GutterSize * 2f);
			ItemHolder.SetHeight(num2 + GutterSize * 2f);
		}

		private Color GetConnectionColor(float securityLevel)
		{
			return EngineASX.Instance.GetSecurityColor(securityLevel);
		}

		private UniverseBlueprintMapItem CreateItem(SectorBlueprint s)
		{
			UniverseBlueprintMapItem component = Object.Instantiate(ItemPrefab.gameObject).GetComponent<UniverseBlueprintMapItem>();
			component.UniverseDrawer = this;
			component.Sector = s;
			component.Refresh();
			return component;
		}
	}
}
