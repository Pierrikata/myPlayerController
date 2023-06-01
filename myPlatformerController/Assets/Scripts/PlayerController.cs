using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerController : MonoBehaviour
{
    [Header("CineMachine")]
    public CinemachineVirtualCamera virtualCamera;
    [SerializeField] float zoomOutSpeed = 1, zoomInSpeed = .2f, minOrthoSize = 4, maxOrthoSize = 10;
    private float _currentOrthoSize;
    
    [Header("Movement")]
    [SerializeField] float maxRunSpeed = 30;
    private Vector2 _moveInput;
    private bool _isMoving, _faceRight = true, _canFlip;
    [SerializeField] private int facingDirection = 1;

    [Header("Acceleration")]
    [SerializeField] float runAcceleration = 3;
    [SerializeField] float runDeceleration = 4;
    [SerializeField] float accInAir = .15f;
    [SerializeField] float decInAir = .15f;

    [Header("Jump")]
    [SerializeField] int extraJumpsValue = 1;
    // [SerializeField] float jumpStartTime;
    [SerializeField] private float jumpForce = 12, jumpHoldForce = 1, jumpAmplifier = .1f, jumpTimeThreshold = .5f, groundCheckRadius = .25f;
    [SerializeField] Transform groundCheck;
    [SerializeField] LayerMask whatIsGround; // determines what layer the player interacts with
    private int _extraJumps;
    private bool _isGrounded, _isJumping;
    [SerializeField] private bool canJump;
    private float _jumpTimer, _turnTimer;

    [Header("WallSlide")]
    [SerializeField] Transform wallCheck;
    [SerializeField] LayerMask whatIsWall;
    [SerializeField] float wallSlideSpeed = 1, wallCheckRadius = .5f;
    private bool _isWallTouch, _canWallSlide, _wallSliding;

    [Header("WallJump")]
    [SerializeField] float wallJumpMaxAngle = 45;
    [SerializeField] float wallJumpAmplifier = 2;
    private Vector2 _wallNorm;
    private float _touchingWallValue, _wallJumpTimer;
    private bool _canWallJump = false;
    private int _lastWallJumpDirection;

    [Header("LedgeClimb")]
    [SerializeField] Transform ledgeCheck;
    private bool _canClimbLedge = false, _ledgeDetected, _isTouchingLedge;
    private Vector2 _ledgePosBot, _ledgePos1, _ledgePos2;
    public float ledgeClimbXOffset1, ledgeClimbXOffset2, ledgeClimbYOffset1, ledgeClimbYOffset2;

    [Header("Other")]
    private Animator _anim;
    private Rigidbody2D _rb;
    private Collision2D _playerCollision;
    
    // set rigidbody and animator in very first frame of program
    void Start()
    {
        virtualCamera = GameObject.FindObjectOfType<CinemachineVirtualCamera>();
        _currentOrthoSize = virtualCamera.m_Lens.OrthographicSize;
        
        _extraJumps = extraJumpsValue;

        _anim = GetComponent<Animator>(); // to manipulate our player's animator
        _rb = GetComponent<Rigidbody2D>(); // can tweak and use our player's rigidbody
    }

    // Called once per frame, is used to manage all physics-related aspects of your game
    private void FixedUpdate()
    {
        Inputs();
        CheckSurroundings();
        Run();
        WallSlide();
        //WallJump();

        /**/
    }
    private void Update() // Update is called once per frame ... NOTE: I set to private
    {
        // Inputs();

        DynamicCameraZoom();
        FlipController();
        Jump();
        WallJump();
        //CheckIfCanWallSlide();
        UpdateAnimations();
    }

    private void DynamicCameraZoom()
    {
        // Calculate the target orthographic size based on the magnitude of rigidbody velocity
        float targetOrthoSize = Mathf.Lerp(minOrthoSize, maxOrthoSize, _rb.velocity.magnitude / maxRunSpeed),
            zoomSpeed = _rb.velocity.magnitude > _currentOrthoSize ? zoomOutSpeed : zoomInSpeed;
        // Gradually adjust camera's orthographic size towards the target size
        _currentOrthoSize = Mathf.Lerp(_currentOrthoSize, targetOrthoSize, Time.deltaTime * zoomSpeed);
        virtualCamera.m_Lens.OrthographicSize = _currentOrthoSize;
    }

    void Inputs()
    {
        _moveInput.x = Input.GetAxisRaw("Horizontal"); // built-in Unity input field (e.g. holding right arrow key -> moveInput = 1)
        _moveInput.y = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(KeyCode.Space))
        {
            canJump = true;
        }
    }
    void CheckSurroundings()
    {
        _isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, whatIsGround);
        _isWallTouch = Physics2D.OverlapCircle(wallCheck.position, wallCheckRadius, whatIsWall);
        //SOMETHING WRONG HERE: _isTouchingLedge = Physics2D.Raycast(ledgeCheck.position, transform.right, wallCheckRadius * 2, whatIsGround);

        if(_isWallTouch)
            Debug.Log("wall is detected");
        if (_isWallTouch && !_isTouchingLedge && !_ledgeDetected)
        {
            _ledgeDetected = true;
            _ledgePosBot = wallCheck.position;
        }
    }
    private void Run()
    {
        // for UpdateAnimations()
        if (_moveInput.x != 0 && _rb.velocity.x != 0)
            _isMoving = true;
        else
            _isMoving = false;
        
        // calculate the direction we want to move in and our desired velocity
        float targetSpeed = _moveInput.x * maxRunSpeed;
        // calculate difference b/w current velocity and desired velocity
        float speedDif = targetSpeed - _rb.velocity.x;
        // change acceleration rate depending on situation
        float accelRate = 0;

        if (_isGrounded)
            accelRate = (Mathf.Abs(targetSpeed) > .01f) ? runAcceleration : runDeceleration;
        else
            accelRate = (Mathf.Abs(targetSpeed) > .01f) ? runAcceleration * accInAir : runDeceleration * decInAir;
        
        //Not used since no jump implemented here, but may be useful if you plan to implement your own
        /* 
        #region Add Bonus Jump Apex Acceleration
        //Increase are acceleration and maxSpeed when at the apex of their jump, makes the jump feel a bit more bouncy, responsive and natural
        if ((IsJumping || IsWallJumping || _isJumpFalling) && Mathf.Abs(RB.velocity.y) < Data.jumpHangTimeThreshold)
        {
            accelRate *= Data.jumpHangAccelerationMult;
            targetSpeed *= Data.jumpHangMaxSpeedMult;
        }
        #endregion
        */
        #region Conserve Momentum

        if (Mathf.Abs(_rb.velocity.x) >= maxRunSpeed - 1 && 
            Mathf.Sign(_rb.velocity.x) - Mathf.Sign(targetSpeed) == 0 &&
            !_isGrounded)
            accelRate = 0;
        
        #endregion
        
        /* applies acceleration to speed difference, then raises to a set power so acceleration increases
             with higher speeds */
        float movement = speedDif * accelRate;
        
        //applies force to rigidbody, multiplying by Vector2.right so that it only affects X axis
        _rb.AddForce(movement * Vector2.right, ForceMode2D.Force);
        
        /*
		 * For those interested here is what AddForce() will do
		 * RB.velocity = new Vector2(RB.velocity.x + (Time.fixedDeltaTime  * speedDif * accelRate) / RB.mass, RB.velocity.y);
		 * Time.fixedDeltaTime is by default in Unity 0.02 seconds equal to 50 FixedUpdate() calls per second
		*/
    }

    void FlipController()
    {
        if((!_faceRight && _moveInput.x > 0) || (_faceRight && _moveInput.x < 0))
        {
            _canFlip = true;
            Flip();
        }
        else if ((!_faceRight && _rb.velocity.x > 0) || (_faceRight && _rb.velocity.x < 0))
        {
            Flip();
        }
    }

    private void Flip()
    {
        if (_canFlip)
        {
            _faceRight = !_faceRight;
            var transform1 = transform;
            Vector3 scaler = transform1.localScale; // set Scaler to player's local xyz scale values
            scaler.x *= -1;
            transform1.localScale = scaler;
        }
    }
    private void Jump()
    {
        if (_isGrounded || _isWallTouch)
            _extraJumps = extraJumpsValue;

        if (Input.GetButtonDown("Jump") && (_extraJumps > 0 || _isGrounded))
        {
            if (!_isJumping)
            {
                _isJumping = true;
                _jumpTimer = Time.time;
                if(_isGrounded)
                    _rb.AddForce(new Vector2(_rb.velocity.x * jumpAmplifier / 10, jumpForce * jumpAmplifier), ForceMode2D.Impulse);
                else
                {
                    if (Mathf.Sign(_moveInput.x) - Mathf.Sign(_rb.velocity.x) != 0)
                        _rb.velocity = new Vector2(-_rb.velocity.x, jumpForce * jumpAmplifier);
                    else
                        _rb.velocity = new Vector2(_rb.velocity.x, jumpForce * jumpAmplifier);
                    _extraJumps--;
                }
            }
        }
        else if (Input.GetButton("Jump") && _isJumping)
        {
            if(Time.time - _jumpTimer < jumpTimeThreshold)
                _rb.AddForce(new Vector2(0, jumpHoldForce * jumpAmplifier), ForceMode2D.Impulse);
        }
        else if (Input.GetButtonUp("Jump"))
            _isJumping = false;
    }
    private void WallJump()
    {
        if(_canWallJump && Input.GetButtonDown("Jump"))
        {
            _isJumping = true;
            float dotProduct = Vector2.Dot(_wallNorm, Vector2.right);
            Vector2 jumpDirection = (dotProduct >= 0) ? Vector2.left : Vector2.right;
            _rb.AddForce(
                new Vector2(-jumpDirection.x * jumpForce * jumpAmplifier * wallJumpAmplifier,
                    jumpDirection.y * jumpForce * jumpAmplifier * wallJumpAmplifier), ForceMode2D.Impulse);
            _extraJumps++;
            _canWallJump = false;
            _wallSliding = false;
            _canWallSlide = false;
        }
    }
    private void OnCollisionEnter2D(Collision2D other)
    {
        // Check if the collision is with a wall
        if (other.gameObject.CompareTag("Wall") || _isWallTouch)
        {
            _wallNorm = other.GetContact(0).normal;
            if (Vector2.Angle(Vector2.up, _wallNorm) >= wallJumpMaxAngle)
            {
                _canWallSlide = true;
                _canWallJump = true;
            }
        }
        else
        {
            _canWallSlide = false;
            _canWallJump = false;
        }

        if (_isGrounded)
            Debug.Log("grounded");
    }
    private void WallSlide()
    {
        if (_canWallSlide)
        {
            _wallSliding = true;
            _rb.AddForce(new Vector2(0, -wallSlideSpeed));
        }
        else
            _wallSliding = false;
    }
    private void CheckIfCanWallSlide()
    {
        if (!_isGrounded && _rb.velocity.y < 0)
            _canWallSlide = true;
        else
            _canWallSlide = false;
    }
    private void CheckLedgeClimb()
    {
        if (_ledgeDetected && !_canClimbLedge)
        {
            _canClimbLedge = true;

            if (_faceRight)
            {
                _ledgePos1 = new Vector2(Mathf.Floor(_ledgePosBot.x + wallCheckRadius * 2) - ledgeClimbXOffset1,
                    Mathf.Floor(_ledgePosBot.y) + ledgeClimbYOffset1);
                _ledgePos2 = new Vector2(Mathf.Floor(_ledgePosBot.x + wallCheckRadius * 2) + ledgeClimbXOffset2,
                    Mathf.Floor(_ledgePosBot.y) + ledgeClimbYOffset2);
            }
            else
            {
                _ledgePos1 = new Vector2(Mathf.Ceil(_ledgePosBot.x - wallCheckRadius * 2) + ledgeClimbXOffset1,
                    Mathf.Floor(_ledgePosBot.y) + ledgeClimbYOffset1);
                _ledgePos2 = new Vector2(Mathf.Ceil(_ledgePosBot.x - wallCheckRadius * 2) - ledgeClimbXOffset2,
                    Mathf.Floor(_ledgePosBot.y) + ledgeClimbYOffset2);
            }

            _isMoving = false;
            _canFlip = false;

            //_anim.SetBool("canClimbLedge", _canClimbLedge);
        }
        if (_canClimbLedge)
            transform.position = _ledgePos1;
    }
    public void FinishLedgeClimb()
    {
        _canClimbLedge = false;
        transform.position = _ledgePos2;
        _isMoving = true;
        _canFlip = true;
        _ledgeDetected = false;
        //_anim.SetBool("canClimbLedge", _canClimbLedge);
    }
    private void UpdateAnimations()
    {
        // run
        _anim.SetBool("isRunning", _isMoving);
        _anim.SetFloat("xVelocity", Mathf.Abs(_rb.velocity.x)); // SPRINT
        _anim.SetFloat("Idle X", Mathf.Abs(_rb.velocity.x));
        
        // jump/fall
        _anim.SetBool("isGrounded", _isGrounded);
        _anim.SetFloat("yVelocity", _rb.velocity.y);
        _anim.SetBool("isJumping/Falling", _isJumping);
        
        // wall
        _anim.SetBool("touchingWall", _isWallTouch);
        _anim.SetBool("wallSliding",_wallSliding);
        _anim.SetBool("wallJump",_canWallJump);
        
        // ledge
        //_anim.SetBool("canClimbLedge", _canClimbLedge);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        Gizmos.DrawWireSphere(wallCheck.position, wallCheckRadius);
        //throw new NotImplementedException();
    }
}
