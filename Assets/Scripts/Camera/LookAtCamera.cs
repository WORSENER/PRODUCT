using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    public Camera Camera;
    private Vector3 target=new Vector3();
    private void Update()
    {
        target.x=Camera.transform.position.x;
        target.z=Camera.transform.position.z;
        target.y=transform.position.y;
        transform.LookAt(target);
    }
}
