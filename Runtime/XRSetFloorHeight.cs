using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XRSetFloorHeight : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        UnityEngine.XR.XRDevice.SetTrackingSpaceType(UnityEngine.XR.TrackingSpaceType.RoomScale);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
