using UnityEngine;

namespace OpenFrontier.IP.Engine.Core
{
	public class PlayerOptionConstants : MonoBehaviour
	{
		public string UI_AutoHideShieldsKey = "ui_autohide_shields";

		public string UI_ScreenSpaceShieldsKey = "ui_screen_space_shields";

		public string UI_ShowPointDefenceTurretsKey = "ui_point_defence_turrets";

		public string UI_ShowHudHeaderKey = "ui_hud_show_header";

		public string UI_ShowHudComponentsKey = "ui_hud_show_components";

		public string UI_ShowHudTargetPanelKey = "ui_hud_show_target_panel";

		public string UI_ShowHudTargetsKey = "ui_hud_show_targets";

		public string UI_ShowHudControlsKey = "ui_hud_show_controls";

		public string UI_ShowHudDialogKey = "ui_hud_show_dialog";

		public string UI_ShowHudIndicatorsKey = "ui_hud_show_indicators";

		public string UI_ShowHudTargetIndicatorsKey = "ui_hud_show_target_indicators";

		public string General_AllowFireAtNeutralKey = "allow_fire_at_neutral";

		public string General_AllowFireAtAlliedKey = "allow_fire_at_allied";

		public string General_CameraShakeOnShieldHitKey = "camera_shake_shield_hit";

		public string General_CameraShakeOnHullHitKey = "camera_shake_hull_hit";

		public bool General_DefaultCameraShakeOnHullHit = true;

		public bool General_DefaultCameraShakeOnShieldHit;

		public string Video_FullScreenKey = "video_full_screen";

		public string Video_TargetFrameRateKey = "video_target_frame_rate";

		public string Video_WormholeAnimationEnabledKey = "video_wormhole_animation_enabled";

		public int Video_DefaultTargetFrameRate = 60;

		public bool Video_DefaultWormholeAnimationEnabled = true;

		public int[] Video_TargetFrameRates = new int[5] { 30, 60, 90, 120, 144 };

		// Open Frontier: v-sync toggle (display-paced rendering on mobile).
		public string Video_VSyncKey = "video_vsync";

		public string Audio_MissileLockSoundKey = "audio_missile_lock_sound";
	}
}
