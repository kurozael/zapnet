using System;
using System.Collections;
using UnityEngine;
using zapnet;

public struct CharacterMotorState
{
    public bool holdingJumpButton;
    public float lastButtonDownTime;
    public float lastStartTime;
    public Vector3 jumpDir;

    public CollisionFlags collisionFlags;
    public Vector3 lastHitPoint;
    public Vector3 hitPoint;

    public Vector3 velocity;
    public bool isGrounded;
    public bool isJumping;

    public void Reset()
    {
        lastButtonDownTime = -100;
        collisionFlags = CollisionFlags.None;
        lastHitPoint = new Vector3(Mathf.Infinity, 0, 0);
        velocity = Vector3.zero;
        isGrounded = true;
        isJumping = false;
        hitPoint = Vector3.zero;
        jumpDir = Vector3.up;
    }
}

[Serializable]
public class CharacterMotorMovement
{
    public float maxForwardSpeed;
    public float maxSidewaysSpeed;
    public float maxBackwardsSpeed;

    [Tooltip("Curve for multiplying speed based on slope (negative = downwards).")]
    public AnimationCurve slopeSpeedMultiplier;

    [Tooltip("How fast does the character change speed?  Higher is faster.")]
    public float maxGroundAcceleration;
    public float maxAirAcceleration;

    public float gravity;
    public float maxFallSpeed;

    public CharacterMotorMovement()
    {
        maxForwardSpeed = 10f;
        maxSidewaysSpeed = 10f;
        maxBackwardsSpeed = 10f;
        slopeSpeedMultiplier = new AnimationCurve(new Keyframe[] { new Keyframe(-90, 1), new Keyframe(0, 1), new Keyframe(90, 0) });
        maxGroundAcceleration = 30f;
        maxAirAcceleration = 20f;
        maxFallSpeed = 20f;
        gravity = 10f;
    }
}

// We will contain all the jumping related variables in one helper class for clarity.
[Serializable]
public class CharacterMotorJumping
{
    [Tooltip("Can the character jump?")]
    public bool enabled = true;

    [Tooltip("How high do we jump when pressing jump and letting go immediately.")]
    public float baseHeight = 1f;

    [Tooltip("We add extraHeight units (meters) on top when holding the button down longer while jumping.")]
    public float extraHeight = 2f;

    [Tooltip("How much does the character jump out perpendicular to walkable surfaces. 0 = fully vertical, 1 = fully perpendicular.")]
    [Range(0, 1)]
    public float perpAmount = 0f;

    [Tooltip("How much does the character jump out perpendicular to steep surfaces. 0 = fully vertical, 1 = fully perpendicular.")]
    [Range(0, 1)]
    public float steepPerpAmount = 0.5f;
}

[RequireComponent(typeof(CharacterController))]
public class CharacterMotor : MonoBehaviour
{
    public CharacterMotorMovement movement;
    public CharacterMotorJumping jumping;
    public CharacterMotorState state;

    private CharacterController _controller;
    private Vector3 _lastGroundNormal;
    private Vector3 _groundNormal;

    public CharacterController Controller
    {
        get
        {
            return _controller;
        }
    }

    public CharacterMotorState Simulate(Vector3 moveDirection, bool shouldJump)
    {
        var deltaTime = Zapnet.Network.FixedDeltaTime;
        var velocity = state.velocity;

        velocity = ApplyInputVelocityChange(velocity, moveDirection);
        velocity = ApplyGravityAndJumping(velocity, shouldJump);

        var moveDistance = Vector3.zero;
        var currentMovementOffset = velocity * deltaTime;
        var lastPosition = transform.position;
        var pushDownOffset = Mathf.Max(_controller.stepOffset, new Vector3(currentMovementOffset.x, 0, currentMovementOffset.z).magnitude);

        if (state.isGrounded)
        {
            currentMovementOffset = currentMovementOffset - (pushDownOffset * Vector3.up);
        }

        if (!Physics.autoSyncTransforms)
        {
            Physics.SyncTransforms();
        }

        _groundNormal = Vector3.zero;
        _controller.Move(Vector3.zero);

        state.collisionFlags = _controller.Move(currentMovementOffset);
        state.lastHitPoint = state.hitPoint;
        _lastGroundNormal = _groundNormal;

        var oldHorizontalVelocity = new Vector3(velocity.x, 0, velocity.z);

        state.velocity = (transform.position - lastPosition) / deltaTime;

        var newHorizontalVelocity = new Vector3(state.velocity.x, 0, state.velocity.z);

        if (oldHorizontalVelocity == Vector3.zero)
        {
            state.velocity = new Vector3(0, state.velocity.y, 0);
        }
        else
        {
            float projectedNewVelocity = Vector3.Dot(newHorizontalVelocity, oldHorizontalVelocity) / oldHorizontalVelocity.sqrMagnitude;
            state.velocity = (oldHorizontalVelocity * Mathf.Clamp01(projectedNewVelocity)) + (state.velocity.y * Vector3.up);
        }

        if (state.velocity.y < (velocity.y - 0.001f))
        {
            if (state.velocity.y < 0)
            {
                state.velocity.y = velocity.y;
            }
            else
            {
                state.holdingJumpButton = false;
            }
        }

        if (state.isGrounded && !IsGroundedTest())
        {
            state.isGrounded = false;
            transform.position = transform.position + (pushDownOffset * Vector3.up);
        }
        else
        {
            if (!state.isGrounded && IsGroundedTest())
            {
                state.isGrounded = true;
                state.isJumping = false;
            }
        }

        return state;
    }

    private void Awake()
    {
        _lastGroundNormal = Vector3.zero;
        _groundNormal = Vector3.zero;
        _controller = GetComponent<CharacterController>();

        state = new CharacterMotorState();
    }

    private Vector3 ApplyInputVelocityChange(Vector3 velocity, Vector3 moveDirection)
    {
        var desiredVelocity = new Vector3();
        var deltaTime = Zapnet.Network.FixedDeltaTime;

        desiredVelocity = GetDesiredHorizontalVelocity(moveDirection);

        if (state.isGrounded)
        {
            desiredVelocity = AdjustGroundVelocityToNormal(desiredVelocity, _groundNormal);
        }
        else
        {
            velocity.y = 0;
        }

        var maxVelocityChange = GetMaxAcceleration(state.isGrounded) * deltaTime;
        var velocityChangeVector = desiredVelocity - velocity;

        if (velocityChangeVector.sqrMagnitude > (maxVelocityChange * maxVelocityChange))
        {
            velocityChangeVector = velocityChangeVector.normalized * maxVelocityChange;
        }

        if (state.isGrounded)
        {
            velocity += velocityChangeVector;
            velocity.y = Mathf.Min(velocity.y, 0);
        }

        return velocity;
    }

    private Vector3 ApplyGravityAndJumping(Vector3 velocity, bool shouldJump)
    {
        var deltaTime = Zapnet.Network.FixedDeltaTime;

        if (!shouldJump)
        {
            state.holdingJumpButton = false;
            state.lastButtonDownTime = -100;
        }

        if ((shouldJump && (state.lastButtonDownTime < 0)))
        {
            state.lastButtonDownTime = Time.time;
        }

        if (state.isGrounded)
        {
            velocity.y = Mathf.Min(0, velocity.y) - (movement.gravity * deltaTime);
        }
        else
        {
            velocity.y = state.velocity.y - (movement.gravity * deltaTime);

            if (state.isJumping && state.holdingJumpButton)
            {
                if (Time.time < (state.lastStartTime + (jumping.extraHeight / CalculateJumpVerticalSpeed(jumping.baseHeight))))
                {
                    velocity += ((state.jumpDir * movement.gravity) * deltaTime);
                }
            }

            velocity.y = Mathf.Max(velocity.y, -movement.maxFallSpeed);
        }

        if (state.isGrounded)
        {
            if (jumping.enabled && ((Time.time - state.lastButtonDownTime) < 0.2f))
            {
                state.isGrounded = false;
                state.isJumping = true;

                state.lastStartTime = Time.time;
                state.lastButtonDownTime = -100;
                state.holdingJumpButton = true;
                state.jumpDir = Vector3.Slerp(Vector3.up, _groundNormal, jumping.perpAmount);

                velocity.y = 0;
                velocity = velocity + (state.jumpDir * CalculateJumpVerticalSpeed(jumping.baseHeight));
            }
            else
            {
                state.holdingJumpButton = false;
            }
        }

        return velocity;
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (((hit.normal.y > 0) && (hit.normal.y > _groundNormal.y)) && (hit.moveDirection.y < 0))
        {
            if (((hit.point - state.lastHitPoint).sqrMagnitude > 0.001f) || (_lastGroundNormal == Vector3.zero))
            {
                _groundNormal = hit.normal;
            }
            else
            {
                _groundNormal = _lastGroundNormal;
            }

            state.hitPoint = hit.point;
        }
    }

    private Vector3 GetDesiredHorizontalVelocity(Vector3 moveDirection)
    {
        var desiredLocalDirection = transform.InverseTransformDirection(moveDirection);
        var maxSpeed = MaxSpeedInDirection(desiredLocalDirection);

        if (state.isGrounded)
        {
            float movementSlopeAngle = Mathf.Asin(state.velocity.normalized.y) * Mathf.Rad2Deg;
            maxSpeed = maxSpeed * movement.slopeSpeedMultiplier.Evaluate(movementSlopeAngle);
        }

        return transform.TransformDirection(desiredLocalDirection * maxSpeed);
    }

    private Vector3 AdjustGroundVelocityToNormal(Vector3 horizontalVelocity, Vector3 groundNormal)
    {
        var sideways = Vector3.Cross(Vector3.up, horizontalVelocity);
        return Vector3.Cross(sideways, groundNormal).normalized * horizontalVelocity.magnitude;
    }

    private bool IsGroundedTest()
    {
        return _groundNormal.y > 0.01f;
    }

    public float GetMaxAcceleration(bool grounded)
    {
        if (grounded)
        {
            return movement.maxGroundAcceleration;
        }
        else
        {
            return movement.maxAirAcceleration;
        }
    }

    public float CalculateJumpVerticalSpeed(float targetJumpHeight)
    {
        return Mathf.Sqrt((2 * targetJumpHeight) * movement.gravity);
    }

    public bool IsJumping()
    {
        return state.isJumping;
    }

    public bool IsTouchingCeiling()
    {
        return (state.collisionFlags & CollisionFlags.CollidedAbove) != 0;
    }

    public bool IsGrounded()
    {
        return state.isGrounded;
    }

    public float MaxSpeedInDirection(Vector3 desiredMovementDirection)
    {
        if (desiredMovementDirection == Vector3.zero)
        {
            return 0;
        }
        else
        {
            var zAxisEllipseMultiplier = (desiredMovementDirection.z > 0 ? movement.maxForwardSpeed : movement.maxBackwardsSpeed) / movement.maxSidewaysSpeed;
            var temp = new Vector3(desiredMovementDirection.x, 0, desiredMovementDirection.z / zAxisEllipseMultiplier).normalized;
            var length = new Vector3(temp.x, 0, temp.z * zAxisEllipseMultiplier).magnitude * movement.maxSidewaysSpeed;

            return length;
        }
    }
}
