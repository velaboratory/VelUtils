using UnityEngine;

public class MonkeHand : MonoBehaviour
{
	public Collision lastCollision { get; set; }

	private void OnCollisionStay(Collision other)
	{
		lastCollision = other;
	}

	private void OnCollisionExit(Collision other)
	{
		lastCollision = null;
	}
}