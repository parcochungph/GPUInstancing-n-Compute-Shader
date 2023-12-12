using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    public float movementSpeed = 10f;
    public float fastMovementSpeed = 100f;
    public float freeLookSensitivity = 3f;
    public float zoomSensitivity = 10f;
    public float fastZoomSensitivity = 50f;

    private bool looking;

    private void Update()
    {
        var fastMode = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        var movementSpeed = fastMode ? fastMovementSpeed : this.movementSpeed;

        if (Input.GetKey(KeyCode.A)) transform.position = transform.position + -transform.right * movementSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.D)) transform.position = transform.position + transform.right * movementSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.W)) transform.position = transform.position + transform.forward * movementSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.S)) transform.position = transform.position + -transform.forward * movementSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.Space)) transform.position = transform.position + transform.up * movementSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.LeftControl)) transform.position = transform.position + -transform.up * movementSpeed * Time.deltaTime;

        var axis = Input.GetAxis("Mouse ScrollWheel");
        if (axis != 0)
        {
            var zoomSensitivity = fastMode ? fastZoomSensitivity : this.zoomSensitivity;
            transform.position = transform.position + transform.forward * axis * zoomSensitivity;
        }

        if (looking)
        {
            var newRotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * freeLookSensitivity;
            var newRotationY = transform.localEulerAngles.x - Input.GetAxis("Mouse Y") * freeLookSensitivity;
            transform.localEulerAngles = new Vector3(newRotationY, newRotationX, 0f);
        }

        if (Input.GetKeyDown(KeyCode.Mouse1))
            StartLooking();
        else if (Input.GetKeyUp(KeyCode.Mouse1)) StopLooking();
    }

    private void OnDisable()
    {
        StopLooking();
    }

    public void StartLooking()
    {
        looking = true;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void StopLooking()
    {
        looking = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}
