using System;
using OpenFrontier.IP.Engine;
using TMPro;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class MessageUI : EngineScreen
	{
		public UnitContextButton SenderContextButton;

		public UnitContextButton SubjectContextButton;

		public static PlayerActiveMessage ShowMessage;

		public Button DeleteMessageButton;

		[NonSerialized]
		public PlayerActiveMessage Message;

		public Text MessageFromLabel;

		public TextMeshProUGUI MessageSubjectLabel;

		public MessagesUI MessagesUI;

		public Text MessageTextLabel;

		public Text MessageToLabel;

		public Text DateLabel;

		public Button NextMessageButton;

		public Button PreviousMessageButton;

		public Button TargetSenderButton;

		public Button TargetSubjectButton;

		public Button ViewSenderButton;

		public Button ViewSubjectButton;

		protected override void awake()
		{
			base.awake();
			TargetSenderButton.onClick.AddListener(TargetSenderButton_Activated);
			TargetSubjectButton.onClick.AddListener(TargetSubjectButton_Activated);
			ViewSenderButton.onClick.AddListener(ViewSenderButton_Activated);
			ViewSubjectButton.onClick.AddListener(ViewSubjectButton_Activated);
			if (NextMessageButton != null)
			{
				NextMessageButton.onClick.AddListener(NextMessageButton_Activated);
			}
			if (PreviousMessageButton != null)
			{
				PreviousMessageButton.onClick.AddListener(PreviousMessageButton_Activated);
			}
			DeleteMessageButton.onClick.AddListener(DeleteMessageButton_Activated);
		}

		protected override void refresh()
		{
			base.refresh();
			CheckNewItem();
			if (Message != null)
			{
				MessageToLabel.text = Message.GetFriendlyToText();
				MessageTextLabel.text = Message.GetMessageText();
				MessageFromLabel.text = Message.GetFriendlyFromText();
				MessageSubjectLabel.text = Message.GetFriendlySubjectText();
				TargetSenderButton.gameObject.SetActive(CanTargetSender());
				TargetSubjectButton.gameObject.SetActive(CanTargetSubject());
				ViewSenderButton.gameObject.SetActive(CanViewSender());
				ViewSubjectButton.gameObject.SetActive(CanViewSubject());
				SenderContextButton.SetUnit(Message.SenderUnit);
				SubjectContextButton.SetUnit(Message.SubjectUnit);
				DeleteMessageButton.gameObject.SetActive(Message.AllowDelete);
				DateLabel.text = EngineASX.Instance.DateTimeUtils.GetFormattedGameWorldDateAndAgeFromRealElapsedSeconds(Message.EngineTimeStamp);
				Message.Opened = true;
			}
		}

		protected override void update()
		{
			base.update();
			CheckNewItem();
			if (MessagesUI != null && !MessagesUI.gameObject.activeInHierarchy)
			{
				MessagesUI.Refresh();
			}
			bool interactable = MessagesUI != null && MessagesUI.ActiveItems.Count > 1;
			if (NextMessageButton != null)
			{
				NextMessageButton.interactable = interactable;
			}
			if (PreviousMessageButton != null)
			{
				PreviousMessageButton.interactable = interactable;
			}
		}

		private void DeleteMessageButton_Activated()
		{
			Eng.LocalPlayer.RemoveMessage(Message);
			NavigateBack();
		}

		private void PreviousMessageButton_Activated()
		{
			SwitchMessage(-1);
		}

		private void NextMessageButton_Activated()
		{
			SwitchMessage(1);
		}

		private void SwitchMessage(int change)
		{
			if (MessagesUI != null)
			{
				Message = MessagesUI.SwitchItem(Message, -1);
				Refresh();
			}
		}

		private void TargetSubjectButton_Activated()
		{
			if (CanTargetSubject())
			{
				if (Message.SubjectUnit != null && Message.SubjectUnit.IsValidAndNotDestroyed && EngineASX.Instance.IsUnitDiscoveredByLocalFaction(Message.SubjectUnit))
				{
					Eng.LocalPlayer.SetCustomWaypointToUnit(Message.SubjectUnit);
				}
				else
				{
					Eng.LocalPlayer.SetCustomWaypointToSectorPosition(Message.SubjectUnitSector, Message.SubjectUnitSectorPosition, autoRemove: false);
				}
			}
		}

		private void TargetSenderButton_Activated()
		{
			if (CanTargetSender())
			{
				if (Message.SenderUnit != null && Message.SenderUnit.IsValidAndNotDestroyed && EngineASX.Instance.IsUnitDiscoveredByLocalFaction(Message.SenderUnit))
				{
					Eng.LocalPlayer.SetCustomWaypointToUnit(Message.SenderUnit);
				}
				else
				{
					Eng.LocalPlayer.SetCustomWaypointToSectorPosition(Message.SenderUnitSector, Message.SenderUnitSectorPosition, autoRemove: false);
				}
			}
		}

		private void ViewSubjectButton_Activated()
		{
			if (CanViewSubject())
			{
				if (Message.SubjectUnit != null && Message.SubjectUnit.IsValidAndNotDestroyed)
				{
					UIController.Instance.ScreenNavigator.ShowSectorMapScreenForUnitRespectingIntel(Message.SubjectUnit, Message.SubjectUnitSector, Message.SubjectUnitSectorPosition);
				}
				else
				{
					UIController.Instance.ScreenNavigator.ShowSectorMapScreenShowingWorldPosition(Message.SubjectUnitSector, Message.SubjectUnitSector.transform.position + Message.SubjectUnitSectorPosition);
				}
			}
		}

		private void ViewSenderButton_Activated()
		{
			if (CanViewSender())
			{
				if (Message.SenderUnit != null && Message.SenderUnit.IsValidAndNotDestroyed)
				{
					UIController.Instance.ScreenNavigator.ShowSectorMapScreenForUnitRespectingIntel(Message.SenderUnit, Message.SenderUnitSector, Message.SenderUnitSectorPosition);
				}
				else
				{
					UIController.Instance.ScreenNavigator.ShowSectorMapScreenShowingWorldPosition(Message.SenderUnitSector, Message.SenderUnitSector.transform.position + Message.SenderUnitSectorPosition);
				}
			}
		}

		public bool CanViewSender()
		{
			return Message.SenderUnitSector != null;
		}

		public bool CanTargetSender()
		{
			return Message.SenderUnitSector != null;
		}

		public bool CanViewSubject()
		{
			if (Message.SubjectUnit == null || !Message.SubjectUnit.IsValidAndNotDestroyed)
			{
				return Message.SubjectUnitSector != null;
			}
			return false;
		}

		public bool CanTargetSubject()
		{
			if (Message.SubjectUnit == null || !Message.SubjectUnit.IsValidAndNotDestroyed)
			{
				return Message.SubjectUnitSector != null;
			}
			return false;
		}

		private void CheckNewItem()
		{
			if (ShowMessage != null)
			{
				Message = ShowMessage;
				ShowMessage = null;
				Refresh();
			}
		}
	}
}
