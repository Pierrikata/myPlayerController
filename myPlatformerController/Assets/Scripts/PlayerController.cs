using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed;
    public float jumpForce;
    private float moveInput;

    private Rigidbody2D rb;

    private bool faceRight = true;

    private bool isGrounded;
    public Transform groundCheck;
    public float checkRadius;
    public LayerMask whatIsGround;

    private int extraJumps;
    public int extraJumpsValue;

    // Start is called before the first frame update
    void Start()
    {
        extraJumps = extraJumpsValue;
        rb = GetComponent<Rigidbody2D>(); // can tweak and use our player's rigidbody
    }

    // Called once per frame, is used to manage all physics-related aspects of your game
    void FixedUpdate()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, whatIsGround);

        moveInput = Input.GetAxisRaw("Horizontal"); // built-in Unity input field (e.g. holding right arrow key -> moveInput = 1)

        rb.velocity = new Vector2 (moveInput * speed, rb.velocity.y); // NOTE: look at this function. Can you explain what is going on here

        if(faceRight == false && moveInput > 0)
            Flip();
        else if(faceRight == true && moveInput < 0)
            Flip();
    }
    void Update() // Update is called once per frame
    {
        if (isGrounded == true)
            extraJumps = extraJumpsValue;
        if (Input.GetKeyDown(KeyCode.Space) && extraJumps > 0)
        {
            rb.velocity = Vector2.up * jumpForce;
            extraJumps--;
        }
        else if (Input.GetKeyDown(KeyCode.Space) && extraJumps == 0 && isGrounded == true)
            rb.velocity = Vector2.up * jumpForce;
    }
    void Flip()
    {
        faceRight = !faceRight;
        Vector3 Scaler = transform.localScale; // set Scaler to player's local xyz scale values
        Scaler.x *= -1;
        transform.localScale = Scaler;
    }
}
