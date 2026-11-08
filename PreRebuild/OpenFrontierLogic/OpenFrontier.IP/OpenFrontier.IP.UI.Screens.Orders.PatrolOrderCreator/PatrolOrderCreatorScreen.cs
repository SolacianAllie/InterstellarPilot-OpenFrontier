using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens.MessageBox;
using OpenFrontier.IP.UI.Screens.SectorMap;
using OpenFrontier.IP.UI.Screens.UniverseMap;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.Orders.PatrolOrderCreator
{
	public class PatrolOrderCreatorScreen : EngineScreen
	{
		public delegate void OnPatrolRouteConfirmedHandler(IEnumerable<SectorTarget> sectorTargets, bool isLoop, bool repeat);

		public Button UniverseMapButton;

		public SectorMapForm SectorMapForm;

		public PatrolOrderCreatorForm PatrolOrderCreatorForm;

		public SectorMapWaypointItem SectorMapWaypointItemPrefab;

		public bool IsLoop => PatrolOrderCreatorForm.IsLoopToggle.isOn;

		public bool Repeat => PatrolOrderCreatorForm.RepeatToggle.isOn;

		public event OnPatrolRouteConfirmedHandler OnPatrolRouteConfirmed;

		protected override void awake()
		{
			base.awake();
			PatrolOrderCreatorForm.ConfirmButton.onClick.AddListener(ConfirmButtonClick);
			SectorMapForm.SectorMap.CustomSelectionFilter = (SectorMapSelectionItem item) => item.Unit == null;
			SectorMapForm.SectorMap.SectorMapCurrentTargetController.OnSelecting = (SectorMapSelectionItem SectorMapSelectionItem) =>
			{
				if (PatrolOrderCreatorForm.PatrolOrderCreatorList.ActiveItems.Count >= 64)
				{
					UIController.Instance.ShowMessageBox("Maximum number of waypoints reached", MessageBoxButtons.Ok, null, MessageBoxIcon.Warning);
				}
				else
				{
					AddPatrolNode(SectorTarget.FromSectorPosition(SectorMapForm.Sector, SectorMapSelectionItem.SectorPosition));
				}
				return false;
			};
			PatrolOrderCreatorForm.PatrolOrderCreatorList.DeletePatrolNodeEvent += PatrolOrderCreatorList_DeletePatrolNodeEvent;
			UniverseMapButton.onClick.AddListener(UniverseMapButtonClick);
			PatrolOrderCreatorForm.IsLoopToggle.onValueChanged.AddListener(IsLoopToggleValueChanged);
		}

		public SectorMapWaypointItem CreateSectorMapWaypointItemWidget(SectorTarget sectorTarget, SectorTarget previousNodeTarget, SectorTarget nextNodeTarget, int nodeIndex, bool isLastNode)
		{
			SectorMapWaypointItem sectorMapWaypointItem = SectorMapForm.SectorMap.CreateAndActivateItemWidget(SectorMapWaypointItemPrefab, SectorMapForm.SectorMap.OverlayRoot);
			sectorMapWaypointItem.SectorTarget = sectorTarget;
			sectorMapWaypointItem.PreviousNodeSectorTarget = previousNodeTarget;
			sectorMapWaypointItem.NextNodeSectorTarget = nextNodeTarget;
			sectorMapWaypointItem.NodeIndex = nodeIndex;
			sectorMapWaypointItem.IsLastNode = isLastNode;
			sectorMapWaypointItem.Refresh();
			return sectorMapWaypointItem;
		}

		private void PatrolOrderCreatorList_DeletePatrolNodeEvent(PatrolOrderCreatorListItem sender, SectorTarget sectorTarget)
		{
			RemovePatrolNode(sectorTarget);
		}

		protected override void update()
		{
			base.update();
			PatrolOrderCreatorForm.ConfirmButton.interactable = PatrolOrderCreatorForm.PatrolOrderCreatorList.ActiveItems.Count > 1;
			PatrolOrderCreatorForm.IsLoopToggle.interactable = PatrolOrderCreatorForm.PatrolOrderCreatorList.ActiveItems.Count > 1;
			SectorMapForm.Tick();
		}

		private void IsLoopToggleValueChanged(bool value)
		{
			Redraw();
		}

		protected override void refresh()
		{
			base.refresh();
			SectorMapForm.Title = "Create Patrol Order - " + SectorMapForm.Sector.Name;
			SectorMapForm.Refresh();
			PatrolOrderCreatorForm.Refresh();
			Redraw();
		}

		private void ConfirmButtonClick()
		{
			if (OnPatrolRouteConfirmed != null)
			{
				OnPatrolRouteConfirmed(PatrolOrderCreatorForm.PatrolOrderCreatorList.ActiveItems, IsLoop, Repeat);
			}
		}

		public void AddPatrolNode(SectorTarget sectorTarget)
		{
			PatrolOrderCreatorForm.PatrolOrderCreatorList.Add(sectorTarget);
			Redraw();
		}

		public void RemovePatrolNode(SectorTarget sectorTarget)
		{
			List<SectorTarget> list = PatrolOrderCreatorForm.PatrolOrderCreatorList.ActiveItems.ToList();
			list.Remove(sectorTarget);
			PatrolOrderCreatorForm.PatrolOrderCreatorList.SetItems(list);
			Redraw();
		}

		public void Redraw()
		{
			SectorMapForm.SectorMapDrawer.RebuildMap();
			AddPatrolNodeItemsToSectorMap();
		}

		private void AddPatrolNodeItemsToSectorMap()
		{
			List<SectorTarget> activeItems = PatrolOrderCreatorForm.PatrolOrderCreatorList.ActiveItems;
			for (int i = 0; i < activeItems.Count; i++)
			{
				SectorTarget sectorTarget = activeItems[i];
				SectorTarget previousNodeTarget = null;
				SectorTarget nextNodeTarget = null;
				if (i > 0)
				{
					previousNodeTarget = activeItems[i - 1];
				}
				else if (IsLoop && activeItems.Count > 1)
				{
					previousNodeTarget = activeItems[activeItems.Count - 1];
				}
				if (i < activeItems.Count - 1)
				{
					nextNodeTarget = activeItems[i + 1];
				}
				else if (IsLoop && activeItems.Count > 0)
				{
					nextNodeTarget = activeItems[0];
				}
				if (sectorTarget.Sector == SectorMapForm.Sector)
				{
					SectorMapWaypointItem item = CreateSectorMapWaypointItemWidget(sectorTarget, previousNodeTarget, nextNodeTarget, i, i == activeItems.Count - 1);
					SectorMapForm.SectorMap.AddItem(item);
				}
			}
		}

		public void SelectSector(Sector sector)
		{
			PatrolOrderCreatorForm.PatrolOrderCreatorList.CurrentSector = sector;
			SectorMapForm.Sector = sector;
		}

		private void UniverseMapButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowCustomUniverseMapScreen((UniverseMapScreen setup) =>
			{
				setup.Title = "Create Patrol Order - Select sector...";
				setup.ShowDockHeader = false;
				setup.ShowSelectedSectorInfo = false;
				setup.SelectSector(SectorMapForm.Sector);
				setup.CenterOnSector(SectorMapForm.Sector);
				setup.SectorSelectedCallback = (UniverseMapScreen mapScreen, Sector sector) =>
				{
					SelectSector(sector);
					SectorMapForm.SectorMap.CenterOnSectorPosition(Vector3.zero);
					Refresh();
					mapScreen.NavigateBack();
				};
			}, (UniverseMapScreen screen) =>
			{
				foreach (Sector item in PatrolOrderCreatorForm.PatrolOrderCreatorList.ActiveItems.Select((SectorTarget e) => e.Sector).Distinct())
				{
					UniverseMapItemUI sectorItem = screen.GetSectorItem(item);
					if (sectorItem != null)
					{
						sectorItem.PatrolPathCreatorImage.enabled = true;
					}
				}
			});
		}

		protected override bool onNavigatingBack()
		{
			if (PatrolOrderCreatorForm.PatrolOrderCreatorList.ActiveItems.Count == 0)
			{
				return base.onNavigatingBack();
			}
			UIController.Instance.ShowMessageBox("Cancel patrol order creation?", MessageBoxButtons.OkCancel, (MessageBoxScreen screen, MessageBoxResult result) =>
			{
				if (result == MessageBoxResult.Ok)
				{
					UIController.Instance.ScreenNavigator.NavigateBackTo(GetBackTarget());
				}
			}, MessageBoxIcon.Question);
			return false;
		}
	}
}
