using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    public Camera playerCam;

    [Header("General")]
    [SerializeField] public float gravityDownForce = 20f;
    [SerializeField] LayerMask groundCheckLayers = -1;
    [SerializeField] float groundCheckDistance = 0.05f;

    [Header("Movement")]
    public float maxSpeedOnGround = 10f;
    [SerializeField] float movementSharpnessOnGround = 10;
    [SerializeField] float maxSpeedInAir = 10f;
    [SerializeField] float accelerationSpeedInAir = 25f;
    public float sprintSpeedModifier = 2f;
    [SerializeField] float maxSpeedCrouchedRatio = 0.5f;

    [Header("Rotation")]
    [SerializeField] float camRotSpeed = 200f;
    [SerializeField] float camRotMultipler = 1.0f;

    [Header("Jump")]
    [SerializeField] float jumpForce = 9f;

    [Header("Stance")]
    [SerializeField] float crouchingSharpness = 10f;
    [SerializeField] float cameraHeightRatio = 0.9f;
    [SerializeField] float capsuleHeightCrouching = 0.9f;
    [SerializeField] float capsuleHeightStanding = 1.8f;

    public UnityAction<bool> onStanceChanged;
    public UnityAction onSprint;

    public Vector3 characterVelocity { get; set; }
    public bool isGrounded { get; private set; }
    public bool hasJumpedThisFrame { get; private set; }
    public bool isCrouching { get; private set; }
    public bool isSprinting { get; private set; }


    PlayerInputHandler inputHandler;
    CharacterController charController;
    public PlayerWeaponManager weaponManager;

    Vector3 groundNormal;
    Vector3 latestImpactSpeed;
    float camVerticalAngle = 0f;
    float lastTimeJumped = 0f;
    float targetCharacterHeight;

    const float JUMP_GROUNDING_PREVENT_TIME = 0.2f;
    const float GROUND_CHECK_DISTANCE_IN_AIR = 0.07f;

    // Start is called before the first frame update
    void Start()
    {
        weaponManager ??= GetComponent<PlayerWeaponManager>();
        inputHandler = GetComponent<PlayerInputHandler>();
        charController = GetComponent<CharacterController>();
        playerCam = GetComponentInChildren<Camera>();

        SetCrouchingState(false, true);
        UpdateCharacterHeight(true);
    }

    // Update is called once per frame
    void Update()
    {
        HandleLook();

        // crouching
        if (inputHandler.GetCrouchInputDown())
        {
            SetCrouchingState(!isCrouching, false);
        }
 
        UpdateCharacterHeight(false);
        HandleMovement();
        GroundCheck();
    }

    void HandleLook()
    {
        // Horizontal Char Rotation:
        float rotDegrees = inputHandler.GetLookInputsHorizontal() * camRotSpeed * camRotMultipler;
        transform.Rotate(new Vector3(0f, rotDegrees, 0f), Space.Self);

        // Vertical Char Rotation:
        camVerticalAngle += inputHandler.GetLookInputsVertical() * camRotSpeed * camRotMultipler;

        // limit the camera's vertical angle to min/max
        camVerticalAngle = Mathf.Clamp(camVerticalAngle, -89f, 89f);

        // apply the vertical angle as a local rotation to the camera transform along its right axis (makes it pivot up and down)
        playerCam.transform.localEulerAngles = new Vector3(camVerticalAngle, 0, 0);
    }

    void HandleMovement()
    {
        hasJumpedThisFrame = false;

        bool tryingToSprint = inputHandler.GetSprintInputHeld();

        isSprinting = tryingToSprint && characterVelocity.magnitude > maxSpeedOnGround * .9f
            && inputHandler.GetMoveInput().z > 0;

        if (isSprinting)
        {
            onSprint?.Invoke();
            isSprinting = SetCrouchingState(false, false);
            weaponManager.SetWeaponSprintAnimation(true);
            // play gun sprint animation
        }
        else
            weaponManager.SetWeaponSprintAnimation(false);

        float speedModifier = isSprinting ? sprintSpeedModifier : 1f;

        // converts move input to a worldspace vector based on our character's transform orientation
        Vector3 worldspaceMoveInput = transform.TransformVector(inputHandler.GetMoveInput());

        if (isGrounded) // handle grounded movement
        {
            // calculate the desired velocity from inputs, max speed, and current slope
            Vector3 targetVelocity = worldspaceMoveInput * maxSpeedOnGround * speedModifier;

            // reduce speed if crouching by crouch speed ratio
            if (isCrouching)
                targetVelocity *= maxSpeedCrouchedRatio;

            targetVelocity = GetDirectionReorientedOnSlope(targetVelocity.normalized, groundNormal) * targetVelocity.magnitude;

            // smoothly interpolate between our current velocity and the target velocity based on acceleration speed
            characterVelocity = Vector3.Lerp(characterVelocity, targetVelocity, movementSharpnessOnGround * Time.deltaTime);

            if (isGrounded && inputHandler.GetJumpInputDown())  // Jumping
            {
                // force the crouch state to false
                if (SetCrouchingState(false, false))
                {
                    // start by canceling out the vertical component of our velocity
                    characterVelocity = new Vector3(characterVelocity.x, 0f, characterVelocity.z);

                    // then, add the jumpSpeed value upwards
                    characterVelocity += Vector3.up * jumpForce;

                    // remember last time we jumped because we need to prevent snapping to ground for a short time
                    lastTimeJumped = Time.time;
                    hasJumpedThisFrame = true;

                    // Force grounding to false
                    isGrounded = false;
                    groundNormal = Vector3.up;
                }
            }
        }
        else    // handle air movement
        {
            // add air acceleration
            characterVelocity += worldspaceMoveInput * accelerationSpeedInAir * Time.deltaTime;

            // limit air speed to a maximum, but only horizontally
            float verticalVelocity = characterVelocity.y;
            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(characterVelocity, Vector3.up);
            horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, maxSpeedInAir * speedModifier);
            characterVelocity = horizontalVelocity + (Vector3.up * verticalVelocity);

            // apply the gravity to the velocity
            characterVelocity += Vector3.down * gravityDownForce * Time.deltaTime;
        }

        // apply the final calculated velocity value as a character movement
        Vector3 capsuleBottomBeforeMove = GetCapsuleBottomHemisphere();
        Vector3 capsuleTopBeforeMove = GetCapsuleTopHemisphere(charController.height);

        charController.Move(characterVelocity * Time.deltaTime);

        // detect obstructions to adjust velocity accordingly
        latestImpactSpeed = Vector3.zero;
        if (Physics.CapsuleCast(capsuleBottomBeforeMove, capsuleTopBeforeMove, charController.radius,
            characterVelocity.normalized, out RaycastHit hit, characterVelocity.magnitude * Time.deltaTime, -1,
            QueryTriggerInteraction.Ignore))
        {
            // We remember the last impact speed because the fall damage logic might need it
            latestImpactSpeed = characterVelocity;

            characterVelocity = Vector3.ProjectOnPlane(characterVelocity, hit.normal);
        }
    }

    // Gets a reoriented direction that is tangent to a given slope
    public Vector3 GetDirectionReorientedOnSlope(Vector3 direction, Vector3 slopeNormal)
    {
        Vector3 directionRight = Vector3.Cross(direction, transform.up);
        return Vector3.Cross(slopeNormal, directionRight).normalized;
    }

    void GroundCheck()
    {
        // Make sure that the ground check distance while already in air is very small, to prevent suddenly snapping to ground
        float chosenGroundCheckDistance =
            isGrounded ? (charController.skinWidth + groundCheckDistance) : GROUND_CHECK_DISTANCE_IN_AIR;

        // reset values before the ground check
        isGrounded = false;
        groundNormal = Vector3.up;

        // only try to detect ground if it's been a short amount of time since last jump; otherwise we may snap to the ground instantly after we try jumping
        if (Time.time >= lastTimeJumped + JUMP_GROUNDING_PREVENT_TIME)
        {
            // if we're grounded, collect info about the ground normal with a downward capsule cast representing our character capsule
            if (Physics.CapsuleCast(GetCapsuleBottomHemisphere(), GetCapsuleTopHemisphere(charController.height),
                charController.radius, Vector3.down, out RaycastHit hit, chosenGroundCheckDistance, groundCheckLayers,
                QueryTriggerInteraction.Ignore))
            {
                // storing the upward direction for the surface found
                groundNormal = hit.normal;

                // Only consider this a valid ground hit if the ground normal goes in the same direction as the character up
                // and if the slope angle is lower than the character controller's limit
                if (Vector3.Dot(hit.normal, transform.up) > 0f &&
                    IsNormalUnderSlopeLimit(groundNormal))
                {
                    isGrounded = true;

                    // handle snapping to the ground
                    if (hit.distance > charController.skinWidth)
                    {
                        charController.Move(Vector3.down * hit.distance);
                    }
                }
            }
        }
    }

    // Gets the center point of the bottom hemisphere of the character controller capsule    
    Vector3 GetCapsuleBottomHemisphere()
    {
        return transform.position + (transform.up * charController.radius);
    }

    // Gets the center point of the top hemisphere of the character controller capsule    
    Vector3 GetCapsuleTopHemisphere(float atHeight)
    {
        return transform.position + (transform.up * (atHeight - charController.radius));
    }

    // Returns true if the slope angle represented by the given normal is under the slope angle limit of the character controller
    bool IsNormalUnderSlopeLimit(Vector3 normal)
    {
        return Vector3.Angle(transform.up, normal) <= charController.slopeLimit;
    }

    void UpdateCharacterHeight(bool force)
    {
        // Update height instantly
        if (force)
        {
            charController.height = targetCharacterHeight;
            charController.center = Vector3.up * charController.height * 0.5f;
            playerCam.transform.localPosition = Vector3.up * targetCharacterHeight * cameraHeightRatio;
        }
        // Update smooth height
        else if (charController.height != targetCharacterHeight)
        {
            // resize the capsule and adjust camera position
            charController.height = Mathf.Lerp(charController.height, targetCharacterHeight, crouchingSharpness * Time.deltaTime);
            charController.center = Vector3.up * charController.height * 0.5f;
            playerCam.transform.localPosition = Vector3.Lerp(playerCam.transform.localPosition, Vector3.up * targetCharacterHeight * cameraHeightRatio, crouchingSharpness * Time.deltaTime);
        }
    }

    // returns false if there was an obstruction
    bool SetCrouchingState(bool crouched, bool ignoreObstructions)
    {
        if (crouched)   // set appropriate heights
        {
            targetCharacterHeight = capsuleHeightCrouching;
        }
        else
        {
            // Detect obstructions
            if (!ignoreObstructions)
            {
                Collider[] standingOverlaps = Physics.OverlapCapsule(
                    GetCapsuleBottomHemisphere(),
                    GetCapsuleTopHemisphere(capsuleHeightStanding),
                    charController.radius,
                    -1,
                    QueryTriggerInteraction.Ignore);
                foreach (Collider c in standingOverlaps)
                {
                    if (c != charController)
                        return false;
                }
            }

            targetCharacterHeight = capsuleHeightStanding;
        }

        onStanceChanged?.Invoke(crouched);
        isCrouching = crouched;
        return true;
    }

    // void OnDrawGizmos()
    // {
    //     Gizmos.color = Color.red;
    //     Gizmos.DrawCube(GetCapsuleTopHemisphere(charController.height), Vector3.one * 0.1f);
    //     Gizmos.DrawCube(GetCapsuleBottomHemisphere(), Vector3.one * 0.1f);
    // }
}
