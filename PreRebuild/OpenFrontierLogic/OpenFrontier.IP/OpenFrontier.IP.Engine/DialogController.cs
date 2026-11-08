using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine.Comms;
using OpenFrontier.IP.UI;
using UnityEngine;
using UnityEngine.UI;
using Input = OpenFrontier.LegacyInput;

namespace OpenFrontier.IP.Engine
{
	public class DialogController : EngineScreen, ICommsController
	{
		public delegate void CommsStageShownHandler(DialogController sender, ICommsStage commsStage);

		public delegate void CommsStageFinishedHandler(DialogController sender, ICommsStage commsStage);

		public Image ShipIconImage;

		public bool PositionCameraNearCommsHandler;

		private List<ICommsStage> stageHistoryFromCommsHandler = new List<ICommsStage>();

		private ICommsStageOptionGroup optionGroupFilter;

		public float MaxDistanceForCameraMove = 1500f;

		public bool AutoShowStageOptions = true;

		public float CameraMoveLerpRate = 10f;

		public float CameraRotateLerpRate = 30f;

		public float CameraSnapThreshold = 0.2f;

		public Button ConfirmButton;

		public GameObject ConfirmButtonRoot;

		public Text CurrentMessageLabel;

		public DialogItemList DialogItemList;

		public GameObject DialogOptionsRoot;

		private ICommsStage commsStage;

		private List<ICommsStageOption> stageOptions = new List<ICommsStageOption>();

		public Text FromMessageLabel;

		public Text FromFactionLabel;

		private bool hasMovedCameraIntoPosition;

		private bool hasAttemptedToShowStageOptions;

		private float lastRealTime;

		public GameObject MessagesRoot;

		public ICommsStage CommsStage
		{
			get
			{
				return commsStage;
			}
			set
			{
				if (this.commsStage == value)
				{
					return;
				}
				ICommsStage commsStage = this.commsStage;
				ICommsHandler commsHandler = null;
				stageOptions.Clear();
				hasAttemptedToShowStageOptions = false;
				OptionGroupFilter = null;
				hasMovedCameraIntoPosition = false;
				this.commsStage = value;
				RefreshFromMessageLabel();
				RefreshFromIconImage();
				if (commsStage != null)
				{
					commsHandler = commsStage.Handler;
					commsStage.SetVisible(visible: false);
					commsHandler?.Hide();
				}
				if (commsHandler != CommsHandler)
				{
					stageHistoryFromCommsHandler.Clear();
				}
				if (this.commsStage != null)
				{
					stageHistoryFromCommsHandler.Add(this.commsStage);
					if (this.commsStage.Handler == null)
					{
						Debug.LogError("DialogController given stage without Handler", this);
					}
					this.commsStage.SetVisible(visible: true);
					this.commsStage.Handler.Show();
				}
			}
		}

		public ICommsHandler CommsHandler
		{
			get
			{
				if (commsStage != null)
				{
					return commsStage.Handler;
				}
				return null;
			}
		}

		public bool HasAttemptedToShowStageOptions => hasAttemptedToShowStageOptions;

		public ICommsStageOptionGroup OptionGroupFilter
		{
			get
			{
				return optionGroupFilter;
			}
			set
			{
				optionGroupFilter = value;
			}
		}

		public int NumStagesShownFromCommsHandler => stageHistoryFromCommsHandler.Count;

		public event CommsStageShownHandler CommsStageShown;

		public event CommsStageFinishedHandler CommsStageFinished;

		private bool CanShowStageOptions()
		{
			if (stageOptions != null)
			{
				return stageOptions.Count() > 0;
			}
			return false;
		}

		public void ConfirmCommsStage()
		{
			if (!hasAttemptedToShowStageOptions && CanShowStageOptions())
			{
				ShowStageOptions();
			}
			else
			{
				OnCommsStageConfirmed();
			}
		}

		private void ShowStageOptions()
		{
			hasAttemptedToShowStageOptions = true;
			RefreshStageOptions();
		}

		public void RefreshStageOptions()
		{
			IEnumerable<ICommsStageOption> stageOptionsToDisplay = GetStageOptionsToDisplay();
			DialogItemList.SetItems(stageOptionsToDisplay);
			RefreshConfirmButton();
		}

		private IEnumerable<ICommsStageOption> GetStageOptionsToDisplay()
		{
			List<ICommsStageOption> list = stageOptions.Where((ICommsStageOption e) => e.OptionGroup == OptionGroupFilter).ToList();
			if (OptionGroupFilter != null)
			{
				CommsStageOptionTargetGroup item = new CommsStageOptionTargetGroup
				{
					OptionText = "Nevermind",
					OptionGroup = null,
					TargetOptionGroup = null
				};
				list.Insert(0, item);
			}
			else
			{
				foreach (ICommsStageOptionGroup item3 in stageOptions.Select((ICommsStageOption e) => e.OptionGroup).Distinct())
				{
					if (item3 != null)
					{
						CommsStageOptionTargetGroup item2 = new CommsStageOptionTargetGroup
						{
							OptionText = item3.DisplayText,
							OptionGroup = null,
							TargetOptionGroup = item3
						};
						list.Add(item2);
					}
				}
			}
			return list;
		}

		private void RefreshFromIconImage()
		{
			if (GetCommsHandlerCurrentUnit() != null)
			{
				ShipIconImage.sprite = EngineASX.Instance.EngineResources.GetUnitClassRenderSpriteOrDefault(CommsHandler.OwnerPilot.CurrentUnit.UnitClass);
			}
		}

		private Unit GetCommsHandlerCurrentUnit()
		{
			if (CommsHandler != null && CommsHandler.OwnerPilot != null)
			{
				return CommsHandler.OwnerPilot.CurrentUnit;
			}
			return null;
		}

		private void RefreshFromMessageLabel()
		{
			if (commsStage != null)
			{
				Color factionHostilityColor = EngineASX.Instance.GetFactionHostilityColor(CommsHandler.OwnerFaction, EngineASX.Instance.LocalFaction);
				FromMessageLabel.text = GetFormattedCommsHandlerOwner();
				FromFactionLabel.enabled = ShouldShowFactionText();
				if (CommsHandler.OwnerFaction != null)
				{
					FromFactionLabel.color = factionHostilityColor;
					FromFactionLabel.text = CommsHandler.OwnerFaction.GetLongNameElseShort();
				}
				FromMessageLabel.color = factionHostilityColor;
			}
		}

		private bool ShouldShowFactionText()
		{
			if (CommsHandler.OwnerFaction != null)
			{
				return !CommsHandler.OwnerFaction.IsFreelancer;
			}
			return false;
		}

		private void RefreshStageMessage()
		{
			ICommsMessage message = commsStage.Message;
			if (message != null)
			{
				CurrentMessageLabel.text = message.MessageText;
			}
		}

		public void ShowStageMessagesAndOptions()
		{
			if (commsStage != null)
			{
				RefreshStageMessage();
				_ = commsStage.Message;
				if (commsStage.Message == null || AutoShowStageOptions)
				{
					ShowStageOptions();
				}
				RefreshConfirmButton();
			}
		}

		private void RefreshConfirmButton()
		{
			ConfirmButtonRoot.SetActive(!hasAttemptedToShowStageOptions || stageOptions == null || stageOptions.Count() == 0);
		}

		public void InitialiseCommsStage(ICommsStage newDialogStage)
		{
			ICommsStage commsStage = this.commsStage;
			UIController.Instance.ScreenNavigator.NavigateToScreen(this);
			if (this.commsStage != null)
			{
				Eng.TrySetCameraSpectatorEnabled(enabled: false);
				this.commsStage.Show(this);
				stageOptions.Clear();
				stageOptions.AddRange(this.commsStage.GetOptions());
				if (CommsStageShown != null)
				{
					CommsStageShown(this, this.commsStage);
				}
				hasAttemptedToShowStageOptions = false;
				gameObject.SetActive(value: true);
				SetRealTime();
				if (commsStage == null || this.commsStage.Handler != commsStage.Handler)
				{
					hasMovedCameraIntoPosition = false;
					HideMessages();
				}
				else
				{
					ShowStageMessagesAndOptions();
				}
			}
			else
			{
				Debug.LogError("Cannot StartDialog. Null dialog ref", this);
			}
		}

		private void HideMessages()
		{
			MessagesRoot.gameObject.SetActive(value: false);
		}

		protected override void start()
		{
			base.start();
			DialogItemList.ToggleValueOn += DialogItemList_ItemActivated;
			ConfirmButton.onClick.AddListener(ConfirmCommsStage);
		}

		protected override void update()
		{
			base.update();
			RefreshDialogOptionsVisibility();
			if (commsStage != null && !hasMovedCameraIntoPosition)
			{
				OnPositionedCamera();
			}
		}

		private bool TryGetCameraPositionForUnit(Unit unit, out Vector3 targetPosition, out Quaternion targetRotation)
		{
			targetPosition = Vector3.zero;
			targetRotation = Quaternion.identity;
			if (unit.GetRootUnit() != EngineASX.Instance.PlayerRootUnit && unit != null && unit.ActiveUnit != null && MoveCameraToTargetUnit(unit))
			{
				if (unit.ActiveUnit.CommsDialogPositioner != null)
				{
					targetPosition = unit.ActiveUnit.CommsDialogPositioner.transform.position;
					targetRotation = unit.ActiveUnit.CommsDialogPositioner.transform.rotation;
					return true;
				}
				Vector3 vector = Vector3.Normalize(new Vector3(1f, 1f, 1f));
				targetPosition = unit.transform.TransformPoint(vector * unit.UnitClass.DisplayData.Radius * 1.2f);
				targetRotation = Quaternion.LookRotation(unit.transform.position - targetPosition, Vector3.up);
				return true;
			}
			return false;
		}

		private bool MoveCameraToTargetUnit(Unit unit)
		{
			return Vector3.Distance(unit.transform.position, GameController.Instance.MainCamera.transform.position) < MaxDistanceForCameraMove;
		}

		private void RefreshDialogOptionsVisibility()
		{
			if (commsStage != null)
			{
				DialogOptionsRoot.gameObject.SetActive(HasAttemptedToShowStageOptions);
			}
		}

		protected override bool AllowNavigateBack()
		{
			return false;
		}

		private void DialogItemList_ItemActivated(ScrollList<ICommsStageOption> sender, ScrollListItem<ICommsStageOption> optionUI)
		{
			optionUI.Item.Select(this);
		}

		private void SetRealTime()
		{
			lastRealTime = Time.realtimeSinceStartup;
		}

		private void OnPositionedCamera()
		{
			MessagesRoot.gameObject.SetActive(value: true);
			hasMovedCameraIntoPosition = true;
			ShowStageMessagesAndOptions();
		}

		private void OnCommsStageConfirmed()
		{
			ICommsMessage message = this.commsStage.Message;
			if (message != null)
			{
				message.Confirm();
			}
			else
			{
				Debug.LogError($"CommsStage: \"{this.commsStage}\" is missing a message");
			}
			ICommsStage commsStage = this.commsStage;
			this.commsStage.Finish(this);
			if (CommsStageFinished != null)
			{
				CommsStageFinished(this, commsStage);
			}
		}

		private bool SkipDialogInput()
		{
			return Input.GetMouseButton(0);
		}

		private string GetFormattedCommsHandlerOwner()
		{
			Person ownerPilot = CommsHandler.OwnerPilot;
			if (ownerPilot != null)
			{
				if (ownerPilot.Faction != null)
				{
					if (ownerPilot.Faction.AISettings == null || !ownerPilot.Faction.AISettings.PreferSingleShip)
					{
						if (ownerPilot.Faction.LeaderPerson == ownerPilot)
						{
							return $"{ownerPilot.FullNameWithFullRank} (leader of {ownerPilot.Faction.GetLongNameElseShort()})";
						}
						return $"{ownerPilot.FullNameWithFullRank} ({ownerPilot.Faction.GetLongNameElseShort()})";
					}
					return ownerPilot.FullNameWithFullRank;
				}
				return ownerPilot.FullNameWithFullRank;
			}
			return "Unknown";
		}

		public void ChangeStage(ICommsStage dialogStage)
		{
			ChangeStage(dialogStage, pausedGame: true);
		}

		public void Dismiss()
		{
			ChangeStage(null);
		}

		public void ChangeStage(ICommsStage newStage, bool pausedGame)
		{
			CommsStage = newStage;
			if (newStage != null)
			{
				InitialiseCommsStage(newStage);
			}
			else if (IsCurrentScreen)
			{
				NavigateBack();
			}
			if (pausedGame)
			{
				Eng.IsPaused = newStage != null;
			}
		}

		public void GoToHandlerDefaultStage()
		{
			if (CommsHandler != null)
			{
				ChangeStage(CommsHandler.GetStage());
			}
			else
			{
				Debug.LogError("CommsController: Cannot goto initial stage. No CommsHandler", this);
			}
		}
	}
}
