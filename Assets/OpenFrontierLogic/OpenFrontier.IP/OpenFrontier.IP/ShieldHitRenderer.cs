using System.Collections.Generic;
using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP
{
	public class ShieldHitRenderer : MonoBehaviour
	{
		private List<ShieldHitInfo> pooledObjects = new List<ShieldHitInfo>();

		public ShieldHitInfo ShieldHitPrefab;

		public float ShieldHitDuration = 2f;

		public float ShieldScaleDuration = 0.5f;

		public float MaxShieldHitScale = 5f;

		public float MinShieldHitScale = 1f;

		public float DamageScaleMultiplier = 5f;

		public int InitialPoolSize = 25;

		// Per (unit, section) contact latch for the one-flash-per-contact
		// rule (pure C# dictionary - no native calls, safe as a static).
		private static readonly Dictionary<(Unit, int), float> LastFlashRequestTimes = new Dictionary<(Unit, int), float>();

		private void Awake()
		{
			for (int i = 0; i < InitialPoolSize; i++)
			{
				ShieldHitInfo item = UnityObjectHelper.InstantiateAndGetComponent(ShieldHitPrefab);
				pooledObjects.Add(item);
			}
		}

		public void Clear()
		{
			for (int i = 0; i < pooledObjects.Count; i++)
			{
				pooledObjects[i].gameObject.SetActive(value: false);
			}
		}

		public ShieldHitInfo RequestShieldHit(Unit unit, Vector3 damageSourceWorldPosition, float shieldDamage)
		{
			if (Vector3.Distance(unit.transform.position, GameController.Instance.MainCamera.transform.position) < EngineASX.Instance.PerformanceSettings.MaxShieldHitDist)
			{
				int shieldIndex = unit.GetShieldIndex(damageSourceWorldPosition);
				// Streams vs shots: a laser's damage ticks arrive every
				// frame (<0.1s apart), discrete shots arrive slower.
				// Streams RIDE the playing envelope (one flash per
				// contact, no strobing); a shot RESETS it, so back-to-
				// back projectiles each get their own flash.
				(Unit, int) key = (unit, shieldIndex);
				float num;
				bool flag = LastFlashRequestTimes.TryGetValue(key, out num) && Time.time - num < 0.1f;
				LastFlashRequestTimes[key] = Time.time;
				if (LastFlashRequestTimes.Count > 1024)
				{
					LastFlashRequestTimes.Clear();
				}
				for (int i = 0; i < pooledObjects.Count; i++)
				{
					ShieldHitInfo shieldHitInfo = pooledObjects[i];
					if (shieldHitInfo.gameObject.activeSelf && shieldHitInfo.TargetUnit == unit && shieldHitInfo.ShieldIndex == shieldIndex)
					{
						if (flag)
						{
							return shieldHitInfo;
						}
						shieldHitInfo.StartExpiryTime = Time.time;
						shieldHitInfo.DepletedFlash = unit.Components != null && unit.Components.ShieldComponent != null && unit.Components.ShieldComponent.GetShieldPointNormalized(shieldIndex) <= 0f;
						shieldHitInfo.Duration = (shieldHitInfo.DepletedFlash ? 0.25f : 5f);
						// Repeat hit while the visual lives: flash only -
						// the scale-in keeps running off its own clock.
						return shieldHitInfo;
					}
				}
				if (flag)
				{
					return null;
				}
				ShieldHitInfo pooledShieldHit = GetPooledShieldHit();
				if (pooledShieldHit != null)
				{
					// Open Frontier: constant size - the stock damage-scaled
					// sizing made depleted-shield hits (which absorb almost
					// nothing) spawn tiny and vanish fast. Always max.
					pooledShieldHit.MaxScale = MaxShieldHitScale;
					pooledShieldHit.gameObject.SetActive(value: true);
					pooledShieldHit.TargetUnit = unit;
					pooledShieldHit.ShieldIndex = shieldIndex;
					// A section this hit just emptied pops: bright red
					// flash, short life.
					pooledShieldHit.DepletedFlash = unit.Components != null && unit.Components.ShieldComponent != null && unit.Components.ShieldComponent.GetShieldPointNormalized(pooledShieldHit.ShieldIndex) <= 0f;
					// 5s envelope: flash choreography up front, then
					// the health color lingers and slowly fades.
					pooledShieldHit.Duration = (pooledShieldHit.DepletedFlash ? 0.25f : 5f);
					// Fresh spawn = scale-in from 0 on its own clock;
					// envelope resets never touch it.
					pooledShieldHit.ScaleInStartTime = Time.time;
					// Cache the instanced material once per pooled object -
					// the property block tint never reached the shader on
					// this stack, so the color is set directly on it.
					if (pooledShieldHit.CachedMaterial == null)
					{
						pooledShieldHit.CachedMaterial = pooledShieldHit.Renderer.material;
					}
					pooledShieldHit.StartExpiryTime = Time.time;
					pooledShieldHit.StartTime = Time.time;
					pooledShieldHit.SetShieldHitOrientation(damageSourceWorldPosition);
					// Apply the first frame NOW: a pooled object would
					// otherwise activate for one frame wearing its
					// PREVIOUS effect's transform and tint - a stale
					// ghost that reads as a flicker before the flash.
					pooledShieldHit.transform.position = unit.transform.TransformPoint(pooledShieldHit.LocalTranslation);
					pooledShieldHit.transform.rotation = unit.transform.rotation * pooledShieldHit.LocalRotation;
					pooledShieldHit.transform.localScale = Vector3.zero;
					if (pooledShieldHit.CachedMaterial != null)
					{
						pooledShieldHit.CachedMaterial.SetColor("_TintColor", new Color(1.4f, 1.4f, 1.4f, 0f));
					}
					return pooledShieldHit;
				}
			}
			return null;
		}

		private void Update()
		{
			for (int i = 0; i < pooledObjects.Count; i++)
			{
				ShieldHitInfo shieldHitInfo = pooledObjects[i];
				if (shieldHitInfo.gameObject.activeSelf)
				{
					float num = shieldHitInfo.StartExpiryTime + shieldHitInfo.Duration;
					if (Time.time > num || shieldHitInfo.TargetUnit == null)
					{
						shieldHitInfo.gameObject.SetActive(value: false);
					}
					else
					{
						UpdateShieldHitMaterial(shieldHitInfo, num);
					}
				}
			}
		}

		private void UpdateShieldHitMaterial(ShieldHitInfo hitInfo, float expiryTime)
		{
			float num2 = Time.time - hitInfo.StartExpiryTime;
			hitInfo.transform.position = hitInfo.TargetUnit.transform.TransformPoint(hitInfo.LocalTranslation);
			hitInfo.transform.rotation = hitInfo.TargetUnit.transform.rotation * hitInfo.LocalRotation;
			// Scale: grow from 0 to full over 0.25s on a fresh spawn -
			// quadratic ease-out: fast start, decelerating into full
			// size. Runs off the spawn clock, so envelope resets
			// (repeat hits) can't snap or restart it.
			float num5 = hitInfo.MaxScale;
			float num6 = Time.time - hitInfo.ScaleInStartTime;
			if (num6 < 0.25f)
			{
				float num7 = num6 / 0.25f;
				num5 = hitInfo.MaxScale * (1f - (1f - num7) * (1f - num7));
			}
			hitInfo.transform.localScale = new Vector3(num5, num5, num5);
			Color color;
			if (hitInfo.DepletedFlash)
			{
				// Death: heavier overdrive bright red, dead in 0.25s.
				float num3 = Mathf.Clamp01(num2 / 0.25f);
				float num4 = Mathf.Clamp01(num2 / 0.06f);
				color = new Color(4f * num4, 0.1f * num4, 0.1f * num4, 1f - num3);
			}
			else
			{
				Color unitShieldColor = EngineASX.Instance.GetUnitShieldColor(hitInfo.TargetUnit, hitInfo.ShieldIndex);
				// Full saturation always: the UI gradient reads washed
				// out through the additive shader. Hue and brightness
				// are preserved; achromatic greys (no-shield) pass
				// through untouched.
				Color.RGBToHSV(unitShieldColor, out float h, out float s, out float v);
				if (s > 0.05f)
				{
					unitShieldColor = Color.HSVToRGB(h, 1f, v);
				}
				// The flash peak is the health color HDR-overdriven
				// (x2.5), not white - a blue shield flashes bright blue.
				Color color4 = unitShieldColor * 2.5f;
				if (num2 < 0.2f)
				{
					// 0.0-0.2s: the flash builds from health color to
					// the overdriven peak - NO alpha fade (the scale-in
					// carries the entrance now).
					Color color2 = Color.Lerp(unitShieldColor, color4, num2 / 0.2f);
					color = new Color(color2.r, color2.g, color2.b, 1f);
				}
				else if (num2 < 0.7f)
				{
					// 0.2-0.7s: the flash fades out over 0.5s - the
					// overdrive releasing back to the health color.
					Color color3 = Color.Lerp(color4, unitShieldColor, (num2 - 0.2f) / 0.5f);
					color = new Color(color3.r, color3.g, color3.b, 1f);
				}
				else
				{
					// 0.7-5.0s: the health color stays visible, slowly
					// fading out for the rest of the effect.
					color = new Color(unitShieldColor.r, unitShieldColor.g, unitShieldColor.b, 1f - (num2 - 0.7f) / (hitInfo.Duration - 0.7f));
				}
			}
			// The runtime shader is Legacy Particles/Additive: it tints
			// via _TintColor (NOT _Color or _BaseColor).
			if (hitInfo.CachedMaterial != null)
			{
				hitInfo.CachedMaterial.SetColor("_TintColor", color);
			}
		}

		private ShieldHitInfo GetPooledShieldHit()
		{
			for (int i = 0; i < pooledObjects.Count; i++)
			{
				if (!pooledObjects[i].gameObject.activeSelf)
				{
					return pooledObjects[i];
				}
			}
			return null;
		}
	}
}
