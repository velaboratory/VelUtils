using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class XRSetFloorHeight : MonoBehaviour
{
	void Start()
	{
		List<XRInputSubsystem> lst = new List<XRInputSubsystem>();
		SubsystemManager.GetSubsystems(lst);
		for (int i = 0; i < lst.Count; i++)
		{
			lst[i].TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);
		}
	}
}
