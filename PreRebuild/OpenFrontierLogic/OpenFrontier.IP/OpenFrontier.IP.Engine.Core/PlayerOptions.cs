using UnityEngine;

namespace OpenFrontier.IP.Engine.Core
{
	public class PlayerOptions : MonoBehaviour
	{
		public bool UI_AutoHideShields = true;

		public bool UI_ScreenSpaceShields;

		public bool UI_ShowPointDefenceTurrets;

		public bool General_AllowFireAtNeutral = true;

		public bool General_AllowFireAtAllied;

		/// <summary>
		/// Whether this installation takes part in multiplayer at all.
		/// </summary>
		/// <remarks>
		/// Off by default: hosting opens a port on the player's machine and
		/// makes them reachable, which should be a deliberate act rather than
		/// something a fresh install does on its own.
		/// </remarks>
		public bool General_EnableMultiplayer;

		public bool Video_WormholeAnimationEnabled = true;

		public bool General_CameraShakeOnShieldHit;

		public bool General_CameraShakeOnHullHit = true;

		public bool UI_ShowHudHeader = true;

		public bool UI_ShowHudComponents = true;

		public bool UI_ShowHudTargetPanel = true;

		public bool UI_ShowHudControls = true;

		public bool UI_ShowHudIndicators = true;

		public bool UI_ShowHudRadar = true;

		public bool UI_ShowHudTargets = true;

		public bool UI_ShowHudMenu = true;

		public bool UI_ShowHudDialog = true;

		public bool UI_ShowHudTargetIndicators = true;

		public bool Audio_MissileLockSound = true;
	}
}
