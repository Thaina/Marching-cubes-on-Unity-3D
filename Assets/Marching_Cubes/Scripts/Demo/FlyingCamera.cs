using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingCamera : MonoBehaviour
{
    public float speed = 10;
    public float rotationSpeed = 10;
    public bool hideMouse = true;
    private float yaw = 0.0f;
    private float pitch = 0.0f;
    private Vector2 pitchClamp = new Vector2(-70,80);

    void Start()
    {
        if (hideMouse)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.Locked;
        }
        yaw = transform.eulerAngles.y;
        pitch = transform.eulerAngles.x;
    }

    // Update is called once per frame
    void Update()
    {
        var dir = Input.GetAxis("Vertical") * transform.forward + Input.GetAxis("Horizontal") * transform.right;
        if(Input.GetKey(KeyCode.Space))
            dir += Vector3.up;
        else if(Input.GetKey(KeyCode.LeftShift))
            dir -= Vector3.up;

        transform.position += (dir * speed * Time.deltaTime);

        yaw += rotationSpeed * Input.GetAxis("Mouse X");
        pitch -= rotationSpeed * Input.GetAxis("Mouse Y");
        pitch = Mathf.Clamp(pitch , pitchClamp.x, pitchClamp.y);

        transform.eulerAngles = new Vector3(pitch, yaw, 0.0f);
    }
}
