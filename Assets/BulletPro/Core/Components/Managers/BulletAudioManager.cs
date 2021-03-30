using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro
{
	[AddComponentMenu("BulletPro/Managers/Bullet Audio Manager")]
	public class BulletAudioManager : MonoBehaviour
	{
		public static BulletAudioManager instance;

		[Tooltip("A list of all AudioSources driven by this manager. Sound effects will play from them.")]
		public AudioSource[] sources;
		private int totalPlayed;

		public void Awake()
		{
			if (!instance) instance = this;
			else Debug.LogWarning("Beware : there is more than one instance of BulletAudioManager in the scene!");
		}

		public void PlayLocal(AudioClip clip)
		{
			if (sources == null) { Debug.LogWarning(name + " : No AudioSource found !"); return; }
			if (sources.Length == 0) { Debug.LogWarning(name + " : No AudioSource found !"); return; }

			// the clip cuts itself : if already played due to another shot, the previous one should be cancelled to avoid overlapping.
			int freeSource = -1;
			for (int i = 0; i < sources.Length; i++)
			{
				if (!sources[i].isPlaying)
				{
					if (freeSource == -1) freeSource = i;
					continue;
				}

				// if the source is playing, either it's already playing the right clip (then we re-play it), or we leave it be
				if (sources[i].clip == clip)
				{
					sources[i].Play();
					return;
				}
			}

			// otherwise, we play from a source that isn't playing.
			if (freeSource > -1)
			{
				totalPlayed++;
				sources[freeSource].clip = clip;
				sources[freeSource].Play();
				return;
			}

			// last resort: everything is taken, we play our SFX with the earliest used source.
			AudioSource source = sources[totalPlayed++ % sources.Length];
			source.clip = clip;
			source.Play();
		}

		// Simple Play function accessible at static level
		public static void Play(AudioClip clip)
		{
			if (instance)
				instance.PlayLocal(clip);
		}
	}
}