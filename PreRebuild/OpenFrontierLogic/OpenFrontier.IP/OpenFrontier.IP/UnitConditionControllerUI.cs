using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Settings;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace OpenFrontier.IP
{
	public class UnitConditionControllerUI : MonoBehaviour
	{
		public bool RevertHullIconToRenderSprite = true;

		public Color DamageShieldFlashColor = Color.yellow;

		private bool flashHullEnabled;

		private float flashHullStartTime;

		private bool[] flashShieldEnabled = new bool[6];

		private float[] flashShieldStartTimes = new float[6];

		public Image HullWidget;

		private float? lastKnownHullPoints;

		private float?[] lastKnownShieldPoints = new float?[6];

		private bool? lastShieldsEnabled;

		private float? lastTotalShieldPoints;

		[SerializeField]
		protected Unit localUnit;

		private bool localUnitHasShields;

		public Transform ShieldBarBearingTransform;

		public Image[] ShieldBarWidgets = new Image[6];

		private Unit targetUnit;

		[FormerlySerializedAs("UpdateHullWidgetTextureName")]
		public bool SetHullIconSprite;

		public bool RefreshOnEnable = true;

		public TimeMode TimeMode = TimeMode.Real;

		public DamageFlashSettings DamageFlashSettings => GameController.Instance.GameSettings.DamageFlashSettings;

		public Unit LocalUnit
		{
			get
			{
				return localUnit;
			}
			set
			{
				if (!(localUnit != value))
				{
					return;
				}
				localUnit = value;
				ClearLastKnownShipData();
				StopAllShieldFlashes();
				flashHullEnabled = false;
				if (!(localUnit != null) || !localUnit.IsValid)
				{
					return;
				}
				localUnitHasShields = LocalUnit.Components != null && localUnit.Components.ShieldComponent != null;
				RefreshShieldWidgetsActive();
				if (localUnitHasShields)
				{
					UpdateLastKnownShieldPoints();
					SetShieldWidgetsColor();
				}
				UpdateLastKnownHullPoints();
				if (SetHullIconSprite)
				{
					if (RevertHullIconToRenderSprite)
					{
						HullWidget.sprite = EngineASX.Instance.EngineResources.GetUnitThumbnailSpriteOrCargoClassSprite(localUnit);
					}
					else
					{
						HullWidget.sprite = localUnit.UnitClass.GetThumbnailIconSprite();
					}
				}
				if (localUnit.UnitClass.DisplayData != null && localUnit.UnitClass.DisplayData.ForceThumbnailIconSpriteColor)
				{
					HullWidget.color = localUnit.UnitClass.DisplayData.ThumbnailIconSpriteColor;
				}
				else if (localUnit.UnitClass.ApplyThumbnailHullColor || !RevertHullIconToRenderSprite)
				{
					SetHullWidgetColor();
				}
				else
				{
					HullWidget.color = Color.white;
				}
			}
		}

		public Unit TargetUnit
		{
			get
			{
				return targetUnit;
			}
			set
			{
				if (targetUnit != value)
				{
					targetUnit = value;
					SetBearingBarActive();
				}
			}
		}

		private bool TargetUnitHasShield
		{
			get
			{
				if (localUnit.Components != null)
				{
					return localUnit.Components.ShieldComponent != null;
				}
				return false;
			}
		}

		public float GetTime()
		{
			if (TimeMode == TimeMode.Real)
			{
				return RealTime.time;
			}
			return Time.time;
		}

		private void Awake()
		{
			SetBearingBarActive();
			SetShieldWidgetsEnabled(enabled: false);
		}

		public void ClearLastKnownShipData()
		{
			lastKnownHullPoints = null;
			lastShieldsEnabled = null;
			lastTotalShieldPoints = null;
			for (int i = 0; i < 6; i++)
			{
				lastKnownShieldPoints[i] = null;
			}
		}

		public void RefreshShieldWidgetsActive()
		{
			SetBearingBarActive();
			SetShieldWidgetsEnabled(localUnitHasShields);
		}

		private void SetShieldWidgetsEnabled(bool enabled)
		{
			for (int i = 0; i < 6; i++)
			{
				if (i < ShieldBarWidgets.Length && ShieldBarWidgets[i] != null)
				{
					ShieldBarWidgets[i].enabled = enabled;
				}
			}
		}

		private void Start()
		{
		}

		private void Update()
		{
			if (RefreshOnEnable)
			{
				Tick();
			}
		}

		private void OnDisable()
		{
			CleanupOnDisable();
		}

		public void CleanupOnDisable()
		{
			ClearLastKnownShipData();
			StopFlashHull();
			StopFlashShields();
		}

		private void StopFlashShields()
		{
			for (int i = 0; i < 6; i++)
			{
				StopFlashShield(i);
			}
		}

		public virtual void Tick()
		{
			if (!(localUnit != null) || !localUnit.IsValid)
			{
				return;
			}
			localUnitHasShields = LocalUnit.Components != null && localUnit.Components.ShieldComponent != null;
			RefreshShieldWidgetsActive();
			if ((localUnit.UnitClass.ApplyThumbnailHullColor || !RevertHullIconToRenderSprite) && localUnit.Destructable != null)
			{
				float currentHealth = localUnit.Destructable.CurrentHealth;
				bool flag = currentHealth != lastKnownHullPoints;
				if (flag)
				{
					if (currentHealth < lastKnownHullPoints)
					{
						StartFlashHull();
					}
					UpdateLastKnownHullPoints();
				}
				if (flag || flashHullEnabled)
				{
					SetHullWidgetColor();
				}
			}
			if (flashHullEnabled && GetTime() - flashHullStartTime > DamageFlashSettings.FlashDuration)
			{
				StopFlashHull();
				SetHullWidgetColor();
			}
			if (!(LocalUnit.Components != null))
			{
				return;
			}
			ShieldComponent shieldComponent = LocalUnit.Components.ShieldComponent;
			bool flag2 = shieldComponent != null && localUnit.ShieldEnabled;
			if (flag2 != lastShieldsEnabled)
			{
				if (!flag2)
				{
					StopAllShieldFlashes();
					SetShieldWidgetsColor(localUnit.Engine.NoShieldColor);
				}
				else
				{
					UpdateShieldWidgetColors(forceUpdate: true);
				}
				lastShieldsEnabled = flag2;
			}
			if (shieldComponent != null && lastShieldsEnabled == true && shieldComponent.CurrentTotalShieldPoints != lastTotalShieldPoints)
			{
				UpdateShieldWidgetColors(forceUpdate: false);
				UpdateLastKnownShieldPoints();
			}
			RepositionTargetShieldBearing();
		}

		protected virtual void RepositionTargetShieldBearing()
		{
			if (ShieldBarBearingTransform != null && TargetUnit != null)
			{
				float z = (float)(-localUnit.GetShieldIndex(TargetUnit.transform.position)) * 60f;
				ShieldBarBearingTransform.transform.localEulerAngles = new Vector3(0f, 0f, z);
			}
		}

		private void UpdateShieldWidgetColors(bool forceUpdate)
		{
			for (int i = 0; i < 6; i++)
			{
				float shieldPoints = localUnit.Components.ShieldComponent.GetShieldPoints(i);
				if (forceUpdate || shieldPoints != lastKnownShieldPoints[i] || flashShieldEnabled[i])
				{
					if (shieldPoints < lastKnownShieldPoints[i])
					{
						StartFlashShield(i);
					}
					if (flashShieldEnabled[i] && GetTime() - flashShieldStartTimes[i] > DamageFlashSettings.FlashDuration)
					{
						StopFlashShield(i);
					}
					if (i < ShieldBarWidgets.Length && ShieldBarWidgets[i] != null)
					{
						SetShieldWidgetColor(i);
					}
				}
			}
		}

		private void UpdateLastKnownShieldPoints()
		{
			bool shieldEnabled = localUnit.ShieldEnabled;
			for (int i = 0; i < 6; i++)
			{
				lastKnownShieldPoints[i] = (shieldEnabled ? localUnit.Components.ShieldComponent.GetShieldPoints(i) : 0f);
			}
		}

		private void StartFlashHull()
		{
			flashHullEnabled = true;
			flashHullStartTime = GetTime();
		}

		private void StopFlashHull()
		{
			flashHullEnabled = false;
		}

		private void StartFlashShield(int index)
		{
			flashShieldStartTimes[index] = GetTime();
			flashShieldEnabled[index] = true;
		}

		private void StopFlashShield(int index)
		{
			flashShieldEnabled[index] = false;
		}

		private void StopAllShieldFlashes()
		{
			for (int i = 0; i < 6; i++)
			{
				StopFlashShield(i);
			}
		}

		private void UpdateLastKnownHullPoints()
		{
			if (localUnit.Destructable != null)
			{
				lastKnownHullPoints = localUnit.Destructable.CurrentHealth;
			}
			else
			{
				lastKnownHullPoints = 0f;
			}
		}

		private void SetShieldWidgetColor(int index, Color color)
		{
			ShieldBarWidgets[index].color = color;
		}

		private void SetHullWidgetColor()
		{
			if (flashHullEnabled && Mathf.Sin((GetTime() - flashHullStartTime) * DamageFlashSettings.FlashRate) > DamageFlashSettings.FlashSineValue)
			{
				HullWidget.color = DamageShieldFlashColor;
			}
			else
			{
				HullWidget.color = LocalUnit.Engine.GetUnitHullColor(LocalUnit);
			}
		}

		private void SetShieldWidgetsColor()
		{
			for (int i = 0; i < 6; i++)
			{
				SetShieldWidgetColor(i);
			}
		}

		private void SetShieldWidgetColor(int index)
		{
			if (flashShieldEnabled[index] && Mathf.Sin((GetTime() - flashShieldStartTimes[index]) * DamageFlashSettings.FlashRate) > DamageFlashSettings.FlashSineValue)
			{
				ShieldBarWidgets[index].color = DamageShieldFlashColor;
			}
			else
			{
				ShieldBarWidgets[index].color = localUnit.Engine.GetUnitShieldColor(localUnit, index);
			}
		}

		private void SetShieldWidgetsColor(Color color)
		{
			for (int i = 0; i < 6; i++)
			{
				if (i < ShieldBarWidgets.Length && ShieldBarWidgets[i] != null)
				{
					ShieldBarWidgets[i].color = color;
				}
			}
		}

		private void SetBearingBarActive()
		{
			if (ShieldBarBearingTransform != null)
			{
				ShieldBarBearingTransform.gameObject.SetActive(localUnit != null && targetUnit != null && localUnitHasShields);
			}
		}
	}
}
