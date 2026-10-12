using System;
using System.Collections.Generic;
using DigitalRubyShared;
using OpenFrontier.IP.UI;
using UnityEngine;
using Input = OpenFrontier.LegacyInput;

namespace OpenFrontier.IP.Engine
{
	public class CinematicScreen : EngineScreen
	{
		public enum IntroState
		{
			Stopped,
			Playing,
			Waiting,
			BuildingMsg
		}

		public enum SkipWithMouseMode
		{
			None,
			Finish,
			NextMessage
		}

		public delegate void FinishedHandler(CinematicScreen sender);

		[Serializable]
		public class IntroCinematicMsg
		{
			public string Message;

			public float ShowDuration = 3f;

			public float WaitTime = 3f;
		}

		public bool AutoPlay;

		public bool ClearOnStop = true;

		public bool UseRealTime = true;

		private IntroCinematicMsg CurrentMessage;

		public float DefaultShowTime = 3f;

		public float DefaultWaitTime = 3f;

		public List<IntroCinematicMsg> MsgQueue = new List<IntroCinematicMsg>();

		private double playTime;

		public bool FinishWhenNoMessagesLeft = true;

		public RollingTextUI RollingTextUI;

		public SkipWithMouseMode SkipWithMouse = SkipWithMouseMode.Finish;

		private IntroState state;

		private double waitExpiryTime;

		private TapGestureRecognizer tapGesture;

		public bool IsPlaying => state != IntroState.Stopped;

		public IntroState State
		{
			get
			{
				return state;
			}
			set
			{
				if (state == value)
				{
					return;
				}
				state = value;
				switch (state)
				{
				case IntroState.Stopped:
					if (ClearOnStop)
					{
						RollingTextUI.Clear();
					}
					break;
				case IntroState.Playing:
					RollingTextUI.Clear();
					playTime = CurrentGameTime();
					break;
				case IntroState.Waiting:
					RollingTextUI.Clear();
					waitExpiryTime = CurrentGameTime() + (double)CurrentMessage.WaitTime;
					break;
				case IntroState.BuildingMsg:
					RollingTextUI.Clear();
					RollingTextUI.SetText(CurrentMessage.Message);
					break;
				}
			}
		}

		public event FinishedHandler Finished;

		protected override void onEnable()
		{
			base.onEnable();
			tapGesture = new TapGestureRecognizer
			{
				MaximumNumberOfTouchesToTrack = 1
			};
			tapGesture.StateUpdated += TapGesture_StateUpdated;
			FingersScript.Instance.AddGesture(tapGesture);
		}

		protected override void onDisable()
		{
			base.onDisable();
			FingersScript.Instance.RemoveGesture(tapGesture);
		}

		private void TapGesture_StateUpdated(GestureRecognizer gesture)
		{
			if (gesture.State == GestureRecognizerState.Ended)
			{
				TrySkipWithTap();
			}
		}

		private double CurrentGameTime()
		{
			if (!UseRealTime)
			{
				return Time.timeAsDouble;
			}
			return Time.realtimeSinceStartupAsDouble;
		}

		public void Play()
		{
			State = IntroState.Playing;
		}

		public void EnqueueSceneName()
		{
			if (Eng.ActiveSector != null)
			{
				EnqueueMessage($" {Eng.ActiveSector.Name} Sector", DefaultShowTime);
			}
			else
			{
				Debug.LogWarning("Null scene", this);
			}
		}

		public void EnqueueShipClass(bool getRoot)
		{
			Unit unit = Eng.PlayerUnit;
			if (unit != null)
			{
				if (getRoot)
				{
					unit = unit.GetRootUnit();
				}
				if (unit.UnitClass.UnitType != UnitType.Station)
				{
					string message = $"{unit.UnitClass.GetClassAndSeriesName()} class {unit.UnitClass.HullType}";
					EnqueueMessage(message, DefaultShowTime);
				}
			}
		}

		public void EnqueueMessage(string message, float duration)
		{
			MsgQueue.Add(new IntroCinematicMsg
			{
				Message = message,
				WaitTime = DefaultWaitTime,
				ShowDuration = duration
			});
		}

		public void EnqueuePlayerUnitName(bool getRoot)
		{
			Unit unit = Eng.PlayerUnit;
			if (!(unit != null))
			{
				return;
			}
			if (getRoot)
			{
				unit = unit.GetRootUnit();
			}
			if (unit.IsStationOrShip())
			{
				if (unit.UnitType == UnitType.Station)
				{
					EnqueueMessage($"\"{unit.GetDesignationOrClassAndSeries()}\"", DefaultShowTime);
				}
				else
				{
					EnqueueMessage($"\"{unit.Components.ShipName}\"", DefaultShowTime);
				}
			}
		}

		public void ClearMsgQueue()
		{
			MsgQueue.Clear();
		}

		protected override void update()
		{
			base.update();
			switch (state)
			{
			case IntroState.Stopped:
				if (AutoPlay && MsgQueue.Count > 0)
				{
					Play();
				}
				break;
			case IntroState.Playing:
				if (MsgQueue.Count > 0)
				{
					TakeNextMessage();
				}
				else if (FinishWhenNoMessagesLeft)
				{
					Finish();
				}
				break;
			case IntroState.Waiting:
				if (CurrentGameTime() > waitExpiryTime)
				{
					State = IntroState.BuildingMsg;
				}
				break;
			case IntroState.BuildingMsg:
				if (RollingTextUI.State == RollingTextUI.ShowMessageState.None)
				{
					State = IntroState.Playing;
				}
				break;
			}
			if (IsCurrentScreen && Input.GetKeyDown(KeyCode.Escape))
			{
				TrySkipWithTap();
			}
		}

		private void TrySkipWithTap()
		{
			if (state == IntroState.Stopped || SkipWithMouse == SkipWithMouseMode.None || !(CurrentGameTime() - playTime > 1.0))
			{
				return;
			}
			switch (SkipWithMouse)
			{
			case SkipWithMouseMode.Finish:
				Finish();
				break;
			case SkipWithMouseMode.NextMessage:
				switch (state)
				{
				case IntroState.Waiting:
					waitExpiryTime = 0.0;
					break;
				case IntroState.BuildingMsg:
					RollingTextUI.FinishBuilding();
					break;
				}
				break;
			}
		}

		private void TakeNextMessage()
		{
			CurrentMessage = MsgQueue[0];
			MsgQueue.RemoveAt(0);
			RollingTextUI.ShowTime = CurrentMessage.ShowDuration;
			State = IntroState.Waiting;
		}

		private void Finish()
		{
			State = IntroState.Stopped;
			if (Finished != null)
			{
				Finished(this);
			}
		}
	}
}
