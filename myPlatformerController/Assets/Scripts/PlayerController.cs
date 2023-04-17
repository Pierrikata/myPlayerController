using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerController : MonoBehaviour
{
    [FormerlySerializedAs("speed")] public float maxRunSpeed;
    public float jumpForce;
    private float _moveInputX;

    public float runAcceleration, runDeceleration,accInAir, decInAir;

    private Rigidbody2D _rb;

    private bool _faceRight = true;

    private bool _isGrounded;
    public Transform groundCheck;
    public float checkRadius;
    public LayerMask whatIsGround; // determines what layer the player interacts with

    [Header("Wall Jump System")]
    public Transform wallCheck;
    private bool _isWallTouch;
    private bool _wallSliding;
    public float wallSlideSpeed;
    
    public float wallJumpDuration;
    public Vector2 wallJumpForce;
    private bool _wallJumping;

    private int _extraJumps;
    public int extraJumpsValue;

    private Animator _anim;
    
    // set rigidbody and animator in very first frame of program
    void Start()
    {
        _extraJumps = extraJumpsValue;

        _anim = GetComponent<Animator>(); // to manipulate our player's animator
        _rb = GetComponent<Rigidbody2D>(); // can tweak and use our player's rigidbody
    }

    // Called once per frame, is used to manage all physics-related aspects of your game
    private void FixedUpdate()
    {
        _isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, whatIsGround);
        _isWallTouch = Physics2D.OverlapCircle(wallCheck.position, checkRadius, whatIsGround);
        _moveInputX = Input.GetAxisRaw("Horizontal"); // built-in Unity input field (e.g. holding right arrow key -> moveInput = 1)
        
        Run();
        //WallSlide();
    }
    void Update() // Update is called once per frame
    {
        Jump();
        WallSlide();
        UpdateAnimations();

        if(_faceRight == false && _moveInputX > 0)
            Flip();
        else if(_faceRight == true && _moveInputX < 0)
            Flip();
    }

    private void WallSlide()
    {
        if (_isWallTouch && !_isGrounded && _moveInputX != 0)
            _wallSliding = true;
        else
            _wallSliding = false;
        if (_wallSliding)
            _rb.velocity = new Vector2(_rb.velocity.x, Mathf.Clamp(_rb.velocity.y, -wallSlideSpeed, float.MaxValue));
        if (_wallJumping)
            _rb.velocity = new Vector2(-_moveInputX * wallJumpForce.x, wallJumpForce.y);
    }
    private void Jump()
    {
        if (_isGrounded)
            _extraJumps = extraJumpsValue;
        
        if (Input.GetKeyDown(KeyCode.Space) && _extraJumps > 0)
        {
            _rb.velocity = new Vector2(_rb.velocity.x, jumpForce);
            _extraJumps--;
        }
        else if (Input.GetKeyDown(KeyCode.Space) && _isGrounded)
            _rb.velocity = new Vector2(_rb.velocity.x, jumpForce);
        
        if (Input.GetKeyDown(KeyCode.Space) && _wallSliding)
        {
            _wallJumping = true;
            _rb.velocity = new Vector2(-_moveInputX * wallJumpForce.x, wallJumpForce.y);
            Invoke("StopWallJump", wallJumpDuration);
        }
    }

    private IEnumerator StopWallJump()
    {
        _wallJumping = false;
        throw new System.NotImplementedException();
    }
    private void Run()
    {
        // calculate the direction we want to move in and our desired velocity
        float targetSpeed = _moveInputX * maxRunSpeed;
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
    }
    private void Flip()
    {
        _faceRight = !_faceRight;
        var transform1 = transform;
        Vector3 scaler = transform1.localScale; // set Scaler to player's local xyz scale values
        scaler.x *= -1;
        transform1.localScale = scaler;
    }

    private void UpdateAnimations()
    {
        // directional input triggers running animation
        if(_moveInputX == 0)
            _anim.SetBool("isRunning", false);
        else
            _anim.SetBool("isRunning", true);
        _anim.SetFloat("xVelocity", Mathf.Abs(_rb.velocity.x));
        
        // not being grounded triggers a jump/fall animation
        _anim.SetBool("isGrounded", _isGrounded);
        _anim.SetFloat("yVelocity", _rb.velocity.y);
    }
}
