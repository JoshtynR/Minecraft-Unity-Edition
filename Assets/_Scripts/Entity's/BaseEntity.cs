using System;
using System.Collections;
using Mirror;
using Popcron;
using UnityEngine;
using Gizmos = Popcron.Gizmos;

[ExecuteAlways]
public abstract partial class BaseEntity : NetworkBehaviour
{
    public float crouchSpeed = 1.31f;
    public float walkSpeed = 4.317f;
    public float sprintSpeed = 5.612f;
    public float flySpeed = 10.92f;
    public float flySprintSpeed = 21.6f;
    public float jumpForce = 10.15f;
    public float gravity = -32f;
    
    [Space]
    
    public float entityWidth = 0.32f;
    public float entityHeight = 1.8f;
    public float eyeHeight = 1.578f;

    [Space] 
    
    public bool isGrounded;
    public bool isSprinting;
    [SyncVar] public bool isCrouching;
    public bool isFlying;
    public bool drawBounds;
    
    [Space]
    
    protected World world;
    
    protected float horizontal;
    protected float vertical;
    
    protected float mouseX;
    protected float mouseY;
    
    protected Vector3 velocity;
    protected Vector3 extraForce;
    protected float verticalMomentum;
    protected bool jumpRequest;
    protected Transform direction;


    public virtual void Start()
    {
        world = World.Instance;
        direction = transform;

        var rayColl = GetComponentInChildren<BoxCollider>();
        if (rayColl != null)
        {
            rayColl.center = new Vector3(0, entityHeight / 2, 0);
            rayColl.size = new Vector3(entityWidth*2, entityHeight, entityWidth*2);
        }
    }

    protected void RequestJump(bool pressed)
    {
        if (pressed) jumpRequest = isGrounded && !isFlying;
    }

    public virtual void FixedUpdate()
    {
        RefreshFluidContact();
        // A press belongs to one physics tick. Never retain an airborne request
        // until a timer expires or the player lands.
        bool requested = jumpRequest;
        jumpRequest = false;
        if (requested && isGrounded && !isFlying && !InWater && !InLava) Jump();
        CalculateVelocity();

        if (Mathf.Abs(velocity.x) <= 0 && Mathf.Abs(velocity.z) <= 0)
        {
            isSprinting = false;
        }

        Move();
        MoveForces();
    }

    public virtual void Move()
    {
        transform.Translate(velocity, Space.World);
    }

    public virtual void MoveForces()
    {
        CheckAroundCollision(ref extraForce);
        
        if (extraForce.magnitude > 0.05)
        {
            transform.Translate(extraForce*Time.fixedDeltaTime, Space.World);
        }
        extraForce = Vector3.Lerp(extraForce, Vector3.zero, 5*Time.fixedDeltaTime);
    }

    public virtual void AddForce(Vector3 dir, float force)
    {
        dir.Normalize();
        if (dir.y < 0) dir.y = -dir.y; // reflect down force on the ground
        extraForce += dir * force;
    }
    
    [Command(requiresAuthority = false)]
    public void CmdAddForce(Vector3 dir, float force)
    {
        RpcAddForce(netIdentity.connectionToClient, dir,force);
    }
    
    [TargetRpc]
    public void RpcAddForce(NetworkConnection netConn, Vector3 dir, float force)
    {
        AddForce(dir,force);
    }

    public virtual void Update()
    {
        if (drawBounds)
        {
            Gizmos.Draw<CubeDrawer>(Color.white, false, transform.position + (Vector3.up*entityHeight/2),Quaternion.identity, new Vector3(entityWidth * 2, entityHeight, entityWidth * 2));
            Gizmos.Draw<CubeDrawer>(Color.red, false, transform.position + (Vector3.up*eyeHeight),Quaternion.identity, new Vector3(entityWidth * 2, 0.01f, entityWidth * 2));
        }
    }
    
    private void Jump()
    {
        verticalMomentum = jumpForce;
        isGrounded = false;
        jumpRequest = false;
        OnJumpPerformed();
    }

    protected virtual void OnJumpPerformed() { }
    
    public virtual void CalculateVelocity()
    {
        if(CalculateFluidVelocity())return;
        // Apply gravity
        if (!isGrounded && !isFlying)
        {
            verticalMomentum += gravity * 0.98f * Time.deltaTime;
            // verticalMomentum *= 0.98f;
        }
        else if(verticalMomentum < 0 && !isFlying)
        {
            verticalMomentum = 0;
        }

        // Calculate movement velocity
        if (isCrouching && !isFlying)
        {
            velocity = ((direction.forward * vertical) + (direction.right * horizontal)) * (Time.fixedDeltaTime * crouchSpeed);
        }
        else if (isSprinting)
        {
            velocity = ((direction.forward * vertical) + (direction.right * horizontal)) * (Time.fixedDeltaTime * (isFlying ? flySprintSpeed : sprintSpeed));
        }
        else
        { 
            velocity = ((direction.forward * vertical) + (direction.right * horizontal)) * (Time.fixedDeltaTime * (isFlying ? flySpeed : walkSpeed));
        }
        
        // Apply vertical momentum (falling & jumping)
        velocity += Vector3.up * verticalMomentum * Time.fixedDeltaTime;


        if (isCrouching && isGrounded && !isFlying)
        {
            if (!BlockCollision.Supported(world, transform.position + new Vector3(velocity.x,0,0), entityWidth, entityHeight)) velocity.x = 0;
            if (!BlockCollision.Supported(world, transform.position + new Vector3(velocity.x,0,velocity.z), entityWidth, entityHeight)) velocity.z = 0;
        }
        CheckAroundCollision(ref velocity);
    }

    public void CheckAroundCollision(ref Vector3 displacement)
    {
        var originalY = displacement.y;
        displacement = BlockCollision.Resolve(world, transform.position, entityWidth, entityHeight, displacement, isGrounded && !isFlying);
        if (originalY != displacement.y) verticalMomentum = 0;
        isGrounded = BlockCollision.Supported(world, transform.position + displacement, entityWidth, entityHeight);
    }
}
