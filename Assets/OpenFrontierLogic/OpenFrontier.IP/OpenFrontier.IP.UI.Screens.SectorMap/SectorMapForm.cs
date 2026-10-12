using System;
using OpenFrontier.IP.Engine;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.SectorMap
{
	public class SectorMapForm : MonoBehaviour
	{
		public Transform StaticTargetInfoTransform;

		public float TimeBetweenUnitRefresh = 2f;

		public bool AutoRefresh = true;

		public Button ConfirmSelectionButton;

		public Button CenterOnPlayerButton;

		private Sector sector;

		[FormerlySerializedAs("SceneDrawer")]
		public SectorMapSceneDrawer SectorMapDrawer;

		public SectorMapLabelDrawer SectorMapLabelDrawer;

		public Text TitleLabel;

		public SectorMap SectorMap;

		public SectorMapZoomer Zoomer;

		public bool AllowSelectionConfirm;

		public Action<SectorMapForm, SectorMapSelectionItem> SelectedCallback;

		public float NextMapAutoRebuildTime = float.MaxValue;

		public Sector Sector
		{
			get
			{
				return sector;
			}
			set
			{
				if (sector != value)
				{
					sector = value;
					if (SectorMapDrawer != null)
					{
						SectorMapDrawer.Sector = Sector;
					}
					if (SectorMap != null)
					{
						SectorMap.Sector = sector;
						SectorMap.OnSectorChanged();
					}
					if (sector != null)
					{
						SetTitleToSectorName();
					}
				}
			}
		}

		public string Title
		{
			get
			{
				return TitleLabel.text;
			}
			set
			{
				TitleLabel.text = value;
			}
		}

		private void Awake()
		{
			CenterOnPlayerButton.onClick.AddListener(SectorMap.CenterOnLocalUnit);
			ConfirmSelectionButton.onClick.AddListener(ConfirmSelectionButtonClicked);
			SectorMap.SectorMapCurrentTargetController.SelectedObjectChanged += SectorMapCurrentTargetController_SelectedObjectChanged;
		}

		private void SectorMapCurrentTargetController_SelectedObjectChanged(SectorMapCurrentTargetController sender, SectorMapSelectionItem? newItem)
		{
			SectorMapDrawer.RefreshUnitLabels();
		}

		public bool CanCenterOnLocalUnit()
		{
			Unit localUnit = EngineASX.Instance.LocalUnit;
			if (localUnit != null)
			{
				return localUnit.Sector == sector;
			}
			return false;
		}

		public void Refresh()
		{
			if (sector != null)
			{
				Redraw();
				SectorMap.Refresh();
			}
		}

		public void Redraw()
		{
			if (SectorMapDrawer != null)
			{
				SectorMapDrawer.RebuildMap();
			}
		}

		private void ConfirmSelectionButtonClicked()
		{
			if (AllowSelectionConfirm && SectorMap.SelectedObject.HasValue)
			{
				RaiseSelectionConfirmedEvent();
			}
		}

		private void RaiseSelectionConfirmedEvent()
		{
			if (SelectedCallback != null)
			{
				SelectedCallback(this, SectorMap.SelectedObject.Value);
			}
		}

		public void Tick()
		{
			Zoomer.Tick();
			SectorMap.UpdateMapSize();
			SectorMapDrawer.Tick();
			SectorMap.RepositionMapItems();
			if (SectorMap.SectorMapCurrentTargetController != null)
			{
				SectorMap.SectorMapCurrentTargetController.Tick();
			}
			RefreshButtons();
			if (AutoRefresh && sector != null && Time.realtimeSinceStartup > NextMapAutoRebuildTime)
			{
				SectorMapDrawer.RebuildMap();
				SetNextMapAutoRebuildTime();
			}
			SectorMapLabelDrawer.Tick();
		}

		public void SetNextMapAutoRebuildTime()
		{
			NextMapAutoRebuildTime = Time.realtimeSinceStartup + TimeBetweenUnitRefresh;
		}

		private void RefreshButtons()
		{
			if (CenterOnPlayerButton != null)
			{
				CenterOnPlayerButton.interactable = CanCenterOnLocalUnit();
			}
			ConfirmSelectionButton.gameObject.SetActive(AllowSelectionConfirm && SectorMap.SelectedObject.HasValue);
		}

		public void SetTitleToSectorName()
		{
			Title = $"Sector Map - {Sector.Name}";
		}

		public void SelectAndCenterOnUnit(Unit unit)
		{
			SectorMap.SectorMapCurrentTargetController.TrySelect(SectorMapSelectionItem.FromUnit(unit));
			SectorMap.CenterOnUnit(unit);
		}
	}
}
