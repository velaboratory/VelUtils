using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VRPhysicalButton : Button
{
	public float depth = .05f;
	public Rigidbody rb;
	public float forceMultiplier = 1;
	private bool clicked;

	protected override void Start()
	{
		base.Start();
		rb = GetComponentInChildren<Rigidbody>();
	}

	// Update is called once per frame
	void FixedUpdate()
	{
		if (!clicked && rb.transform.localPosition.y < -depth * .9f)
		{
			onClick.Invoke();
			clicked = true;
		}
		else if (rb.transform.localPosition.y > -depth * .8f)
		{
			clicked = false;
		}

		// limit movement
		if (rb.transform.localPosition.y < -depth)
		{
			rb.transform.localPosition = new Vector3(0, -depth, 0);
		} else if (rb.transform.localPosition.y > 0)
		{
			rb.transform.localPosition = new Vector3(0, 0, 0);
		}

		if (rb.transform.localPosition.y < 0)
		{
			rb.AddForce(0, 100 * Time.fixedDeltaTime * forceMultiplier, 0);
		}
	}
}
