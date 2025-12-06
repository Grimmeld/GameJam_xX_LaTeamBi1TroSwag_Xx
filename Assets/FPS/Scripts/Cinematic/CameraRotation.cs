using System.Collections;
using UnityEngine;

public class CameraRotation : MonoBehaviour
{
public Camera camera;
public Camera playercamera;
private bool LaunchCinematic = false;

    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (camera.transform.rotation.eulerAngles.z < 180)
            //if(LaunchCinematic==true)
        {

            camera.transform.Rotate(new Vector3(0, 0, 1));

        }

        void OnTriggerEnter(Collider other)
        {
            LaunchCinematic = true;
        }

        IEnumerator LaunchCinemaric()
        {
            camera.depth = 1;
            
            yield return null;
        }
    }
}

