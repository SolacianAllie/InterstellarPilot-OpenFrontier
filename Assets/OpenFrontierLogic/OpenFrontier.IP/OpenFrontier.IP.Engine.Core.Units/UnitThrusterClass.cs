using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Core.Units
{
	public class UnitThrusterClass : MonoBehaviour
	{
		public List<UnitThrusterParticleSystem> UnitThrusterParticleSystems;

		/// <summary>
		/// How hot this ship's thrusters make it look, at full thrust. Scaled
		/// by the throttle, so a shut engine is a cold ship.
		/// </summary>
		/// <remarks>
		/// The sum of every thruster's particle size, so a ship carrying more
		/// of them is hotter than one carrying fewer, which is the whole of the
		/// infrared seeker's picture of a hull.
		///
		/// This replaced the engine's ThermalLoad as the signature, because that
		/// was gated on the thermal vents and no ship in the game has a vent bay
		/// to fit - so every hull radiated identically and the seeker could not
		/// tell a scout from a battleship. Thruster size is carried by data every
		/// ship actually has.
		/// </remarks>
		public float TotalHeatSize
		{
			get
			{
				if (!hasTotalHeatSize)
				{
					float total = 0f;
					for (int i = 0; i < UnitThrusterParticleSystems.Count; i++)
					{
						UnitThrusterParticleSystem thruster = UnitThrusterParticleSystems[i];
						if (thruster != null)
						{
							total += thruster.HeatSize;
						}
					}
					totalHeatSize = total;
					hasTotalHeatSize = true;
				}
				return totalHeatSize;
			}
		}

		private float totalHeatSize;

		private bool hasTotalHeatSize;

		private void OnValidate()
		{
			// Editing a thruster's size in the inspector has to show up, and
			// nothing else would tell this it changed.
			hasTotalHeatSize = false;
		}
	}
}
