using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Movement : MonoBehaviour
{
    private Collision coll;
    [HideInInspector]
    public Rigidbody2D rb;
    private AnimationScript anim;

    [Space]
    [Header("Stats")]
    public float speed = 10;
    public float jumpForce = 50;
    public float slideSpeed = 5;
    public float wallJumpLerp = 10;
    public float dashSpeed = 20;

    [Space]
    [Header("Booleans")]
    public bool canMove;
    public bool wallGrab;
    public bool wallJumped;
    public bool wallSlide;
    public bool isDashing;

    [Space]

    private bool groundTouch;
    private bool hasDashed;

    public int side = 1;

    [Space]
    [Header("Polish")]
    public ParticleSystem dashParticle;
    public ParticleSystem jumpParticle;
    public ParticleSystem wallJumpParticle;
    public ParticleSystem slideParticle;

    // Start is called before the first frame update
    void Start()
    {
        coll = GetComponent<Collision>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<AnimationScript>();
    }

    // Update is called once per frame
    void Update()
    {
        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Vertical");
        float xRaw = Input.GetAxisRaw("Horizontal");
        float yRaw = Input.GetAxisRaw("Vertical");
        Vector2 dir = new Vector2(x, y);

        Walk(dir);
        anim.SetHorizontalMovement(x, y, rb.velocity.y);

        if (coll.onWall && Input.GetButton("Fire3") && canMove)
        {
            if(side != coll.wallSide)
                anim.Flip(side*-1);
            wallGrab = true;
            wallSlide = false;
        }

        if (Input.GetButtonUp("Fire3") || !coll.onWall || !canMove)
        {
            wallGrab = false;
            wallSlide = false;
        }

        if (coll.onGround && !isDashing)
        {
            wallJumped = false;
            GetComponent<BetterJumping>().enabled = true;
        }
        
        if (wallGrab && !isDashing)
        {
            rb.gravityScale = 0;
            if(x > .2f || x < -.2f)
            rb.velocity = new Vector2(rb.velocity.x, 0);

            float speedModifier = y > 0 ? .5f : 1;

            rb.velocity = new Vector2(rb.velocity.x, y * (speed * speedModifier));
        }
        else
        {
            rb.gravityScale = 3;
        }

        if(coll.onWall && !coll.onGround)
        {
            if (x != 0 && !wallGrab)
            {
                wallSlide = true;
                WallSlide();
            }
        }

        if (!coll.onWall || coll.onGround)
            wallSlide = false;

        if (Input.GetButtonDown("up"))
        {
            anim.SetTrigger("up");

            if (coll.onGround)
                Jump(Vector2.up, false);
            if (coll.onWall && !coll.onGround)
                WallJump();
        }

        if (Input.GetButtonDown("Fire1") && !hasDashed)
        {
            if(xRaw != 0 || yRaw != 0)
                Dash(xRaw, yRaw);
        }

        if (coll.onGround && !groundTouch)
        {
            GroundTouch();
            groundTouch = true;
        }

        if(!coll.onGround && groundTouch)
        {
            groundTouch = false;
        }

        WallParticle(y);

        if (wallGrab || wallSlide || !canMove)
            return;

        if(x > 0)
        {
            side = 1;
            anim.Flip(side);
        }
        if (x < 0)
        {
            side = -1;
            anim.Flip(side);
        }


    }

    void GroundTouch()
    {
        hasDashed = false;
        isDashing = false;

        side = anim.sr.flipX ? -1 : 1;

        jumpParticle.Play();
    }

    private void Dash(float x, float y)
    {
        Camera.main.transform.DOComplete();
        Camera.main.transform.DOShakePosition(.2f, .5f, 14, 90, false, true);
        FindObjectOfType<RippleEffect>().Emit(Camera.main.WorldToViewportPoint(transform.position));

        hasDashed = true;

        anim.SetTrigger("dash");

        rb.velocity = Vector2.zero;
        Vector2 dir = new Vector2(x, y);

        rb.velocity += dir.normalized * dashSpeed;
        StartCoroutine(DashWait());
    }

    IEnumerator DashWait()
    {
        FindObjectOfType<GhostTrail>().ShowGhost();
        StartCoroutine(GroundDash());
        DOVirtual.Float(14, 0, .8f, RigidbodyDrag);

        dashParticle.Play();
        rb.gravityScale = 0;
        GetComponent<BetterJumping>().enabled = false;
        wallJumped = true;
        isDashing = true;

        yield return new WaitForSeconds(.3f);

        dashParticle.Stop();
        rb.gravityScale = 3;
        GetComponent<BetterJumping>().enabled = true;
        wallJumped = false;
        isDashing = false;
    }

    IEnumerator GroundDash()
    {
        yield return new WaitForSeconds(.15f);
        if (coll.onGround)
            hasDashed = false;
    }

    private void WallJump()
    {
        if ((side == 1 && coll.onRightWall) || side == -1 && !coll.onRightWall)
        {
            side *= -1;
            anim.Flip(side);
        }

        StopCoroutine(DisableMovement(0));
        StartCoroutine(DisableMovement(.1f));

        Vector2 wallDir = coll.onRightWall ? Vector2.left : Vector2.right;

        Jump((Vector2.up / 1.5f + wallDir / 1.5f), true);

        wallJumped = true;
    }

    private void WallSlide()
    {
        if(coll.wallSide != side)
         anim.Flip(side * -1);

        if (!canMove)
            return;

        bool pushingWall = false;
        if((rb.velocity.x > 0 && coll.onRightWall) || (rb.velocity.x < 0 && coll.onLeftWall))
        {
            pushingWall = true;
        }
        float push = pushingWall ? 0 : rb.velocity.x;

        rb.velocity = new Vector2(push, -slideSpeed);
    }

    private void Walk(Vector2 dir)
    {
        if (!canMove)
            return;

        if (wallGrab)
            return;

        if (!wallJumped)
        {
            rb.velocity = new Vector2(dir.x * speed, rb.velocity.y);
        }
        else
        {
            rb.velocity = Vector2.Lerp(rb.velocity, (new Vector2(dir.x * speed, rb.velocity.y)), wallJumpLerp * Time.deltaTime);
        }
    }

    private void Jump(Vector2 dir, bool wall)
    {
        slideParticle.transform.parent.localScale = new Vector3(ParticleSide(), 1, 1);
        ParticleSystem particle = wall ? wallJumpParticle : jumpParticle;

        rb.velocity = new Vector2(rb.velocity.x, 0);
        rb.velocity += dir * jumpForce;

        particle.Play();
    }

    IEnumerator DisableMovement(float time)
    {
        canMove = false;
        yield return new WaitForSeconds(time);
        canMove = true;
    }

    void RigidbodyDrag(float x)
    {
        rb.drag = x;
    }

    void WallParticle(float vertical)
    {
        var main = slideParticle.main;

        if (wallSlide || (wallGrab && vertical < 0))
        {
            slideParticle.transform.parent.localScale = new Vector3(ParticleSide(), 1, 1);
            main.startColor = Color.white;
        }
        else
        {
            main.startColor = Color.clear;
        }
    }

    int ParticleSide()
    {
        int particleSide = coll.onRightWall ? 1 : -1;
        return particleSide;
    }
}


// using System;
// using Random = UnityEngine.Random;
// using UnityEngine;

// public class movement : MonoBehaviour
// {
//     [SerializeField] private float _speed;
//     ///[SerializeField] private SpriteRenderer _characterSprite;
//     [SerializeField] private float _jumpForce;
//     [SerializeField] Vector3 _groundcheckOffset;
//     [SerializeField] private Rigidbody2D _rb;        //Rigidbody initialization
    
//     private bool _isFlying;
//     private bool _isMoving;


//     private void Update()
//     {
//         GatherInputs();

//         HandleGrounding();

//         HandleMoving();

//         HandleJump();

//         Move();

//         CheckGround();
//         if (Input.GetKeyDown(KeyCode.Space))
//         {
//             if (_isGrounded)
//             {
//                 Jump();
//             }
//         }
//     }

//     #region Inputs 
//     /// <summary>
//     /// Initialize x and y
//     /// </summary>
//     private bool _facingLeft;

//     private void GatherInputs()
//     {
//         Inputs.RawX = (int)Input.GetAxisRaw("Horizontal");
//         Inputs.RawY = (int)Input.GetAxisRaw("Vertical");
//         Inputs.X = Input.GetAxis("Horizontal");
//         Inputs.Y = Input.GetAxis("Vertical");

//         _facingLeft = Inputs.RawX != 1 && (Inputs.RawX == -1 || _facingLeft);

//     }

//     /// _input = new Vector2(Input.GetAxis("Horizontal"), 0);
//     #endregion

//     #region Perception
//     [Header("Perception")] [SerializeField] LayerMask _groundMask;
//     [SerializeField] private float _grounderOffset = -1, _grounderRadius = 0.1;

//     public bool IsGrounded;
//     public static event Action OnTouchedGround;
//     private readonly Collider[] _ground = new Collider[1];

//     private void HandleGrounding()
//     {
//         var grounded = Physics.OverlapSphereNonAlloc(transfor.position + Vector3(0, _grounderOffset), _grounderRadius, _ground, _groundMask) > 0;

//         if (!IsGrounded && grounded)
//         {
//             IsGrounded = true;
//             _currentMovementLerpSpeed = 100;
//             transform.SetParent(_ground[0].transform);
//         }
//         else if (IsGrounded && !grounded) 
//         {
//             IsGrounded = false;
//             _timeLeftGrounded = Time.time;
//             transform.SetParent(null);
//         }
//         //Wall Detection
//         _isAgainstLeftWall = Physics.OverlapSphereNonAlloc(transform.position + new Vector3(-_wallCheckOffset, 0), _wallCheckRadius, _leftWall, _groundMask) > 0;
//         _isAgainstRightWall = Physics.OverlapSphereNonAlloc(transform.position + new Vector3(_wallCheckOffset, 0), _wallCheckRadius, _rightWall, _groundMask) > 0;
//         _pushingLeftWall = _isAgainstLeftWall && Inputs.X < 0;
//         _pushingRightWall = _isAgainstRightWall && Inputs.X > 0;
//     }
    

//     //private void CheckGround()
//     //{

//     //}




//     //float rayLength = 0.8f;
//     //Vector3 rayStartPosition = transform.position + _groundcheckOffset;
//     //RaycastHit2D hit = Physics2D.Raycast(rayStartPosition, rayStartPosition + Vector3.down, rayLength);

//     //if (hit.collider != null)
//     //{
//     //    _isGrounded = hit.collider.CompareTag("Ground") ? true : false;
//     //}
//     //else
//     //{
//     //    _isGrounded = false;
//     //}

//     #endregion

//     #region Flying
//     private bool isFlying()
//     {
//         if (rb.velocity.y < 0)
//         {
//             return true;
//         }
//         else
//         {
//             return false;
//         }
//     }
//     #endregion

//     #region Move

//     [Header("Move")] [SerializeField] private float _moveSpeed = 4;
//     [SerializeField] private float _accelaration = 2;
//     [SerializeField] private float _currentMovementLerpSpeed = 100;

//     private void HandleMoving()
//     {
//         // Slowly release control after wall jump
//         _currentMovementLerpSpeed = Mathf.MoveTowards(_currentMovementLerpSpeed, 100, _wallJumpMovementLerp * Time.deltaTime);

//         // This can be done using just X & Y input as they lerp to max values, but this gives greater control over velocity acceleration
//         var accelaration = IsGrounded ? _accelaration : _accelaration * 0.5f;

//         if (Input.GetKey(KeyCode.LeftArrow))
//         {
//             if (rb.velocity.x > 0) Inputs.X = 0;   // Imidiate stop and turn
//             Inputs.X = Mathf.MoveTowards(Inputs.X, -1, accelaration * Time.deltaTime);
//         }
//         else if (Input.GetKey(KeyCode.RightArrow)) {
//             if (rb.velocity.x < 0) Inputs.X = 0;
//             Inputs.X = Mathf.MoveTowards(Inputs.X, 1, accelaration * Time.deltaTime);
//         }
//         else
//         {
//             Inputs.X = Mathf.MoveTowards(Inputs.X, 0, accelaration * Time.deltaTime);
//         }

//         // _currentMovementLerpSpeed should be set to something crazy high to be effectively instant. But slowed down after a wall jump and slowly released
//         var idealVel = new Vector3(Inputs.X * _moveSpeed, rb.velocity.y);
//     }




//     //private void Move()

//     //{
//     //    _input = new Vector2(Input.GetAxis("Horizontal"), 0);
//     //    transform.position += _input * _speed * Time.deltaTime;

//     //    /// if (_input.x != 0)
//     //    ///{
//     //    ///_characterSprite.flipX = _input.x > 0 ? false : true;
//     //    ///}
//     //}

//     #endregion

//     #region Jump
//     [Header("Jump")] [SerializeField] private float _jumpforce = 15;
//     [SerializeField] private float _fallMultiplier = 7;
//     [SerializeField] private float _jumpVelocityFalloff = 8;
//     [SerializeField] private float _wallJumpLock = 0.25f;
//     [SerializeField] private float _wallJumpMovementLerp = 5;
//     [SerializeField] private float _coyoteTime = 0.2f;
//     [SerializeField] private bool _enableDoubleJump = true;
//     private float _timeLeftGrounded = -10;
//     private float _timeLastWallJumped;
//     private bool _hasJumped;
//     private bool _hasDoubleJumped;

//     private void HandleJump()
//     {
//         if (Input.GetKeyDown(KeyCode.Space))
//         {
//             if (!IsGrounded && (_isAgainstLeftWall || _isAgainstRightWall))
//             {
//                 _timeLastWallJumped = Time.time;
//                 _currentMovementLerpSpeed = _wallJumpMovementLerp;
//                 // ExecuteJump(new Vector2(_isAgainstLeftWall ? _jumpForce : -_jumpForce, _jumpForce)); // Wall Jump
//             }
//             else if (IsGrounded || Time.time < _timeLeftGrounded + _coyoteTime || _enableDoubleJump && !_hasDoubleJumped)
//             {
//                 if (!_hasJumped || _hasJumped && !_hasDoubleJumped) ExecuteJump(new Vector2(rb.velocity.x, _jumpForce), _hasJumped);
//             }
//         }
//         void ExecuteJump(Vector3 dir, bool doubleJump = false)
//         {
//             _rb.velocity = dir;
//             _jumpLaunchPoof.up = rb.velocity;
//             _jumpParticles.Play();
//             _hasDoubleJumped = doubleJump;
//             _hasJumped = true;
//         }
//         // Fall faster and allow small jumps. _jumpVelocityFalloff is the point at which we start adding extra gravity. Using 0 causes floating
//         if (_rb.velocity.y < _jumpVelocityFalloff || _rb.velocity.y > 0 && !Input.GetKey(KeyCode.Space))
//             _rb.velocity += _fallMultiplier * Physics.gravity.y * Vector3.up * Time.deltaTime;
//     }



//     //private void Jump()
//     //{
//     //    Debug.Log("Jump");
//     //    rb.AddForce(transform.up * _jumpForce, ForceMode2D.Impulse);
//     //}
//     #endregion
// }

