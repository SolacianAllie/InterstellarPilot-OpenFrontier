using System.Collections;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Adopts the joiner's own pilot, once the host's world has actually arrived.
	/// </summary>
	/// <remarks>
	/// This used to run the instant TryLoadScenario returned, and it never
	/// worked: the load brings in a scene, and "started" is not "finished".
	/// The lookup ran against a world that had not had its save applied yet,
	/// found no faction of its own, and reported that the host had not sent
	/// one - which was true of the moment it looked at and false of the world
	/// a second later.
	///
	/// So it waits. Polling is the honest shape for this: there is no event
	/// for "a save has finished importing into a world that already existed",
	/// and the thing being waited for - a faction with an id we already know -
	/// is precisely checkable.
	///
	/// The timeout is not a fallback that gives up quietly. If the faction
	/// never turns up, that is a real answer and the player is told it,
	/// because silently staying the host's pilot while a message says
	/// "joined" is worse than being told it went wrong.
	/// </remarks>
	public sealed class JoinerPilotAdoption : MonoBehaviour
	{
		private const float TimeoutSeconds = 60f;

		/// <summary>Raised with a sentence for the player.</summary>
		public static event System.Action<string> Finished;

		/// <summary>Fraction of the way to the timeout, for the session panel.</summary>
		public float Waited { get; private set; }

		/// <summary>Begins waiting for the joiner's faction and adopting it.</summary>
		public void Begin(WorldDescriptorMessage descriptor)
		{
			StartCoroutine(AdoptWhenPresent(descriptor));
		}

		private IEnumerator AdoptWhenPresent(WorldDescriptorMessage descriptor)
		{
			float deadline = Time.unscaledTime + TimeoutSeconds;
			int factionId = descriptor.MyFactionId;

			while (factionId >= 0 && Time.unscaledTime < deadline)
			{
				Faction arrived = FindFaction(factionId);

				if (arrived != null)
				{
					// The world is up. Adopt now, rather than in the same
					// frame as finding it, because the rest of the load may
					// still be settling and the shuttle has to be findable
					// too.
					yield return null;

					// What the joiner was flying before, and what they are
					// flying now. The before is a control that needs no
					// theory to interpret: a ship this game made, described
					// in the same run and the same format as the one being
					// questioned.
					Unit previous = EngineASX.Instance.LocalPlayer == null
						? null
						: EngineASX.Instance.LocalPlayer.Person.CurrentUnit;

					Debug.Log("DiagS "
						+ UnitDiagnostics.Describe(previous, "joiner's previous ship"));

					string result = JoinerIdentityAdopter.Adopt(factionId, descriptor.MyShipId);

					// Samples taken against the world that was just replaced
					// describe units that may not exist any more, at times on a
					// clock that no longer means anything. Kept, they would
					// have the joiner's new ship drawn from another world's
					// history for the first fraction of a second.
					UnitInterpolator.Forget();

					Unit adopted = EngineASX.Instance.LocalPlayer == null
						? null
						: EngineASX.Instance.LocalPlayer.Person.CurrentUnit;

					Debug.Log("DiagS "
						+ UnitDiagnostics.Describe(adopted, "joiner's new ship"));

					Debug.Log("Multiplayer: join finished - " + result);
					Finished?.Invoke(result);
					yield break;
				}

				Waited = (Time.unscaledTime - (deadline - TimeoutSeconds)) / TimeoutSeconds;

				yield return null;
			}

			string failure = factionId < 0
				? "the host never made a player for you"
				: "the world arrived without your pilot in it";

			Debug.LogWarning("Multiplayer: join finished, but could not become your own "
				+ "pilot - " + failure + ".");

			Finished?.Invoke("Joined, but could not become your own pilot: " + failure + ".");
		}

		private static Faction FindFaction(int factionId)
		{
			if (factionId < 0)
			{
				return null;
			}

			EngineASX engine = EngineASX.Instance;

			if (engine == null || engine.World == null)
			{
				return null;
			}

			return engine.GetFactionByid(factionId);
		}
	}
}