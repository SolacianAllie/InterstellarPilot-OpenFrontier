using System;
using System.Collections.Generic;
using OpenFrontier.IP.Avatars;
using OpenFrontier.Unity.Utils;
using UnityEngine;
using Random = System.Random;

namespace OpenFrontier.IP.Engine.Core
{
	public class AvatarController : MonoBehaviour
	{
		public AvatarGenerator AvatarGenerator;

		public AvatarResourceLoader AvatarResourceLoader;

		public List<AvatarProfile> HumanAvatarProfiles;

		public List<AvatarProfile> AllAvatarProfiles;

		public List<AvatarProfile> HumanPunkAvatarProfiles;

		public List<AvatarProfile> HumanCleanAvatarProfiles;

		private Dictionary<int, AvatarProfile> avatarProfilesById = new Dictionary<int, AvatarProfile>(8);

		public void Init()
		{
			AvatarResourceLoader.Load();
			if (AllAvatarProfiles.Count == 0)
			{
				Debug.LogError("No avatar profiles found");
			}
			foreach (AvatarProfile allAvatarProfile in AllAvatarProfiles)
			{
				if (avatarProfilesById.ContainsKey(allAvatarProfile.Id))
				{
					Debug.LogError($"Duplicate avatar profile id \"{allAvatarProfile.Id}\" found", allAvatarProfile);
				}
				else
				{
					avatarProfilesById.Add(allAvatarProfile.Id, allAvatarProfile);
				}
			}
		}

		public AvatarProfile GetAvatarProfileById(int id)
		{
			return avatarProfilesById.GetValueOrDefault(id);
		}

		public void UpdateAvatarRenderer(AvatarRenderer avatarRenderer, Person person)
		{
			try
			{
				System.Random random = new System.Random(person.Seed);
				AvatarProfile avatarProfile = person.AvatarProfile;
				if (avatarProfile == null)
				{
					avatarProfile = GameController.Instance.AvatarController.HumanAvatarProfiles.GetRandom(random);
				}
				AvatarData avatar = GameController.Instance.AvatarController.AvatarGenerator.GenerateRandom(avatarProfile, person.IsMale, random);
				avatarRenderer.SetAvatar(avatar);
				avatarRenderer.gameObject.SetActive(value: true);
			}
			catch (Exception message)
			{
				avatarRenderer.gameObject.SetActive(value: false);
				Debug.LogError("Error showing avatar for pilot", person);
				Debug.LogError(message);
			}
		}
	}
}
