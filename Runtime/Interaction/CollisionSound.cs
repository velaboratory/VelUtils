using UnityEngine;

namespace unityutilities.Interaction
{
	public class CollisionSound : MonoBehaviour
	{
		public AudioSource collisionSound;
		[Range(0, 1f)] public float volumeMultiplier = 1;
		public bool dynamicVolume = true;

		private float startTime;

		private void Awake()
		{
			startTime = Time.time;
		}

		private void OnCollisionEnter(Collision other)
		{
			if (collisionSound && Time.time - startTime > 1)
			{
				float volume = other.relativeVelocity.magnitude / 8f;
				volume = Mathf.Clamp01(volume);
				if (dynamicVolume) collisionSound.volume = volume * volumeMultiplier;
				collisionSound.Play();
			}
		}
	}
}