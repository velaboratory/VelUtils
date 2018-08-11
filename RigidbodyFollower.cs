using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RigidbodyFollower : MonoBehaviour {

    public Transform rightController;

    void FixedUpdate()
    {
        GetComponent<Rigidbody>().velocity = (rightController.position - transform.position) / Time.deltaTime;
        transform.rotation = rightController.rotation;
        transform.Rotate(0, -90, -90);
    }
}
