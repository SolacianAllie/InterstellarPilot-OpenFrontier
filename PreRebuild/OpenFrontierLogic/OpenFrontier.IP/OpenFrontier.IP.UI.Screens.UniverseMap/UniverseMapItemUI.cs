using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Core.Units;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.UniverseMap
{
	public class UniverseMapItemUI : MonoBehaviour
	{
		public Image HighlightImage;

		public Image SelectedImage;

		public Image PatrolPathCreatorImage;

		public SectorDisplayIcons SectorDisplayIcons;

		public SectorPathIconsDisplay SectorPathIconsDisplay;

		public TextMeshProUGUI NameLabel;

		public Sector Sector;

		public Image Image;

		private UniverseMapScreen universeMapUI;

		public Button Button;

		public Image PlanetImage;

		public Image AsteroidIconImage;

		public Image SectorControlImage;

		public Image PlayerUnitImage;

		public Image GasCloudImage;

		public float NextSectorIconsRefreshTime = float.MinValue;

		public const float SectorIconsRefreshTimeInterval = 5f;

		public UniverseMapScreen UniverseMapUI
		{
			get
			{
				return universeMapUI;
			}
			set
			{
				universeMapUI = value;
			}
		}

		public bool IsSelected => this == universeMapUI.SelectedSectorItem;

		private void Awake()
		{
			if (Button != null)
			{
				Button.onClick.AddListener(OnButtonClick);
			}
			PatrolPathCreatorImage.enabled = false;
		}

		public void UpdateMapItem()
		{
			RefreshVolatile();
			NameLabel.enabled = universeMapUI.ScaleFactor > universeMapUI.ShowLabelsScaleFactorThreshold;
			if (Time.time > NextSectorIconsRefreshTime)
			{
				SectorDisplayIcons.RefreshVolatile();
				NextSectorIconsRefreshTime = Time.time + 5f;
			}
		}

		public Vector3 CalcLocalPosition()
		{
			return UniverseMapUI.GetSectorLocalPosition(Sector);
		}

		public void Reposition()
		{
			if (Sector != null)
			{
				transform.localPosition = CalcLocalPosition();
			}
		}

		public void Refresh()
		{
			RefreshStatic();
			RefreshVolatile();
			Rescale();
			RefreshSelectedImage();
		}

		public void RefreshSelectedImage()
		{
			SelectedImage.enabled = IsSelected;
		}

		public void RefreshVolatile()
		{
			if (UniverseMapUI != null)
			{
				HighlightImage.enabled = UniverseMapUI.SelectedSectorItem != this;
			}
			RefreshSectorImageColor();
			RefreshSectorControlImage();
			RefreshSectorNameLabel();
			RefreshPlayerIcon();
			RefreshInteractable();
			SectorPathIconsDisplay.Sector = Sector;
			SectorPathIconsDisplay.Refresh();
		}

		public void Rescale()
		{
			float itemScale = universeMapUI.ItemScale;
			transform.localScale = new Vector3(itemScale, itemScale, itemScale);
		}

		private void RefreshPlayerIcon()
		{
			bool flag = EngineASX.Instance.LocalUnit != null && EngineASX.Instance.LocalUnit.Sector == Sector;
			PlayerUnitImage.gameObject.SetActive(flag);
			if (flag)
			{
				PlayerUnitImage.color = EngineASX.Instance.OwnedColor;
				PlayerUnitImage.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, 0f - EngineASX.Instance.LocalUnit.transform.rotation.eulerAngles.y));
				PlayerUnitImage.sprite = EngineASX.Instance.EngineResources.GetUnitClassThumbnailIconSpriteOrDefault(EngineASX.Instance.LocalUnit.UnitClass);
			}
		}

		private void RefreshSectorNameLabel()
		{
			if (Button.interactable && Sector.ControllingFaction != null)
			{
				NameLabel.color = GameController.Instance.GameSettings.ColorSettings.UniverseMapClaimedSectorLabelColor;
				if (Sector.ControllingFaction.HomeSector == Sector)
				{
					NameLabel.fontStyle = FontStyles.Bold;
				}
				else
				{
					NameLabel.fontStyle = FontStyles.Normal;
				}
			}
			else
			{
				NameLabel.color = GameController.Instance.GameSettings.ColorSettings.UniverseMapUnclaimedSectorLabelColor;
				NameLabel.fontStyle = FontStyles.Normal;
			}
			if (!Button.interactable)
			{
				Color color = NameLabel.color;
				color.a = 0.3f;
				NameLabel.color = color;
			}
		}

		public void RefreshInteractable()
		{
			Button.interactable = universeMapUI.EnabledSectors == null || universeMapUI.EnabledSectors.Contains(Sector);
		}

		public void RefreshStatic()
		{
			Reposition();
			RefreshNameLabel();
			RefreshAsteroidImage();
			RefreshPlanetImage();
			RefreshSectorImageScale();
			RefreshGasCloudImage();
			RefreshSectorDisplayIconsStatic();
		}

		public void RefreshNameLabel()
		{
			if (Sector != null)
			{
				NameLabel.text = Sector.Name;
			}
			else
			{
				NameLabel.text = null;
			}
		}

		private void RefreshSectorImageScale()
		{
			Image.transform.localScale = new Vector3(Sector.GateDistanceMultiplier, Sector.GateDistanceMultiplier, Sector.GateDistanceMultiplier);
		}

		private void RefreshSectorDisplayIconsStatic()
		{
			SectorDisplayIcons.Sector = Sector;
			SectorDisplayIcons.Refresh();
		}

		private void OnButtonClick()
		{
			if (UniverseMapUI != null && UniverseMapUI.AllowSectorSelection)
			{
				UniverseMapUI.TrySelect(this);
			}
		}

		private void RefreshPlanetImage()
		{
			bool flag = Sector != null && Sector.HasPlanets;
			PlanetImage.gameObject.SetActive(flag);
			if (!flag)
			{
				return;
			}
			List<Unit> unitsByType = Sector.GetUnitsByType(UnitType.Planet);
			if (unitsByType != null)
			{
				Unit unit = unitsByType.FirstOrDefault((Unit e) => e.UnitClass.PlanetClass != null);
				if (unit != null && unit.UnitClass.PlanetClass.PlanetSprite != null)
				{
					PlanetImage.sprite = unit.UnitClass.PlanetClass.PlanetSprite;
				}
			}
		}

		private void RefreshGasCloudImage()
		{
			bool flag = ShouldShowGasCloudImage(out var color);
			GasCloudImage.gameObject.SetActive(flag);
			if (flag)
			{
				GasCloudImage.color = color;
			}
		}

		private bool ShouldShowGasCloudImage(out Color color)
		{
			color = Color.white;
			if (ShouldShowAsteroidImage(out var _))
			{
				return false;
			}
			if (Sector != null && Sector.HasGasClouds)
			{
				List<Unit> unitsByType = Sector.GetUnitsByType(UnitType.GasCloud);
				if (unitsByType != null && unitsByType.Count > 0)
				{
					color = unitsByType[0].GetComponent<UnitGasCloud>().GasCloudClass.UniverseMapSectorSpriteColor;
					return true;
				}
			}
			return false;
		}

		private void RefreshAsteroidImage()
		{
			bool flag = ShouldShowAsteroidImage(out var sprite);
			AsteroidIconImage.gameObject.SetActive(flag);
			if (flag)
			{
				AsteroidIconImage.sprite = sprite;
			}
		}

		private bool ShouldShowAsteroidImage(out Sprite sprite)
		{
			sprite = null;
			if (Sector != null && Sector.HasAsteroidClusters)
			{
				List<Unit> unitsByType = Sector.GetUnitsByType(UnitType.AsteroidCluster);
				if (unitsByType != null && unitsByType.Count > 0)
				{
					AsteroidCluster component = unitsByType[0].GetComponent<AsteroidCluster>();
					sprite = component.AsteroidType.UniverseMapSectorTypeSprite;
					return true;
				}
			}
			return false;
		}

		private void RefreshSectorControlImage()
		{
			SectorControlImage.gameObject.SetActive(Sector.ControllingFaction != null);
			if (SectorControlImage.gameObject.activeSelf)
			{
				Color sectorControlImageColor = GetSectorControlImageColor();
				SectorControlImage.color = sectorControlImageColor.WithAlpha(SectorControlImage.color.a);
			}
		}

		public Color GetSectorControlImageColor()
		{
			if (Sector.ControllingFaction == null)
			{
				return GameController.Instance.GameSettings.ColorSettings.UniverseMapUnclaimedSectorLabelColor;
			}
			return EngineASX.Instance.GetFactionHostilityColor(Sector.ControllingFaction, EngineASX.Instance.LocalFaction);
		}

		public void RefreshSectorImageColor()
		{
			if (Image != null)
			{
				Color sectorColor = UniverseMapUI.GetSectorColor(Sector);
				Image.color = sectorColor;
			}
		}
	}
}
