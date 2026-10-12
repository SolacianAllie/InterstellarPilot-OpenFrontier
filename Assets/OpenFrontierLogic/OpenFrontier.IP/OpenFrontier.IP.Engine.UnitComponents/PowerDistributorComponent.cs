using UnityEngine;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	/// <summary>
	/// The three-way power bias a ship's distributor can be set to: SYS, ENG
	/// and WEP.
	/// </summary>
	/// <remarks>
	/// Six pips split across three sectors, two each by default. Piping into one
	/// sector takes from the other two, so the total is always six and no
	/// sector can have more than four.
	///
	/// The effect is a multiplier, not a separate store. The capacitor is the
	/// whole pool and the generator fills it at its normal rate; the pips bias
	/// how that one pool serves each kind of consumer. A zeroed sector still
	/// works, at half.
	///
	/// Legacy ships fit no distributor at all, so UnitComponentHolder reports
	/// every multiplier as exactly 1.0 for them and nothing changes.
	/// </remarks>
	public class PowerDistributorComponent : ComponentBase
	{
		public const int TotalPips = 6;

		public const float MaxPips = 4f;

		public const float PipsPerSectorDefault = 2f;

		/// <summary>Cost in multiplier of one pip, either way.</summary>
		public const float PipValue = 0.25f;

		public PowerDistributorClass PowerDistributorClass;

		public override ComponentClass ComponentClass => PowerDistributorClass;

		[SerializeField]
		private float systemsPips = PipsPerSectorDefault;

		[SerializeField]
		private float enginesPips = PipsPerSectorDefault;

		[SerializeField]
		private float weaponsPips = PipsPerSectorDefault;

		/// <summary>
		/// What a pip count is worth. Two pips is the unboosted baseline, so
		/// every pip either side of it is a quarter more or less: zero is 50%,
		/// four is 150%.
		/// </summary>
		public static float MultiplierFor(float pips)
		{
			return 1f + (pips - PipsPerSectorDefault) * PipValue;
		}

		public float SystemsPips => systemsPips;

		public float EnginesPips => enginesPips;

		public float WeaponsPips => weaponsPips;

		public float SystemsMultiplier => MultiplierFor(systemsPips);

		public float EnginesMultiplier => MultiplierFor(enginesPips);

		public float WeaponsMultiplier => MultiplierFor(weaponsPips);

		public void SetPips(float systems, float engines, float weapons)
		{
			float[] pips =
			{
				Snap(Clamp(systems)),
				Snap(Clamp(engines)),
				Snap(Clamp(weapons))
			};

			Balance(pips);

			systemsPips = pips[0];
			enginesPips = pips[1];
			weaponsPips = pips[2];
		}

		/// <summary>
		/// Moves pips into one sector, taking half of the move from each of the
		/// other two, which is what a player pressing "more engines" expects.
		/// </summary>
		/// <returns>False if the move is not legal and nothing was changed.</returns>
		public bool TryMovePips(int sector, float delta)
		{
			if (sector < 0 || sector > 2)
			{
				return false;
			}

			// Halving the move has to still land on a half pip, so the move
			// itself is a whole number of pips. Half-pip values are reachable
			// through SetPips, which is what the UI should use for direct drags.
			if (Mathf.Abs(delta * 2f - Mathf.Round(delta * 2f)) > 0.001f)
			{
				return false;
			}

			delta = Snap(delta);

			int a = (sector + 1) % 3;
			int b = (sector + 2) % 3;

			float[] pips = { systemsPips, enginesPips, weaponsPips };
			float half = -delta / 2f;

			if (pips[sector] + delta > MaxPips || pips[sector] + delta < 0f)
			{
				return false;
			}

			if (pips[a] + half < 0f || pips[b] + half < 0f)
			{
				return false;
			}

			if (Mathf.Abs(half * 2f - Mathf.Round(half * 2f)) > 0.001f)
			{
				return false;
			}

			pips[sector] += delta;
			pips[a] += half;
			pips[b] += half;

			SetPips(pips[0], pips[1], pips[2]);
			return true;
		}

		/// <summary>
		/// Brings the three sectors back to six pips in total. Six cannot be
		/// created or destroyed, so any difference is put back on whichever
		/// sector has room - which is why a UI may ask for a total the model
		/// quietly corrects.
		/// </summary>
		private static void Balance(float[] pips)
		{
			float excess = pips[0] + pips[1] + pips[2] - TotalPips;

			// Hand a half pip at a time to or from whichever sector has room.
			// The guard is generous on purpose: if no sector can take the
			// correction the loop gives up rather than writing a pip count
			// outside 0..4.
			for (int guard = 0; guard < 12 && Mathf.Abs(excess) > 0.001f; guard++)
			{
				int roomiest = -1;
				float room = 0f;

				for (int j = 0; j < 3; j++)
				{
					float candidate = excess > 0f ? pips[j] : MaxPips - pips[j];

					if (candidate > room)
					{
						room = candidate;
						roomiest = j;
					}
				}

				if (roomiest < 0 || room <= 0f)
				{
					break;
				}

				float move = Snap(Mathf.Clamp(-excess, -room, room));

				if (Mathf.Abs(move) < 0.001f)
				{
					break;
				}

				pips[roomiest] += move;
				excess += move;
			}
		}

		/// <summary>
		/// The sector with the most room to absorb the correction: the fullest
		/// one when taking away, the emptiest one when giving.
		/// </summary>
		private static int Roomiest(float[] pips, float excess)
		{
			int best = 0;

			for (int i = 1; i < 3; i++)
			{
				if (excess > 0f ? pips[i] > pips[best] : pips[i] < pips[best])
				{
					best = i;
				}
			}

			return best;
		}

		private static float Clamp(float pips)
		{
			return Mathf.Clamp(pips, 0f, MaxPips);
		}

		private static float Snap(float pips)
		{
			return Mathf.Round(pips * 2f) / 2f;
		}

		protected override void onAttachedToUnit(UnitComponentHolder unit)
		{
			base.onAttachedToUnit(unit);
			unit.PowerDistributor = this;
		}

		protected override void onDetachedFromUnit(UnitComponentHolder unit)
		{
			base.onDetachedFromUnit(unit);
			unit.PowerDistributor = null;
		}
	}
}