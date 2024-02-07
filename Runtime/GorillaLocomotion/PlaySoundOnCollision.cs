using UnityEngine;

namespace GorillaLocomotion
{
	[RequireComponent(typeof(AudioSource))]
	public class PlaySoundOnCollision : MonoBehaviour
	{
		private AudioSource source;

		private void Start()
		{
			source = GetComponent<AudioSource>();
		}

		private void OnCollisionEnter(Collision other)
		{
			source.Play();
		}
	}
}