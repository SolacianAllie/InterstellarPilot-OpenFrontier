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
	//   material carrying that HULL's faction pattern sheet - sheets live
	//   per ship under its Resources/Decals folder, because the masks are
	//   painted in the hull's own UV space - and the faction's triad
	//   colours. Legacy URP Lit hulls fail the gate and stay untouched.
	// - A unit's paint is ASSIGNED, not derived: its faction's role type
	//   names a pool of sheets on the hull, and the unit draws one
	//   deterministically (hash of unique id + painted role). The draw
	//   STICKS while the role stands; a changed role repaints at the
	//   station dock (OnDocked, hooked inside UnitComponentHolder.
	//   TryDockInHangar - the dock primitive every approved dock goes
	//   through, TryDockInUnit included, so fleet regroup and the
	//   multiplayer docked-state sync repaint with everything else, and
	//   an unpainted or already-agreeing ship pays a dictionary read).
	//   Player-repaint-at-station is the same choke point later.
	// - Clones are cached per (source material, faction, sheet), so
	//   ships wearing the same paint share ONE material instance and
	//   SRP batch normally; the cloak path keeps pushing
	//   _CloakAmount/_CloakFloor over the top through its material
	//   property block, which composes with this without either side
	//   overwriting the other.
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
	//   spread   - the triad's relationship is PICKED, not poured: one of
	//              four canonical brand schemes (triadic, split-
	//              complementary, complement-plus-kin, analogous) chosen
	//              per faction off its seed, tight jitter only. Zones
	//              stay >=30deg apart pairwise, so the three colours
	//              stay distinct even after reshaping.
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

		// Per-ship sheet sets under Ships/<Ship>/Resources/Decals/. The
		// contract names: "<Ship> Decal <FactionType> <mark>" (the authored
		// plates) or "<Ship>_<FactionType>_<mark>" (workbench masks) -
		// tokens split on spaces AND underscores, so both spellings bind
		// identically. The ship token binds art to its hull's UV space,
		// the FactionType token puts a sheet into that role's DRAW POOL,
		// and one Resources.LoadAll("Decals") collects every ship's set
		// at once. Adding sheets to a role widens its pool automatically.
		private sealed class ShipSheetSet
		{
			public string shipToken;
			public readonly List<Texture2D> bound = new List<Texture2D>();
			public readonly Dictionary<int, List<Texture2D>> listsByType =
				new Dictionary<int, List<Texture2D>>();
		}

		private static readonly List<ShipSheetSet> shipSheetSets = new List<ShipSheetSet>();
		private static bool sheetsLoaded;
		private static bool warnedNoSheets;

		// Which ROLE each unit is currently painted as - the paint stick
		// itself. Runtime state, deliberately not serialized: the same
		// draw recomputes deterministically, so all this buys is
		// "repaint after the dock, not on arbitrary sector entries".
		private static readonly Dictionary<int, FactionType> paintedRoleOfUnit =
			new Dictionary<int, FactionType>();

		private static readonly Dictionary<int, Color[]> paletteCache =
			new Dictionary<int, Color[]>();

		private static readonly Dictionary<(string sourceName, int factionId, string sheet), Material> cloneCache =
			new Dictionary<(string sourceName, int factionId, string sheet), Material>();

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
				// Pattern sheets are authored per hull - a Magnus sheet is
				// painted in Magnus UV space and is nonsense on any other
				// hull - so the sheet follows the MATERIAL's ship token,
				// never the faction alone. A hull without its own sheet
				// keeps its authored look untouched.
				Texture2D sheet = SheetFor(unit, source.name);
				if (sheet == null)
				{
					continue;
				}
				renderer.sharedMaterial = AcquireClone(source, faction, sheet, palette);
			}
		}

		// The repaint station. Called from UnitComponentHolder.
		// TryDockInHangar - the dock primitive every approved dock goes
		// through (TryDockInUnit funnels into it; fleet regroup, the
		// multiplayer docked-state applier and the loader land here
		// too) - never a poll. Station-gated inside: a ship whose
		// painted role no longer matches its faction's role draws its
		// NEW look as it docks, and keeps that until the next role
		// change. Unpainted units pass through untouched - their first
		// paint already used their current role, which is exactly what
		// makes the load, seed and join paths free here.
		public static void OnDocked(Unit unit, Unit station)
		{
			if (unit == null || unit.Faction == null || unit.IsDestroyed)
			{
				return;
			}
			if (station == null || station.UnitClass == null ||
				station.UnitClass.StationPurpose == StationPurpose.None)
			{
				return;
			}
			if (!paintedRoleOfUnit.TryGetValue(unit.UniqueId, out FactionType painted) ||
				painted == unit.Faction.FactionType)
			{
				return;
			}
			// Station services rendered: record the new role and refresh
			// the hull at once (hidden docked units simply rebuild with
			// it at the next sector entry).
			paintedRoleOfUnit[unit.UniqueId] = unit.Faction.FactionType;
			ApplyToUnit(unit);
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
		}

		// ---- material layer -----------------------------------------------------

		private static Material AcquireClone(Material source, Faction faction,
			Texture2D sheet, Color[] palette)
		{
			(string sourceName, int factionId, string sheet) key =
				(source.name, faction.UniqueId, sheet.name);
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

		// ---- sheet library -----------------------------------------------------
		//
		// Sheets live PER SHIP, inside the ship's own texture area, under a
		// private Resources root so runtime loads can reach them:
		//
		//   Assets/OpenFrontier/Textures/Ships/<Ship>/Resources/Decals/
		//       <Ship> Decal <FactionType> <mark>.png   (authored plates)
		//       <Ship>_<FactionType>_<mark>.png            (workbench masks)
		//
		// "<Ship>_" binds a sheet to its hull - the plates are painted in
		// that hull's UV0 space and are meaningless on any other ship.
		// The next token puts the sheet into that FactionType's DRAW
		// POOL; the trailing mark ("1", "2", ...) decides nothing at
		// runtime - it is authoring bookkeeping.
		//
		// A hull whose material carries no matching ship token, or whose
		// ship has no sheets yet, keeps its authored look untouched - never
		// wear another ship's masks.

		private static string ShipTokenFor(string materialName)
		{
			LoadSheets();
			if (shipSheetSets.Count == 0 || string.IsNullOrEmpty(materialName))
			{
				return null;
			}
			// Longest token first, so a hull-variant folder (when one
			// exists) is never shadowed by its family prefix.
			ShipSheetSet best = null;
			int length = -1;
			foreach (ShipSheetSet set in shipSheetSets)
			{
				if (set.shipToken != null && set.shipToken.Length > length &&
					materialName.IndexOf(set.shipToken, StringComparison.OrdinalIgnoreCase) >= 0)
				{
					best = set;
					length = set.shipToken.Length;
				}
			}
			return best != null ? best.shipToken : null;
		}

		// ---- the paint job ------------------------------------------------------
		//
		// A UNIT's paint is ASSIGNED, not derived: its faction's role
		// type names a pool of sheets for the hull, and the unit
		// deterministically draws one (stable hash of its unique id and
		// the role it is painted as) - same id, same paint, on every
		// client and every launch, with no saved state and no network
		// traffic. The draw STICKS: the painted role is recorded per
		// unit, and only a role CHANGE leads to a fresh draw - and only
		// once the unit docks (OnDocked). A hull with no sheet for its
		// role and no inherited pool keeps its authored look untouched.

		private static Texture2D SheetFor(Unit unit, string materialName)
		{
			string shipToken = ShipTokenFor(materialName);
			LoadSheets();
			if (shipToken == null)
			{
				return null;
			}
			ShipSheetSet set = ShipSetFor(shipToken);
			if (set == null || set.bound.Count == 0)
			{
				return null;
			}
			FactionType painted = PaintedRoleOf(unit);
			List<Texture2D> pool = ListForRole(set, painted);
			return (pool != null && pool.Count > 0)
				? pool[PickIndex(unit.UniqueId, painted, pool.Count)]
				: null;
		}

		// The role a unit is painted as: recorded on first paint, read
		// back on every sector entry, so the livery never re-rolls while
		// its role stands still.
		private static FactionType PaintedRoleOf(Unit unit)
		{
			FactionType current = unit.Faction.FactionType;
			if (!paintedRoleOfUnit.TryGetValue(unit.UniqueId, out FactionType painted))
			{
				painted = current;
				paintedRoleOfUnit[unit.UniqueId] = painted;
			}
			return painted;
		}

		// The role's draw pool: every sheet its FactionType names, then
		// the trading families draw from the Trader pool, everyone else
		// from Generic, and the ship's whole bound set is the last
		// resort. Sheets added to a role widen its pool automatically.
		private static List<Texture2D> ListForRole(ShipSheetSet set, FactionType role)
		{
			if (set.listsByType.TryGetValue((int)role, out List<Texture2D> pool) &&
				pool.Count > 0)
			{
				return pool;
			}
			if (TradingFamily(role) &&
				set.listsByType.TryGetValue((int)FactionType.Trader, out pool) &&
				pool.Count > 0)
			{
				return pool;
			}
			if (set.listsByType.TryGetValue((int)FactionType.Generic, out pool) &&
				pool.Count > 0)
			{
				return pool;
			}
			return set.bound;
		}

		// The stable draw: multiply-xor hash of the unit's id and the
		// role it is painted as, mod the pool length. Same on both sides
		// of a multiplayer link and across restarts; only content edits
		// (a pool growing) can re-bucket a draw.
		private static int PickIndex(int uniqueId, FactionType role, int count)
		{
			ulong hash = (ulong)(uint)uniqueId * 0x9E3779B97F4A7C15UL;
			hash ^= hash >> 32;
			hash += (ulong)(int)role * 0xD1B54A32D192ED03UL;
			hash ^= hash >> 29;
			hash *= 0xBF58476D1CE4E5B9UL;
			hash ^= hash >> 32;
			return (int)(hash % (ulong)count);
		}

		private static ShipSheetSet ShipSetFor(string shipToken)
		{
			foreach (ShipSheetSet set in shipSheetSets)
			{
				if (set.shipToken == shipToken)
				{
					return set;
				}
			}
			return null;
		}

		private static void LoadSheets()
		{
			if (sheetsLoaded)
			{
				return;
			}
			sheetsLoaded = true;
			Texture2D[] raw = Resources.LoadAll<Texture2D>("Decals");
			Array.Sort(raw, (a, b) => string.CompareOrdinal(a.name, b.name));
			foreach (Texture2D texture in raw)
			{
				// The binding contract speaks BOTH spellings: the authored
				// plates ("<Ship> Decal <FactionType> <mark>" - the user's
				// painting files) and workbench masks
				// ("<Ship>_<FactionType>_<mark>"). Tokens split on spaces
				// AND underscores, so "Magnus Decal Trader 1" and
				// "Magnus_Trader_2" bind identically. Anything whose
				// faction token is not a FactionType (the dirt plate, the
				// mask stock) is authoring-only and skipped, never
				// guessed at.
				string[] tokens = texture.name.Split(
					new char[] { ' ', '_' }, StringSplitOptions.RemoveEmptyEntries);
				if (tokens.Length < 2)
				{
					continue;
				}
				string shipToken = tokens[0];
				int cursor = 1;
				if (string.Equals(tokens[cursor], "Decal", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(tokens[cursor], "Decals", StringComparison.OrdinalIgnoreCase))
				{
					cursor++;
				}
				// Numeric tokens are marks, never factions ("Decal 1"
				// would otherwise parse "1" straight into Trader=1).
				if (cursor >= tokens.Length || char.IsDigit(tokens[cursor][0]))
				{
					continue;
				}
				if (!Enum.TryParse(tokens[cursor], true, out FactionType parsed) ||
					!Enum.IsDefined(typeof(FactionType), parsed))
				{
					continue;
				}
				ShipSheetSet set = ShipSetFor(shipToken);
				if (set == null)
				{
					set = new ShipSheetSet { shipToken = shipToken };
					shipSheetSets.Add(set);
				}
				// Every sheet a role names joins that role's list - the
				// lists are draw pools, not single slots. The bound list
				// keeps only parseable sheets, so a fallback never dresses
				// from authoring stock.
				List<Texture2D> pool;
				if (!set.listsByType.TryGetValue((int)parsed, out pool))
				{
					pool = new List<Texture2D>();
					set.listsByType[(int)parsed] = pool;
				}
				pool.Add(texture);
				set.bound.Add(texture);
			}
			if (raw.Length == 0 && !warnedNoSheets)
			{
				warnedNoSheets = true;
				Debug.LogWarning("ShipDecals: no pattern sheets under any " +
					"Ships/<Ship>/Resources/Decals folder - ship decals " +
					"stay unwired.");
			}
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

			// The triad's relationship is PICKED, not poured - corporate
			// palettes feel right because the two support colours sit in
			// fixed roles against the primary, not at random wheel
			// points. Four canonical schemes, chosen per faction from its
			// seed; jitter is tight (about 7 deg) so same-scheme
			// factions read as siblings, not clones. The identity wheel
			// still spreads primaries, so scheme reuse between factions
			// lands on different hues.
			float secondaryHue;
			float tertiaryHue;
			switch (random.Next(4))
			{
				case 0: // triadic: even thirds
					secondaryHue = Wrap01(hue + 0.3333f + 0.02f * ((float)random.NextDouble() - 0.5f));
					tertiaryHue = Wrap01(hue - 0.3333f + 0.02f * ((float)random.NextDouble() - 0.5f));
					break;
				case 1: // split-complementary: the classic brand relation
					secondaryHue = Wrap01(hue + 0.4167f + 0.02f * ((float)random.NextDouble() - 0.5f));
					tertiaryHue = Wrap01(hue - 0.4167f + 0.02f * ((float)random.NextDouble() - 0.5f));
					break;
				case 2: // one complement, one kin of the primary
					secondaryHue = Wrap01(hue + 0.5f + 0.02f * ((float)random.NextDouble() - 0.5f));
					tertiaryHue = Wrap01(hue - (0.085f + 0.04f * (float)random.NextDouble()));
					break;
				default: // analogous: kin either side, separation carried by value
					secondaryHue = Wrap01(hue + 0.09f + 0.05f * (float)random.NextDouble());
					tertiaryHue = Wrap01(hue - 0.16f - 0.08f * (float)random.NextDouble());
					break;
			}
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
