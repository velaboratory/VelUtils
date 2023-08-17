using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VelUtils.Interaction.WorldMouse;

public class ColliderWorldMouse : WorldMouse
{
	public string needsTag;

	private Vector3 lastTouchPos;
	private Vector3 lastTouchDir;

	private void OnCollisionEnter(Collision collision)
	{
		if (string.IsNullOrEmpty(needsTag) || collision.collider.CompareTag(needsTag))
		{
			Debug.Log("TOUCH");
			Enable();
			Press();
		}
	}

	private void OnCollisionExit(Collision collision)
	{
		if (string.IsNullOrEmpty(needsTag) || collision.collider.CompareTag(needsTag))
		{
			Debug.Log("RELEASE");
			Release();
			Disable();
		}
	}

	private void OnCollisionStay(Collision collision)
	{
		lastTouchPos = collision.GetContact(0).point;
		lastTouchDir = collision.GetContact(0).normal;
	}
}