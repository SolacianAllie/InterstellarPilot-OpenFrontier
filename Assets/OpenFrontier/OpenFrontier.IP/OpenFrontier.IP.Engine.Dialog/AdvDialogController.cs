using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Dialog
{
	public class AdvDialogController : MonoBehaviour
	{
		private Dictionary<AdvDialogType, List<AdvDialogMessage>> messageByType = new Dictionary<AdvDialogType, List<AdvDialogMessage>>();

		public void Init()
		{
			AdvDialogMessage[] componentsInChildren = GetComponentsInChildren<AdvDialogMessage>();
			foreach (AdvDialogMessage advDialogMessage in componentsInChildren)
			{
				if (!messageByType.TryGetValue(advDialogMessage.DialogType, out var value))
				{
					value = new List<AdvDialogMessage>();
					messageByType[advDialogMessage.DialogType] = value;
				}
				value.Add(advDialogMessage);
				advDialogMessage.Compile();
			}
		}

		public string GetMessaage(AdvDialogType advDialogType, Faction sourceFaction, Person sourcePerson, float? opinionToSource)
		{
			if (messageByType.TryGetValue(advDialogType, out var value))
			{
				bool sourceIsFreelancer = sourceFaction != null && sourceFaction.People.Count == 1;
				IEnumerable<AdvDialogMessage> enumerable = value.Where((AdvDialogMessage e) => MessageMatches(e, sourceFaction, sourcePerson, opinionToSource, sourceIsFreelancer));
				if (enumerable.Any())
				{
					AdvDialogMessage random = enumerable.GetRandom();
					if (random.HasSingularPluralVariants && sourceFaction != null)
					{
						if (sourceFaction.People.Count > 1)
						{
							return random.PluralVersion;
						}
						return random.SingularVersion;
					}
					return random.Message;
				}
			}
			return null;
		}

		public bool MessageMatches(AdvDialogMessage message, Faction sourceFaction, Person sourcePerson, float? opinionToSource, bool sourceIsFreelancer)
		{
			if (sourceIsFreelancer && message.ExcludeFreelancer)
			{
				return false;
			}
			if (sourceFaction != null)
			{
				if (sourceFaction.Aggression < message.MinAggression || sourceFaction.Aggression > message.MaxAggression)
				{
					return false;
				}
				if (sourceFaction.Virtue < message.MinVirtue || sourceFaction.Virtue > message.MaxVirtue)
				{
					return false;
				}
				if (sourceFaction.Greed < message.MinGreed || sourceFaction.Greed > message.MaxGreed)
				{
					return false;
				}
				if ((opinionToSource.HasValue && opinionToSource < message.MinOpinion) || opinionToSource > message.MaxOpinion)
				{
					return false;
				}
			}
			if (sourcePerson != null)
			{
				if (sourcePerson.Properness >= message.MinProperness)
				{
					return sourcePerson.Properness <= message.MaxProperness;
				}
				return false;
			}
			return true;
		}
	}
}
