using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;
using UnityEngine.Rendering;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    private CharacterController Controller;

    [SerializeField] private GameObject AimTarget;

    private float cameraRotationY;
    private float cameraRotationX;
    private const float RotationSpeed = 3f;

    [SerializeField] private float WalkingSpeed = 10f;
    private LayerMask ground;

    private const float CheckGroundRadius = 0.3f;
    [SerializeField] private float JumpHeight = 2.0f;
    [SerializeField] private float falloff = 2.0f;
    [SerializeField] private float SprintMultiplier = 1.5f;

    private readonly float normalGravity = -2.0f;
    private float currentGravity = 0.0f;

    private float jumpVelocity;


    void Start()
    {
        Controller = GetComponent<CharacterController>();
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Jump();
        }

        UpdateRotation();
        Move();

        // interact
        if (Input.GetKeyDown(KeyCode.E))
        {
            //playerInteractor.Interact();
        }

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            //playerCharacter.Attack();
        }


    }
    private void Jump()
    {
        jumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(normalGravity) * JumpHeight);
        currentGravity = jumpVelocity;
        Controller.Move(Vector3.up * 0.1f);
    }

    private void Move()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        float gravity;


        //control basic gravity
        if (!Controller.isGrounded)
        {
            gravity = currentGravity -= falloff * Time.deltaTime;
        }
        else
        {
            currentGravity = normalGravity;
            gravity = normalGravity;
        }

        // Determine the current speed based on whether the player is sprinting
        float currentSpeed = WalkingSpeed;
        if (Input.GetKey(KeyCode.LeftShift))
        {
            currentSpeed *= SprintMultiplier;
        }

        Vector3 move = transform.right * x + transform.forward * z;
        move *= currentSpeed;
        move += transform.up * gravity;

        Controller.Move(move * Time.deltaTime);

    }



    private void UpdateRotation()
    {
        float MouseX = Input.GetAxis("Mouse X");
        float MouseY = Input.GetAxis("Mouse Y");

        cameraRotationY -= MouseY * RotationSpeed;
        cameraRotationY = Mathf.Clamp(cameraRotationY, -90.0f, 90.0f);
        cameraRotationX = Mathf.Clamp(cameraRotationX, -100, 100);

        //Rotate the body (left right) and the head (up down)
        transform.Rotate(Vector3.up * (MouseX * RotationSpeed));
        AimTarget.transform.localEulerAngles = Vector3.right * cameraRotationY;

    }
}