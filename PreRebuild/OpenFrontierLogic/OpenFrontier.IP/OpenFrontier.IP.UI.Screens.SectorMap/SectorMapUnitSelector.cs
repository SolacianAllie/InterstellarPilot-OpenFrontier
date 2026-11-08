using UnityEngine;
using UnityEngine.UI;
using Input = OpenFrontier.LegacyInput;

namespace OpenFrontier.IP.UI.Screens.SectorMap
{
	public class SectorMapUnitSelector : MonoBehaviour
	{
		public SectorMap SectorMap;

		private Button button;

		public float MaxSelectDistance = 100f;

		private void Awake()
		{
			button = GetComponent<Button>();
			button.onClick.AddListener(OnClick);
		}

		private void OnClick()
		{
			if (SectorMap.SectorMapCurrentTargetController == null)
			{
				return;
			}
			Vector3 mousePosition = Input.mousePosition;
			// Open Frontier: MaxSelectDistance is authored in canvas pixels
			// (scaleFactor 1). Scale it by the canvas factor so the touch
			// radius keeps its physical size on high-DPI mobile screens.
			float maxSelectDistance = MaxSelectDistance;
			Canvas componentInParent = SectorMap.MapAreaTransform.GetComponentInParent<Canvas>();
			if (componentInParent != null)
			{
				maxSelectDistance *= componentInParent.scaleFactor;
			}
			SectorMapItem sectorMapItem = null;
			float num = 0f;
			foreach (SectorMapItem item2 in SectorMap.Items)
			{
				if (item2 is SectorMapUnitItem sectorMapUnitItem && item2.Image != null && item2.Image.enabled && SectorMap.CanSelectUnit(sectorMapUnitItem.Unit))
				{
					float num2 = Vector3.Distance(item2.Image.transform.position, mousePosition);
					if (num2 < maxSelectDistance && (sectorMapItem == null || num2 < num))
					{
						sectorMapItem = item2;
						num = num2;
					}
				}
			}
			if (sectorMapItem != null)
			{
				SectorMap.SectorMapCurrentTargetController.TrySelect(SectorMapSelectionItem.FromUnit(((SectorMapUnitItem)sectorMapItem).Unit));
				return;
			}
			RectTransformUtility.ScreenPointToLocalPointInRectangle(SectorMap.MapAreaTransform, new Vector2(mousePosition.x, mousePosition.y), null, out var localPoint);
			Vector3 sectorPosition = SectorMap.ConvertMapAreaLocalSectorPosition(new Vector3(localPoint.x, localPoint.y, 0f));
			if (SectorMap.CanSelectSectorPosition(sectorPosition))
			{
				SectorMapSelectionItem item = SectorMapSelectionItem.FromSectorPosition(SectorMap.Sector, sectorPosition);
				SectorMap.SectorMapCurrentTargetController.TrySelect(item);
			}
		}
	}
}
