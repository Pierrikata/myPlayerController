using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float maxRunSpeed = 30;
    private Vector2 _moveInput;
    private bool _isMoving;
    private bool _faceRight = true;
    [SerializeField] private int facingDirection = 1;

    [Header("Acceleration")]
    [SerializeField] float runAcceleration = 3;
    [SerializeField] float runDeceleration = 4;
    [SerializeField] float accInAir = .15f;
    [SerializeField] float decInAir = .15f;

    [Header("Jump")]
    [SerializeField] int extraJumpsValue = 1;
    [SerializeField] float jumpForce = 12;
    [SerializeField] float groundCheckRadius = .25f;
    [SerializeField] Transform groundCheck;
    [SerializeField] LayerMask whatIsGround; // determines what layer the player interacts with
    private int _extraJumps;
    private bool _isGrounded;
    private bool _canJump;
    private float _jumpTimer;
    private float _turnTimer;

    [Header("WallSlide")]
    [SerializeField] float wallSlideSpeed = 1;
    [SerializeField] float wallCheckRadius = .5f;
    [SerializeField] Transform wallCheck;
    private bool _isWallTouch;
    private bool _wallSliding;
    
    [Header("WallJump")]
    private float _touchingWallValue;
    private float _wallJumpTimer;
    [SerializeField] float wallJumpTimerSet;
    [SerializeField] float wallJumpDirection = -1;
    [SerializeField] Vector2 wallJumpForce;
    private bool _wallJumping;
    private int _lastWallJumpDirection;

    private const bool CanClimbLedge = false;

    [Header("Other")]
    private Animator _anim;
    private Rigidbody2D _rb;
    
    // set rigidbody and animator in very first frame of program
    void Start()
    {
        _extraJumps = extraJumpsValue;

        _anim = GetComponent<Animator>(); // to manipulate our player's animator
        _rb = GetComponent<Rigidbody2D>(); // can tweak and use our player's rigidbody
        wallJumpForce.Normalize(); // ???
    }

    // Called once per frame, is used to manage all physics-related aspects of your game
    private void FixedUpdate()
    {
        Inputs();
        CheckWorld();
        Run();
        //WallSlide();
        //WallJump();

        /**/
    }
    private void Update() // Update is called once per frame ... NOTE: I set to private
    {
        // Inputs();
        
        Jump();
        //WallSlide();
        WallSlide();
        WallJump();
        UpdateAnimations();
    }

    void Inputs()
    {
        _moveInput.x = Input.GetAxisRaw("Horizontal"); // built-in Unity input field (e.g. holding right arrow key -> moveInput = 1)
        _moveInput.y = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(KeyCode.Space))
            _canJump = true;
    }
    void CheckWorld()
    {
        _isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, whatIsGround);
        _isWallTouch = Physics2D.OverlapCircle(wallCheck.position, wallCheckRadius, whatIsGround);
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
        if (Mathf.Abs(_rb.velocity.x) >= maxRunSpeed - 1
            && Mathf.Sign(_rb.velocity.x) == Mathf.Sign(targetSpeed)
            && !_isGrounded)
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
        
        if(!_faceRight && _moveInput.x > 0)
            Flip();
        else if(_faceRight && _moveInput.x < 0)
            Flip();
    }
    private void Flip()
    {
        /*if (!_wallSliding)
        {
            wallJumpDirection *= -1;
            _faceRight = !_faceRight;
            transform.Rotate(0, 180, 0);
        }*/
        
        _faceRight = !_faceRight;
        var transform1 = transform;
        Vector3 scaler = transform1.localScale; // set Scaler to player's local xyz scale values
        scaler.x *= -1;
        transform1.localScale = scaler;
    }
    private void Jump()
    {
        if (_isGrounded || _isWallTouch)
            _extraJumps = extraJumpsValue;

        if (Input.GetKeyDown(KeyCode.Space) && _extraJumps > 0)
        {
            _rb.velocity = new Vector2(_rb.velocity.x, jumpForce);
            _extraJumps--;
        }
        else if (Input.GetKeyDown(KeyCode.Space) && _isGrounded)
            _rb.velocity = new Vector2(_rb.velocity.x, jumpForce);
    }
    private void WallSlide()
    {
        if (_isWallTouch && !_isGrounded && _rb.velocity.y < 0)
            _wallSliding = true;
        else
            _wallSliding = false;
        if (_wallSliding)
            _rb.velocity = new Vector2(_rb.velocity.x, -wallSlideSpeed);
    }
    private void CheckIfWallSliding()
    {
        if(_isWallTouch && _rb.velocity.y < 0 && !CanClimbLedge)
            _wallSliding = true;
        else
            _wallSliding = false;
    }
    private void WallJump()
    {
        if (_wallSliding && Input.GetKeyDown(KeyCode.Space))
        {
            _rb.AddForce(new Vector2(wallJumpForce.x * wallJumpDirection, wallJumpForce.y),ForceMode2D.Impulse);
            Flip();
        } 
    }
    private void StopWallJump()
    {
        _wallJumping = false;
        throw new System.NotImplementedException();
    }
    private void UpdateAnimations()
    {
        // run
        _anim.SetBool("isRunning", _isMoving);
        _anim.SetFloat("xVelocity", Mathf.Abs(_rb.velocity.x)); // SPRINT
        
        // jump/fall
        _anim.SetBool("isGrounded", _isGrounded);
        _anim.SetFloat("yVelocity", _rb.velocity.y);
        
        // wall
        _anim.SetBool("wallLanding", _isWallTouch);
        _anim.SetBool("wallSliding",_wallSliding);
        _anim.SetBool("wallJump",_wallJumping);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        Gizmos.DrawWireSphere(wallCheck.position, wallCheckRadius);
        throw new NotImplementedException();
    }
}
