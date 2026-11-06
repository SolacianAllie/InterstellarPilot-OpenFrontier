using System;
using System.Collections.Generic;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	// Ship decals - the runtime half of the decal language documented in
	// tools/decals/README.md. The ship shaders carry two albedo layers
	// (faction pattern + superpower brand) but deliberately ship NOTHING
	// on the materials: livery is decided here, in post, per faction.
	//
	// WHAT IT DOES
	// - Applied from ActiveUnit.Init, the only place unit visuals come
	//   into being; the visuals are re-made on every active-sector
	//   entry, so already-spawned units get the transplant automatically
	//   the next time they are observed. One choke point, no sweeps.
	// - Every hull/cloak-shell renderer whose material has the decal
	//   slots (OpenFrontier/Ship Surface and Ship Cloak) gets a CLONED
	//   material carrying the faction's pattern sheet and its triad
	//   colours. Legacy URP Lit hulls fail the gate and stay untouched.
	// - Clones are cached per (source material, faction), so a fleet of
	//   N ships shares ONE material instance and SRP batches normally;
	//   the cloak path keeps pushing _CloakAmount/_CloakFloor over the
	//   top through its material property block, which composes with
	//   this without either side overwriting the other.
	//
	// THE PALETTE DECIDER
	// One faction, three colours, picked from its identity, status and
	// economy:
	//   hue      - IDENTITY, not type. A saturated Faction.TeamColour is
	//              still the anchor (a hand-authored colour wins);
	//              otherwise the hue is golden-ratio stepped from the
	//              faction's UniqueId, so every faction owns a stable,
	//              distinct hue instead of inheriting its type family's
	//              band (the bandits-always-red / mercs-always-green
	//              lock is gone). Chartreuse is still skipped - paint
	//              there reads sickly on grey hulls.
	//   spread   - secondary and tertiary hues fan out +120..170deg and
	//              -100..145deg from the primary, enforced >=30deg apart
	//              pairwise, so the three zones stay distinct even after
	//              reshaping.
	//   status   - faction type plus personality: bandit/outlaw paint is
	//              grimy (saturated down, darkened), empire dress SAT is
	//              nudged up, aggression darkens the bite, minors are
	//              muted. Trading types get a slight value lift.
	//   economy  - net worth against the minor-faction threshold on a
	//              log scale: destitute factions wear faded paint
	//              (-22% sat, -14% val), rich ones vivid (+8% sat, +10%
	//              val). A faction that prospers literally paints
	//              louder, and the extremes are allowed to run free -
	//              the shaping layer owns the look, nothing counters it.
	//   domain   - saturation and value run the FULL 0..1 range: the
	//              old [0.30, 0.85] mid-tone caps are gone by decision,
	//              the only clamp left is HSVToRGB's legal domain. A
	//              maximal faction may light up at full saturation; a
	//              starved one may paint nearly bare. Ink stays alpha 1
	//              - translucency remains a hand-authored material
	//              knob, not a decider output.
	public static class ShipDecals
	{
		private const string PropDecalMap = "_DecalMap";
		private const string PropPrimary = "_DecalPrimary";
		private const string PropSecondary = "_DecalSecondary";
		private const string PropTertiary = "_DecalTertiary";

		// Validity caps only: saturation/value run the full 0..1 domain
		// (the [0.30, 0.85] aesthetic pair is gone by decision). The
		// clamp exists purely because HSVToRGB needs a legal [0..1]
		// input - over-saturated shaping lands exactly at the cap,
		// never wraps.
		private const float MinSaturation = 0f;
		private const float MaxSaturation = 1f;
		private const float MinValue = 0f;
		private const float MaxValue = 1f;

		private static bool sheetWarned;

		private static Texture2D[] sheets;

		private static readonly Dictionary<int, Texture2D> sheetByType =
			new Dictionary<int, Texture2D>();

		private static readonly Dictionary<int, Color[]> paletteCache =
			new Dictionary<int, Color[]>();

		private static readonly Dictionary<(string sourceName, int factionId), Material> cloneCache =
			new Dictionary<(string sourceName, int factionId), Material>();

		// ---- the entry point ----------------------------------------------------

		public static void ApplyToUnit(Unit unit)
		{
			ActiveUnit activeUnit = (unit != null) ? unit.ActiveUnit : null;
			if (activeUnit == null)
			{
				return;
			}
			Faction faction = unit.Faction;
			if (faction == null)
			{
				return;
			}
			Color[] palette = PaletteFor(faction);
			Texture2D sheet = SheetFor(faction);
			if (palette == null || sheet == null)
			{
				return;
			}
			foreach (MeshRenderer renderer in RenderersOf(activeUnit))
			{
				Material source = renderer.sharedMaterial;
				// The same gate shape the cloak probe uses: only hulls
				// whose shader actually carries the decal slots are
				// considered; everything else must keep its authored look.
				if (source == null || !source.HasProperty(PropDecalMap))
				{
					continue;
				}
				renderer.sharedMaterial = AcquireClone(source, faction, sheet, palette);
			}
		}

		private static IEnumerable<MeshRenderer> RenderersOf(ActiveUnit activeUnit)
		{
			// Hulls first (both pools, whichever the prefab authored),
			// then the cloak shells so the paint rides the fold. The
			// outline glow renderers are deliberately not walked.
			if (activeUnit.UnitRenderer != null)
			{
				yield return activeUnit.UnitRenderer;
			}
			if (activeUnit.UnitRenderers != null)
			{
				foreach (MeshRenderer renderer in activeUnit.UnitRenderers)
				{
					if (renderer != null)
					{
						yield return renderer;
					}
				}
			}
			if (activeUnit.CloakRenderer != null)
			{
				yield return activeUnit.CloakRenderer;
			}
			if (activeUnit.CloakRenderers != null)
			{
				foreach (MeshRenderer renderer in activeUnit.CloakRenderers)
				{
					if (renderer != null)
					{
						yield return renderer;
					}
				}
			}
		}

		// ---- material layer -----------------------------------------------------

		private static Material AcquireClone(Material source, Faction faction,
			Texture2D sheet, Color[] palette)
		{
			(string sourceName, int factionId) key = (source.name, faction.UniqueId);
			if (cloneCache.TryGetValue(key, out Material cached) && cached != null)
			{
				return cached;
			}
			Material clone = new Material(source);
			clone.name = source.name + " (ShipDecals)";
			clone.hideFlags = HideFlags.HideAndDontSave;
			clone.SetTexture(PropDecalMap, sheet);
			clone.SetColor(PropPrimary, palette[0]);
			clone.SetColor(PropSecondary, palette[1]);
			clone.SetColor(PropTertiary, palette[2]);
			cloneCache[key] = clone;
			return clone;
		}

		// ---- sheet library ------------------------------------------------------

		private static Texture2D SheetFor(Faction faction)
		{
			if (sheets == null)
			{
				sheets = Resources.LoadAll<Texture2D>("Decals");
				if (sheets == null || sheets.Length == 0)
				{
					sheets = Array.Empty<Texture2D>();
				}
				Array.Sort(sheets, (a, b) => string.CompareOrdinal(a.name, b.name));
			}
			if (sheets.Length == 0)
			{
				if (!sheetWarned)
				{
					sheetWarned = true;
					Debug.LogWarning("ShipDecals: no pattern sheets under a " +
						"Resources/Decals folder - ship decals stay unwired.");
				}
				return null;
			}
			// Type-named sheets bind by prefix ("<FactionType>_") with
			// the trading family inheriting Trader and everyone else
			// inheriting Generic; the first sheet alphabetically is the
			// last-resort look when no names resolve.
			if (sheetByType.Count == 0)
			{
				foreach (Texture2D candidate in sheets)
				{
					int underscore = candidate.name.IndexOf('_');
					if (underscore > 0 &&
						Enum.TryParse(candidate.name.Substring(0, underscore),
							out FactionType parsed) &&
						!sheetByType.ContainsKey((int)parsed))
					{
						sheetByType[(int)parsed] = candidate;
					}
				}
			}
			if (sheetByType.TryGetValue((int)faction.FactionType, out Texture2D typed))
			{
				return typed;
			}
			// The family ruling: who CAN wear the trader decal. Trading
			// types (with no sheet of their own) inherit it; every other
			// faction lands on the Generic sheet, which doubles as the
			// last-resort fallback when nothing is named at all.
			if (TradingFamily(faction.FactionType) &&
				sheetByType.TryGetValue((int)FactionType.Trader, out Texture2D trader))
			{
				return trader;
			}
			if (sheetByType.TryGetValue((int)FactionType.Generic, out Texture2D generic))
			{
				return generic;
			}
			return sheets[0];
		}

		// A faction wearing the trader decal family: trades for a living,
		// even when its own <FactionType>_ sheet was never painted.
		private static bool TradingFamily(FactionType type)
		{
			return type == FactionType.Trader
				|| type == FactionType.EquipmentDealer
				|| type == FactionType.PassengerTransport;
		}

		// ---- the palette decider ------------------------------------------------

		private static Color[] PaletteFor(Faction faction)
		{
			if (paletteCache.TryGetValue(faction.UniqueId, out Color[] cached))
			{
				return cached;
			}

			// Status first: the faction character sets the anchors and the
			// reshaping multipliers, economy then fans them open.
			System.Random random = new System.Random((int)SeedFor(faction));
			float satMul = 1f;
			float valMul = 1f;

			HueFor(faction, out float hue);

			// Economy on a log scale: 1x the minor-faction threshold is a
			// young faction, 40x threshold is top of the range.
			long netWorth = faction.GetCachedNetWorth();
			float minorThreshold = 2500000f;
			GameSettings settings = EngineASX.Instance != null
				? EngineASX.Instance.GameSettings : null;
			if (settings != null)
			{
				minorThreshold = settings.MinorFactionNetWorthThreshold;
			}
			float economy = Mathf.Clamp01(
				Mathf.Log10(Mathf.Max(1f, netWorth / minorThreshold)) / 1.6f);
			satMul *= Mathf.Lerp(0.78f, 1.08f, economy);
			valMul *= Mathf.Lerp(0.86f, 1.1f, economy);

			// Personality + minor status.
			float aggression = Mathf.Clamp01(faction.Aggression);
			float loudness = Mathf.Clamp01(faction.Greed);
			satMul *= Mathf.Lerp(0.85f, 1.12f, (aggression + loudness) * 0.5f);
			valMul *= Mathf.Lerp(1f, 0.88f, aggression);
			if (faction.IsMinor)
			{
				satMul *= 0.88f;
				valMul *= 0.92f;
			}

			FactionType type = faction.FactionType;
			if (type == FactionType.Bandit || type == FactionType.Outlaw)
			{
				satMul *= 0.9f;
				valMul *= 0.8f;
			}
			else if (type == FactionType.Empire)
			{
				satMul *= 1.05f;
			}
			else if (type == FactionType.Trader || type == FactionType.EquipmentDealer
				|| type == FactionType.PassengerTransport)
			{
				valMul *= 1.06f;
			}

			float secondaryHue = Wrap01(hue + 0.33f + 0.12f * (float)random.NextDouble());
			float tertiaryHue = Wrap01(hue - 0.28f - 0.12f * (float)random.NextDouble());
			secondaryHue = PushedApart(hue, secondaryHue, random);
			tertiaryHue = PushedApart(hue, tertiaryHue, random);
			tertiaryHue = PushedApart(secondaryHue, tertiaryHue, random);

			Color primary = HsvClamped(hue, 0.72f * satMul, 0.6f * valMul);
			Color secondary = HsvClamped(secondaryHue, 0.62f * satMul, 0.55f * valMul);
			Color tertiary = HsvClamped(tertiaryHue, 0.5f * satMul, 0.66f * valMul);

			Color[] palette = { primary, secondary, tertiary };
			paletteCache[faction.UniqueId] = palette;
			return palette;
		}

		private static uint SeedFor(Faction faction)
		{
			uint seed = (uint)faction.UniqueId;
			seed ^= seed << 13;
			seed ^= seed >> 17;
			seed ^= seed << 5;
			return seed ^ (uint)(int)faction.FactionType * 2654435761u;
		}

		// The TeamColour seed outranks everything when it carries real
		// saturation; otherwise the hue belongs to the FACTION, not its
		// type: Knuth-multiply the UniqueId and lay the top 24 bits over
		// the readable wheel. Golden-ratio stepping spreads neighbouring
		// ids far apart, keeps the hue stable across sessions (ids are
		// stored verbatim in saves), and the chartreuse gap survives -
		// paint in 0.2..0.45 reads sickly on grey hulls.
		private static void HueFor(Faction faction, out float hue)
		{
			Color.RGBToHSV(faction.TeamColour, out float teamHue,
				out float teamSat, out float teamVal);
			bool realTeamColour = teamSat >= 0.15f && teamVal >= 0.12f &&
				teamVal <= 0.95f && teamSat <= 0.98f;
			if (realTeamColour)
			{
				hue = teamHue;
				return;
			}

			uint hashed = (uint)faction.UniqueId * 2654435761u;
			float wheel = (hashed >> 8) / 16777216f;   // 0..1, top 24 bits
			float scaled = wheel * 0.75f;              // usable span 0.20 + 0.55
			hue = Wrap01(scaled < 0.20f ? scaled : 0.45f + (scaled - 0.20f));
		}

		private static Color HsvClamped(float hue, float sat, float val)
		{
			sat = Mathf.Clamp(sat, MinSaturation, MaxSaturation);
			val = Mathf.Clamp(val, MinValue, MaxValue);
			return Color.HSVToRGB(Wrap01(hue), sat, val);
		}

		private static float PushedApart(float from, float candidate, System.Random random)
		{
			// >=30 deg circular separation, pushing outward on collisions.
			float distance = CircleDistance(from, candidate);
			if (distance >= 0.0833f)   // 30/360
			{
				return candidate;
			}
			float direction = CircleDistance(from, candidate) < 0.5f
				? 1f : -1f;
			float pushed = from + direction * (0.0833f + 0.08f * (float)random.NextDouble());
			return Wrap01(pushed);
		}

		private static float CircleDistance(float a, float b)
		{
			float d = Mathf.Abs(Wrap01(a) - Wrap01(b));
			return Mathf.Min(d, 1f - d);
		}

		private static float Wrap01(float value)
		{
			value %= 1f;
			if (value < 0f)
			{
				value += 1f;
			}
			return value;
		}
	}
}
