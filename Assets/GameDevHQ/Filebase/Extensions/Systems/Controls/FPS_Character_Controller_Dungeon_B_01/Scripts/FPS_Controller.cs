using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameDevHQ.FileBase.Plugins.FPS_Character_Controller;

namespace GameDevHQ.FileBase.Plugins.FPS_Character_Controller
{
    [RequireComponent(typeof(CharacterController))]
    public class FPS_Controller : MonoBehaviour
    {
        [Header("Controller Info")]
        [SerializeField ][Tooltip("How fast can the controller walk?")]
        public float _walkSpeed = 1.0f; //how fast the character is walking
        [SerializeField][Tooltip("How fast can the controller run?")]
        private float _runSpeed = 2.0f; // how fast the character is running
        [SerializeField][Tooltip("Set your gravity multiplier")] 
        private float _gravity = 1.0f; //how much gravity to apply 
        [SerializeField][Tooltip("How high can the controller jump?")]
        private float _jumpHeight = 15.0f; //how high can the character jump
        [SerializeField]
        private bool _crouching = false; //bool to display if we are crouched or not

        private CharacterController _controller; //reference variable to the character controller component
        private float _yVelocity = 0.0f; //cache our y velocity


        [Header("Headbob Settings")]

        private Animator _anim;
        private float _speedMultForTimeline;
        private bool _isGrounded;
        private FootstepSFX _footstepSFX;

        [SerializeField][Tooltip("Smooth out the transition from moving to not moving")]
        private float _smooth = 20.0f; //smooth out the transition from moving to not moving
        [SerializeField][Tooltip("How quickly the player head bobs")]
        private float _walkFrequency = 4.8f; //how quickly the player head bobs when walking
        [SerializeField][Tooltip("How quickly the player head bobs")]
        private float _runFrequency = 7.8f; //how quickly the player head bobs when running
        [SerializeField][Tooltip("How dramatic the headbob is")][Range(0.0f, 0.2f)]
        private float _heightOffset = 0.05f; //how dramatic the bobbing is
        private float _timer = Mathf.PI / 2; //This is where Sin = 1 -- used to simulate walking forward. 
        private Vector3 _initialCameraPos; //local position where we reset the camera when it's not bobbing

        [Header("Camera Settings")]
        [SerializeField][Tooltip("Control the look sensitivty of the camera")]
        private float _lookSensitivity = 5.0f; //mouse sensitivity 

        private Camera _fpsCamera;
        private void Start()
        {
            _controller = GetComponent<CharacterController>(); //assign the reference variable to the component
            _fpsCamera = GetComponentInChildren<Camera>();
            _initialCameraPos = _fpsCamera.transform.localPosition;
            _anim = transform.Find("Main Camera").GetComponentInChildren<Animator>();
            _footstepSFX = transform.Find("Main Camera").GetComponentInChildren<FootstepSFX>();
            Cursor.visible = false;
        }

        private void Update()
        {
            FPSController();
            CameraController();
            HeadBobbing();
            
      
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                _controller.height = 2.0f;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                _controller.height = 1.0f;
            }
        }

        void FPSController()
        {
            float h = Input.GetAxis("Horizontal"); //horizontal inputs (a, d, leftarrow, rightarrow)
            float v = Input.GetAxis("Vertical"); //veritical inputs (w, s, uparrow, downarrow)

            Vector3 direction = new Vector3(h, 0, v); //direction to move
            Vector3 velocity = direction * _walkSpeed; //velocity is the direction and speed we travel

            if (Input.GetKeyDown(KeyCode.C))
            {
                _crouching = !_crouching;

                if (_crouching == true)
                {
                    _controller.height = 2.0f;
                }
                else
                {
                    _controller.height = 1.0f;
                }
                
            }

            if (Input.GetKey(KeyCode.LeftShift) && _crouching == false) //check if we are holding down left shift
            {
                velocity = direction * _runSpeed; //use the run velocity 
                _isGrounded = true;
            }

            if (_controller.isGrounded == true) //check if we're grounded
            {
                _footstepSFX.walkVolume = 1.0f;  //turn off walk volume
                if (Input.GetKeyDown(KeyCode.Space)) //check for the space key
                {
                    _yVelocity = _jumpHeight; //assign the cache velocity to our jump height
                    _footstepSFX.PlayJumpSound();
                }
            }
            else //we're not grounded
            {
                _yVelocity -= _gravity; //subtract gravity from our yVelocity 
                _isGrounded = false;
                _footstepSFX.walkVolume = 0.0f;  //turn on walk volume

            }

            velocity.y = _yVelocity; //assign the cached value of our yvelocity

            velocity = transform.TransformDirection(velocity);

            _controller.Move(velocity * Time.deltaTime); //move the controller x meters per second
        }

        void CameraController()
        {
            float mouseX = Input.GetAxis("Mouse X"); //get mouse movement on the x
            float mouseY = Input.GetAxis("Mouse Y"); //get mouse movement on the y

            Vector3 rot = transform.localEulerAngles; //store current rotation
            rot.y += mouseX * _lookSensitivity; //add our mouseX movement to the y axis
            transform.localRotation = Quaternion.AngleAxis(rot.y, Vector3.up); ////rotate along the y axis by movement amount

            Vector3 camRot = _fpsCamera.transform.localEulerAngles; //store the current rotation
            camRot.x += -mouseY * _lookSensitivity; //add the mouseY movement to the x axis
            _fpsCamera.transform.localRotation = Quaternion.AngleAxis(camRot.x, Vector3.right); //rotate along the x axis by movement amount
        }

        void HeadBobbing()
        {
            float h = Input.GetAxis("Horizontal"); //horizontal inputs (a, d, leftarrow, rightarrow)
            float v = Input.GetAxis("Vertical"); //veritical inputs (w, s, uparrow, downarrow)

            if (h != 0 || v != 0) //Are we moving?
            {
                _anim.SetBool("IsWalking", true); // start walking animation head bob
                _speedMultForTimeline = _walkSpeed * 1.1f;//adjust speed variable in animator to scale with different speeds
                _anim.SetFloat("SpeedFactor", _speedMultForTimeline);//change speed playback in animator

                if (_isGrounded == true)
                {
                    if (Input.GetKey(KeyCode.LeftShift))
                    {
                        _anim.SetBool("IsRunning", true);  //start running animation head bob
                        _speedMultForTimeline = _walkSpeed * 1.2f;  //adjust speed variable in animator to scale with different speeds
                        _anim.SetFloat("SpeedFactor", _speedMultForTimeline);  //change speed playback in animator
                    }

                    else
                    {
                        _anim.SetBool("IsRunning", false);//turn off running when shift is not pressed       
                        _anim.SetBool("Idle", true);
                    }
                    
                }

                if (_isGrounded == false)
                {
                    if (Input.GetKey(KeyCode.LeftShift))
                    {
                        _anim.SetBool("IsRunning", false);//turn off walking animation if we are not moving
                        _speedMultForTimeline = _walkSpeed * 1.2f;  //adjust speed variable in animator to scale with different speeds
                        _anim.SetFloat("SpeedFactor", _speedMultForTimeline);  //change speed playback in animator
                    }
                }

                /*
           
                if (Input.GetKey(KeyCode.LeftShift) && _isGrounded == false)
                    {
                        _anim.SetBool("isRunning", false);  //go to idle head bob
                        _anim.SetBool("Idle", true);
                        _speedMultForTimeline = _walkSpeed * 1.1f;  //adjust speed variable in animator to scale with different speeds
                        _anim.SetFloat("SpeedFactor", _speedMultForTimeline);  //change speed playback in animator
                    Debug.Log("did animation reset to idle?");
                    }

                else
                    {
                        _anim.SetBool("IsRunning", false);//turn off running when shift is not pressed       
                        //_anim.SetBool("Idle", true);
                    }    */
            }
            else
            {
                _anim.SetBool("IsWalking", false);//turn off walking animation if we are not moving
                _anim.SetFloat("SpeedFactor", 1.0f);//reset speed factor back to 1.0f
                
            }
        }
    }
}

