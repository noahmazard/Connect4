using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCamera : MonoBehaviour
{
    public GameObject targetGameObject;
    // Update is called once per frame
    void Update()
    {
        Vector3 tmin = targetGameObject.GetComponent<Renderer>().bounds.min;
        Vector3 tmax = targetGameObject.GetComponent<Renderer>().bounds.max;

        // we would like to keep tmin and tmax constant
        float c = (tmax.x - tmin.x) / 2;

        if (Camera.main.transform.position.z > .0001 || Camera.main.transform.position.z < -.0001)
            Camera.main.fieldOfView = Mathf.Abs(4f * 180.0f / 3.14159f * Mathf.Atan(c / Camera.main.transform.position.z));
        if (Camera.main.fieldOfView < 0.01f)
            Camera.main.fieldOfView = 0.01f;
        if (Camera.main.fieldOfView > 180.0f)
            Camera.main.fieldOfView = 180.0f;
    }
}
