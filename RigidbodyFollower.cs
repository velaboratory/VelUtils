using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RigidbodyFollower : MonoBehaviour
{
	public enum FollowType
	{
		Copy,
		Velocity,
		Force
	}
	
	public Transform target;
	
	[Header("Position")]
	public bool followPosition;
	public FollowType positionFollowType;
	public float positionForceMult;

	public Vector3 positionOffset;
	
	
	[Header("Rotation")]
	public bool followRotation;
	public FollowType rotationFollowType;
	public float rotationForceMult;

	public Vector3 rotationOffset;


	private void Update()
	{
		if (!target)
		{
			Debug.Log("No target set!");
			return;
		}
		
		if (followPosition && positionFollowType == FollowType.Copy)
		{
			transform.position = target.position + positionOffset;
		}

		if (followRotation && rotationFollowType == FollowType.Copy)
		{
			transform.rotation = target.rotation;
			transform.Rotate(rotationOffset);
		}
	}

	void FixedUpdate()
	{
		if (!target)
		{
			Debug.Log("No target set!");
			return;
		}
		
		if (followPosition && positionFollowType == FollowType.Copy)
		{
			GetComponent<Rigidbody>().velocity = (target.position - transform.position) / Time.fixedDeltaTime;
		}
		
		if (followPosition && positionFollowType == FollowType.Force) {
				GetComponent<Rigidbody>().AddForce((target.position - transform.position) * positionForceMult);
		}

		if (followRotation && rotationFollowType == FollowType.Velocity)
		{
			transform.rotation = target.rotation;
			transform.Rotate(rotationOffset);
		}
		
		if (followRotation && rotationFollowType == FollowType.Force)
		{
			transform.rotation = target.rotation;
			transform.Rotate(rotationOffset);
		}
	}
}