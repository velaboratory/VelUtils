using UnityEngine;

public class CollisionSound : MonoBehaviour
{
	public AudioSource collisionSound;

	private void OnCollisionEnter(Collision other)
	{
		if (collisionSound && Time.timeSinceLevelLoad > 1)
		{
			float volume = other.relativeVelocity.magnitude / 8f;
			volume = Mathf.Clamp01(volume);
			//collisionSound.volume = volume;
			collisionSound.Play();

		}
	}
}
