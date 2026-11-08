using OpenFrontier.IP.UI.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class RollingTextUI : MonoBehaviour
	{
		public enum ShowMessageState
		{
			None,
			Building,
			Show,
			FadeOut
		}

		public delegate void StateChangedHandler(RollingTextUI sender, ShowMessageState oldState);

		public AudioClip AudioClip;

		public AudioSource AudioSource;

		public float CharsPerSecond = 20f;

		private double curExpiryTime;

		private int currentCharCount;

		[SerializeField]
		private string currentText;

		public float FadeOutTime = 1f;

		public Text Label;

		private double lastLetterPrint;

		public bool PlayOnAwake;

		public bool ShowExpiry = true;

		public double ShowTime = 3.0;

		private ShowMessageState state;

		public bool UseRealTime = true;

		public ShowMessageState State
		{
			get
			{
				return state;
			}
			private set
			{
				if (state != value)
				{
					ShowMessageState oldState = state;
					state = value;
					switch (state)
					{
					case ShowMessageState.None:
						Label.text = null;
						currentCharCount = 0;
						break;
					case ShowMessageState.FadeOut:
						curExpiryTime = CurrentGameTime() + (double)FadeOutTime;
						break;
					case ShowMessageState.Building:
						lastLetterPrint = CurrentGameTime();
						Label.SetAlpha(1f);
						currentCharCount = 0;
						break;
					case ShowMessageState.Show:
						curExpiryTime = CurrentGameTime() + ShowTime;
						break;
					}
					Label.gameObject.SetActive(state != ShowMessageState.None);
					if (StateChanged != null)
					{
						StateChanged(this, oldState);
					}
				}
			}
		}

		public event StateChangedHandler StateChanged;

		private double CurrentGameTime()
		{
			if (!UseRealTime)
			{
				return Time.timeAsDouble;
			}
			return Time.realtimeSinceStartupAsDouble;
		}

		public void Clear()
		{
			State = ShowMessageState.None;
			Label.text = null;
		}

		public void SetText(string text)
		{
			Clear();
			if (!string.IsNullOrEmpty(text))
			{
				currentText = text;
				State = ShowMessageState.Building;
			}
		}

		public void FinishBuilding()
		{
			currentCharCount = currentText.Length;
			State = ShowMessageState.Show;
			SetLabelText();
		}

		public void SetLabelText()
		{
			if (currentCharCount <= currentText.Length)
			{
				Label.text = currentText.Substring(0, currentCharCount);
			}
		}

		private void Awake()
		{
			if (Label == null)
			{
				Debug.LogWarning("RollingTextUI missing Label Component", this);
			}
			if (PlayOnAwake)
			{
				SetText(currentText);
			}
		}

		private void Update()
		{
			switch (state)
			{
			case ShowMessageState.FadeOut:
			{
				if (CurrentGameTime() > curExpiryTime)
				{
					State = ShowMessageState.None;
					break;
				}
				float num4 = (float)(CurrentGameTime() - (curExpiryTime - (double)FadeOutTime));
				Label.SetAlpha(1f - num4 / FadeOutTime);
				break;
			}
			case ShowMessageState.Show:
				if (ShowExpiry && CurrentGameTime() > curExpiryTime)
				{
					State = ShowMessageState.FadeOut;
				}
				break;
			case ShowMessageState.Building:
			{
				float num = (float)(CurrentGameTime() - lastLetterPrint);
				float num2 = 1f / CharsPerSecond;
				int num3 = Mathf.FloorToInt(num / num2);
				if (num3 > 0)
				{
					lastLetterPrint = CurrentGameTime();
					currentCharCount += num3;
					if (currentCharCount > currentText.Length)
					{
						FinishBuilding();
					}
					else
					{
						SetLabelText();
					}
					if (AudioClip != null)
					{
						AudioHelper.PlaySound(AudioClip);
					}
					else if (AudioSource != null)
					{
						AudioSource.Play();
					}
				}
				break;
			}
			}
		}
	}
}
