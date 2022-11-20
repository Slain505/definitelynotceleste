using System;
using Random = UnityEngine.Random;
using UnityEngine;

public class movement : MonoBehaviour
{
    [SerializeField] private float _speed;
    ///[SerializeField] private SpriteRenderer _characterSprite;
    [SerializeField] private float _jumpForce;
    [SerializeField] Vector3 _groundcheckOffset;
    [SerializeField] private Rigidbody2D rb;        //Rigidbody initialization
    private FrameInputs _inputs;
    private Vector3 _input;
    private bool _isFlying;
    private bool _isMoving;


    private void Update()
    {
        GatherInputs();

        HandleGrounding();

        HandleMoving();

        HandleJump();

        Move();

        CheckGround();
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (_isGrounded)
            {
                Jump();
            }
        }
    }

    #region Inputs 
    /// <summary>
    /// Initialize x and y
    /// </summary>
    private bool _facingLeft;

    private void GatherInputs()
    {
        _inputs.RawX = (int)Input.GetAxisRaw("Horizontal");
        _inputs.RawY = (int)Input.GetAxisRaw("Vertical");
        _inputs.X = Input.GetAxis("Horizontal");
        _inputs.Y = Input.GetAxis("Vertical");

        _facingLeft = _inputs.RawX != 1 && (_inputs.RawX == -1 || _facingLeft);

    }

    /// _input = new Vector2(Input.GetAxis("Horizontal"), 0);
    #endregion

    #region Perception
    [Header("Perception")] [SerializeField] LayerMask _groundMask;
    [SerializeField] private float _grounderOffset = -1, _grounderRadius = 0.1;

    public bool IsGrounded;
    public static event Action OnTouchedGround;
    private readonly Collider[] _ground = new Collider[1];

    private void HandleGrounding()
    {
        var grounded = Physics.OverlapSphereNonAlloc(transfor.position + Vector3(0, _grounderOffset), _grounderRadius, _ground, _groundMask) > 0;

        if (!IsGrounded && !grounded)
        {
            IsGrounded = true;
            _currentMovementLerpSpeed = 100;
            transform.SetParent(_ground[0].transform);
        }
        else if {
            IsGrounded = false;
            _timeLeftGrounded = Time.time;
            transform.SetParent(null);
        }
        //Wall Detection
        _isAgainstLeftWall = Physics.OverlapSphereNonAlloc(transform.position + new Vector3(-_wallCheckOffset, 0), _wallCheckRadius, _leftWall, _groundMask) > 0;
        _isAgainstRightWall = Physics.OverlapSphereNonAlloc(transform.position + new Vector3(_wallCheckOffset, 0), _wallCheckRadius, _rightWall, _groundMask) > 0;
        _pushingLeftWall = _isAgainstLeftWall && _inputs.X < 0;
        _pushingRightWall = _isAgainstRightWall && _inputs.X > 0;
    }
    

    //private void CheckGround()
    //{

    //}




    //float rayLength = 0.8f;
    //Vector3 rayStartPosition = transform.position + _groundcheckOffset;
    //RaycastHit2D hit = Physics2D.Raycast(rayStartPosition, rayStartPosition + Vector3.down, rayLength);

    //if (hit.collider != null)
    //{
    //    _isGrounded = hit.collider.CompareTag("Ground") ? true : false;
    //}
    //else
    //{
    //    _isGrounded = false;
    //}

    #endregion

    #region Flying
    private bool isFlying()
    {
        if (rb.velocity.y < 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    #endregion

    #region Move

    [Header("Move")] [SerializeField] private float _moveSpeed = 4;
    [SerializeField] private float _accelaration = 2;
    [SerializeField] private float _currentMovementLerpSpeed = 100;

    private void HandleMoving()
    {
        // Slowly release control after wall jump
        _currentMovementLerpSpeed = Mathf.MoveTowards(_currentMovementLerpSpeed, 100, _wallJumpMovementLerp * Time.deltaTime);

        // This can be done using just X & Y input as they lerp to max values, but this gives greater control over velocity acceleration
        var accelaration = IsGrounded ? _accelaration : _accelaration * 0,5f;

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            if (rb.velocity.x > 0) _inputs.X = 0;   // Imidiate stop and turn
            _inputs.X = Mathf.MoveTowards(_inputs.X, -1, accelaration * Time.deltaTime);
        }
        else if (Input.GetKey(KeyCode.RightArrow)) {
            if (rb.velocity.x < 0) _inputs.X = 0;
            _inputs.X = Mathf.MoveTowards(_inputs.X, 1, accelaration * Time.deltaTime);
        }
        else
        {
            _inputs.X = Mathf.MoveTowards(_inputs.X, 0, accelaration * Time.deltaTime);
        }

        // _currentMovementLerpSpeed should be set to something crazy high to be effectively instant. But slowed down after a wall jump and slowly released
        var idealVel = new Vector3(_inputs.X * _moveSpeed, rb.velocity.y);
    }




    //private void Move()

    //{
    //    _input = new Vector2(Input.GetAxis("Horizontal"), 0);
    //    transform.position += _input * _speed * Time.deltaTime;

    //    /// if (_input.x != 0)
    //    ///{
    //    ///_characterSprite.flipX = _input.x > 0 ? false : true;
    //    ///}
    //}

    #endregion

    #region Jump
    [Header("Jump")] [SerializeField] private float _jumpforce = 15;
    [SerializeField] private float _fallMultiplier = 7;
    [SerializeField] private float _jumpVelocityFalloff = 8;
    [SerializeField] private float _wallJumpLock = 0.25f;
    [SerializeField] private float _wallJumpMovementLerp = 5;
    [SerializeField] private float _coyoteTime = 0.2f;
    [SerializeField] private bool _enableDoubleJump = true;
    private float _timeLeftGrounded = -10;
    private float _timeLastWallJumped;
    private bool _hasJumped;
    private bool _hasDoubleJumped;

    private void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (!IsGrounded && (_isAgainstLeftWall || _isAgainstRightWall))
            {
                _timeLastWallJumped = Time.time;
                _currentMovementLerpSpeed = _wallJumpMovementLerp;
                // ExecuteJump(new Vector2(_isAgainstLeftWall ? _jumpForce : -_jumpForce, _jumpForce)); // Wall Jump
            }
            else if (IsGrounded || Time.time < _timeLeftGrounded + _coyoteTime || _enableDoubleJump && !_hasDoubleJumped)
            {
                if (!_hasJumped || _hasJumped && !_hasDoubleJumped) ExecuteJump(new Vector2(rb.velocity.x, _jumpForce), _hasJumped);
            }
        }
        void ExecuteJump(Vector3 dir, bool doubleJump = false)
        {
            _rb.velocity = dir;
            _jumpLaunchPoof.up = rb.velocity;
            _jumpParticles.Play();
            _hasDoubleJumped = doubleJump;
            _hasJumped = true;
        }
        // Fall faster and allow small jumps. _jumpVelocityFalloff is the point at which we start adding extra gravity. Using 0 causes floating
        if (_rb.velocity.y < _jumpVelocityFalloff || _rb.velocity.y > 0 && !Input.GetKey(KeyCode.Space))
            _rb.velocity += _fallMultiplier * Physics.gravity.y * Vector3.up * Time.deltaTime;
    }



    //private void Jump()
    //{
    //    Debug.Log("Jump");
    //    rb.AddForce(transform.up * _jumpForce, ForceMode2D.Impulse);
    //}
    #endregion
}

