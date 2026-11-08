using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine.AmbientSounds
{
	public class EngineAmbientSoundManager : MonoBehaviour
	{
		private List<AmbientSound> ambientSounds = new List<AmbientSound>();

		private EngineASX engine;

		private void Awake()
		{
			engine = UnityObjectHelper.FindInParentsOrSelf<EngineASX>(gameObject);
		}

		public AmbientSound PlayAmbientSound(AudioSource audioSourcePrefab, Transform parent, float volume = 1f)
		{
			if (parent != null)
			{
				GameObject pooledObjectOrCreate = engine.Pooler.GetPooledObjectOrCreate(audioSourcePrefab.gameObject);
				pooledObjectOrCreate.gameObject.SetActive(value: true);
				AudioSource component = pooledObjectOrCreate.GetComponent<AudioSource>();
				component.volume = volume;
				component.transform.position = parent.transform.position;
				component.Play();
				AmbientSound ambientSound = new AmbientSound
				{
					AudioSource = component,
					Parent = parent,
					MaxVolume = volume
				};
				ambientSounds.Add(ambientSound);
				return ambientSound;
			}
			if (LogWrapper.LogMsgs)
			{
				Debug.LogError("Trying to play ambient sound without parent", this);
			}
			return null;
		}

		public void Clear()
		{
			ambientSounds.Clear();
		}

		private void Update()
		{
			for (int i = 0; i < ambientSounds.Count; i++)
			{
				AmbientSound ambientSound = ambientSounds[i];
				if (ambientSound.AudioSource != null)
				{
					if (ambientSound.Parent != null)
					{
						ambientSound.AudioSource.transform.position = ambientSound.Parent.position;
						continue;
					}
					engine.Pooler.RecyclePoolObject(ambientSound.AudioSource.gameObject);
					ambientSounds.RemoveAt(i);
					i--;
				}
				else
				{
					ambientSounds.RemoveAt(i);
					i--;
				}
			}
		}
	}
}
