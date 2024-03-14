using UnityEngine;
using JetBrains.Annotations;
using VRC.SDKBase;

namespace Nessie.Udon.Movement
{
    [RequireComponent(typeof(CharacterController))]
    [AddComponentMenu("Nessie/Movement/NUMovement")]
    public class NUMovement : AbstractMovement
    {
        [SerializeField] private BoxCollider groundedCollider;

        [SerializeField] protected bool isActive = true;

        [Header("Misc")]
        [Tooltip("Determines what the player should inherit from the platform they're standing on.")]
        [SerializeField] protected PlatformInteraction platformMode = PlatformInteraction.Position;
        [Tooltip("Multiply the movement speed and jump height by the avatar eye height.")]
        [SerializeField] protected bool scaleMovement;

        [Header("Walking")]
        [Tooltip("The speed used when walking forward in desktop mode when not sprinting.")]
        [SerializeField] protected float walkSpeed = 2f;
        [Tooltip("The speed used when walking sideways.")]
        [SerializeField] protected float strafeSpeed = 2f;
        [Tooltip("The speed used when walking forward in vr or running in desktop mode.")]
        [SerializeField] protected float runSpeed = 4f;
        [Tooltip("The limit for how steep a surface can be before the user slides down.")]
        [SerializeField] [Min(0f)] protected float slopeLimit = 60f;
        [Tooltip("If enabled, the user will snap down to the ground when they become ungrounded and the ground below them is walkable.")]
        [SerializeField] protected bool groundSnap = true;
        [Tooltip("The maximum distance that the user will be able to snap down by.")]
        [SerializeField] [Min(0f)] protected float groundSnapHeight = 0.25f;

        [Header("Jumping")]
        [Tooltip("The maximum height at the apex of the jump")]
        [SerializeField] protected float jumpHeight = 1f;
        [Tooltip("If enabled, the user can hold down their jump button to keep jumping and retain their velocity.")]
        [SerializeField] protected bool jumpAuto = true;
        [Tooltip("If enabled, the user can let go of their jump button early and halve their upwards velocity.")]
        [SerializeField] protected bool jumpCancel = true;

        [Header("Shape")]
        [Tooltip("If enabled, the controller will use the default settings that the VRChat controller uses.")]
        [SerializeField] private bool defaultShape = true;
        [Tooltip("If enabled, the controller will be able to extend up and down depend on the users current head height.")]
        [SerializeField] protected bool heightAdjust = false;
        [Tooltip("If enabled, limits the minimum and maximum height of the controller.")]
        [SerializeField] protected bool clampHeight = false;
        [Tooltip("The minimum height of the controller.")]
        [SerializeField] [Min(0f)] protected float minHeight = 0f;
        [Tooltip("The maximum height of the controller.")]
        [SerializeField] [Min(0f)] protected float maxHeight = 100f;
        [Tooltip("The height of the controller if Default Shape is off.")]
        [SerializeField] [Min(0f)] protected float height = VRC_HEIGHT;
        [Tooltip("The radius of the controller if Default Shape is off.")]
        [SerializeField] [Min(0f)] protected float radius = VRC_RADIUS;

        [Header("Debug")]
        [SerializeField] public bool debugDrawer;
        [SerializeField] protected GameObject drawerPrefab;
        
        // Motion
        [PublicAPI] protected Vector3 Velocity;
        [PublicAPI] protected Vector3 MotionOffset;

        [PublicAPI] protected Quaternion LastLookRotation;
        
        [PublicAPI] protected float JumpHoldTime;
        [PublicAPI] protected bool IsJumping;
        [PublicAPI] protected float JumpTime;
        
        // Grounded
        [PublicAPI] protected bool IsGrounded;
        [PublicAPI] protected bool WasGrounded;
        [PublicAPI] protected bool IsSteep;
        [PublicAPI] protected bool WasSteep;
        [PublicAPI] protected bool IsWalkable;
        [PublicAPI] protected bool WasWalkable;
        [PublicAPI] protected bool ForcePlayerGrounded;
        
        [PublicAPI] protected Vector3 GroundUp;
        [PublicAPI] protected Transform GroundTransform;
        [PublicAPI] protected Vector3 RelativeGroundPosition;
        [PublicAPI] protected Quaternion GroundRotation;
        [PublicAPI] protected Quaternion GroundRotationDelta;
        [PublicAPI] protected Vector3 GroundVelocity;
        
        [PublicAPI] protected float PlayerHeight;

        #region Movement Events

        protected override void ControllerStart()
        {
            InitializeController();
            
            _SetWalkSpeed(walkSpeed);
            _SetStrafeSpeed(strafeSpeed);
            _SetRunSpeed(runSpeed);
            _SetJumpImpulse(Mathf.Sqrt(jumpHeight * GravityStrength * GravityMagnitude * 2f));

            Physics.IgnoreCollision(Controller, groundedCollider);
            CollisionMask = CollisionMask & ~(1 << groundedCollider.gameObject.layer);
            
            SnapToPlayer();
        }
        
        protected override void ControllerUpdate()
        {
            if (!isActive)
            {
                return;
            }

            ApplyGround();
            ApplyWalk();
            ApplyJump();
            ApplyGravity();

            Move(Velocity * DeltaTime + MotionOffset);

            ApplyGroundSnap();
            ApplyToPlayer();

            Vector3 origin = transform.position;
            DrawArrow(origin, origin + InputDirectionToMovementDirection(Vector3.right), Color.red);
            DrawArrow(origin, origin + InputDirectionToMovementDirection(Vector3.forward), Color.blue);
            DrawArrow(origin, origin + Velocity, Color.cyan); // Debug flattened velocity.
            if (IsWalkable)
            {
                DrawArrow(origin, origin + GroundVelocity * 0.1f, Color.magenta); // Debug inferred ground velocity.
            }
        }

        protected override void Move(Vector3 motion)
        {
            Unground();
            
            base.Move(motion);
            if (!WasGrounded && IsGrounded)
            {
                OnGrounded();
            }
            
            MotionOffset = Vector3.zero; // Consume.
        }
        
        protected override void ApplyHit(ControllerColliderHit hit)
        {
            Vector3 normal = hit.normal;
            Vector3 point = hit.point;
            
            Vector3 position = transform.position;
            Vector3 center = Controller.center;
            
            float capOffset = Controller.height * 0.5f - Controller.radius;
            Vector3 bottomCapCenter = position + ControllerDown * capOffset + center;
            Vector3 topCapCenter = position + ControllerUp * capOffset + center;

            if (Vector3.Dot(Velocity.normalized, normal) < 0f)
            {
                if (Vector3.Dot(point - bottomCapCenter, ControllerDown) >= 0f)
                {
                    IsGrounded = true;
                    GroundUp = normal;
                    if (hit.transform)
                    {
                        GroundTransform = hit.transform;
                        RelativeGroundPosition = GroundTransform.InverseTransformPoint(position);
                        GroundRotation = GroundTransform.rotation;
                    }

                    float normalAngle = Vector3.Angle(normal, -GravityDirection);
                    float toFootAngle = Vector3.Angle(point - bottomCapCenter, -GravityDirection);
                    float surfaceAngle = Mathf.Min(normalAngle, toFootAngle);
                    if (surfaceAngle > slopeLimit)
                    {
                        IsSteep = true;
                    }
                    else
                    {
                        IsWalkable = true;
                        if (!jumpAuto)
                        {
                            HoldJump = false; // Consume.
                        }
                    }
                }
                else if (Vector3.Dot(point - topCapCenter, ControllerUp) >= 0f)
                {
                    IsJumping = false;
                }
                
                if (IsWalkable)
                {
                    Velocity = HoldMove || HoldJump ? Vector3.ProjectOnPlane(Velocity, -GravityDirection) : Vector3.zero;
                }
                else
                {
                    Velocity = Vector3.ProjectOnPlane(Velocity, normal);
                }
            }
            
            Color debugColor = IsGrounded ? (!IsSteep ? Color.green : Color.yellow) : Color.red;
            DrawLine(bottomCapCenter, point, debugColor);
            DrawCircle(point, normal, 0.25f, debugColor);
        }
        
        #endregion Movement Events
        
        #region VRChat Events
        
        public override void OnPlayerRespawn(VRCPlayerApi player)
        {
            if (!player.isLocal) // Do a local check in case OnPlayerRespawn ever starts being called for remote users.
            {
                return;
            }

            SnapToPlayer();
            Velocity = -GravityDirection * 2f;
        }

        public override void OnAvatarEyeHeightChanged(VRCPlayerApi player, float prevEyeHeightAsMeters)
        {
            base.OnAvatarEyeHeightChanged(player, prevEyeHeightAsMeters);

            if (!player.isLocal || !scaleMovement) return;

            if (AvatarHeight < prevEyeHeightAsMeters)
            {
                float ratio = AvatarHeight / prevEyeHeightAsMeters;
                if (Vector3.Dot(Velocity, -GravityDirection) > 0f)
                    Velocity.y *= ratio;
                Velocity.x *= ratio;
                Velocity.z *= ratio;
            }
        }

        #endregion VRChat Events
        
        #region Controls

        protected virtual void ApplyGround()
        {
            if (IsWalkable && GroundTransform && GroundTransform.gameObject.activeInHierarchy)
            {
                if (platformMode == PlatformInteraction.PositionAndRotation)
                {
                    Quaternion oldGroundRotation = GroundRotation;
                    LookRotation *= Quaternion.Inverse(oldGroundRotation) * GroundTransform.rotation;
                }

                Vector3 oldPosition = transform.position;
                Vector3 newPosition = GroundTransform.TransformPoint(RelativeGroundPosition);
                GroundVelocity = (newPosition - oldPosition) / DeltaTime;
                if (platformMode != PlatformInteraction.None)
                {
                    _SetPosition(newPosition);

                    if (!WasGrounded)
                    {
                        Vector3 projGroundVelocity = Vector3.ProjectOnPlane(GroundVelocity, ControllerUp);
                        DrawArrow(transform.position, transform.position + -projGroundVelocity * 0.1f, Color.magenta, 5f); // Debug applied ground force.
                        _AddForce(-projGroundVelocity);
                    }
                }
            }
            else if (platformMode != PlatformInteraction.None)
            {
                DrawArrow(transform.position, transform.position + GroundVelocity * 0.1f, Color.magenta, 5f); // Debug applied unground force.
                _AddForce(GroundVelocity);
                GroundTransform = null;
                GroundVelocity = Vector3.zero;
            }
        }
        
        protected virtual void ApplyWalk()
        {
            if (InVR)
            {
                MotionOffset += GetVROffset(); // Don't affect velocity.
            }
            
            if (HoldMove)
            {
                float speedMultiplier = scaleMovement ? AvatarHeight : 1f;
                switch (PlayerStance)
                {
                    case Stance.Crouching:
                    {
                        speedMultiplier *= CROUCH_SPEED_MULTIPLIER;
                        break;
                    }
                    case Stance.Prone:
                    {
                        speedMultiplier *= PRONE_SPEED_MULTIPLIER;
                        break;
                    }
                }
                
                Vector3 inputVector = Vector3.ClampMagnitude(new Vector3(InputMoveX, 0f, InputMoveY), 1f) * speedMultiplier;
                inputVector.x *= strafeSpeed;
                inputVector.z *= HoldRun ? runSpeed : walkSpeed;
                Vector3 movementVector = InputDirectionToMovementDirection(inputVector);
                if (IsWalkable && !HoldJump)
                {
                    Velocity = movementVector;
                }
                else
                {
                    _TargetVelocity(movementVector, 5f * runSpeed * (scaleMovement ? AvatarHeight : 1f) * DeltaTime);
                }
            }
            else if (GroundTransform && IsWalkable && !HoldJump)
            {
                Velocity = Vector3.zero;
            }
        }

        protected virtual void ApplyJump()
        {
            if (IsJumping)
            {
                float jumpApex = JumpImpulse / (GravityStrength * GravityMagnitude);
                
                if (jumpCancel && !HoldJump)
                {
                    Vector3 jumpDirection = -GravityDirection;
                    _AddForce(-Vector3.Project(Velocity, jumpDirection) * 0.5f);
                    IsJumping = false;
                }
                else if (JumpTime < jumpApex)
                {
                    JumpTime += DeltaTime;
                }
                else
                {
                    JumpTime = jumpApex;
                    IsJumping = false;
                }
            }
            
            if (IsWalkable && HoldJump)
            {
                JumpTime = 0f;
                IsJumping = true;
                
                Vector3 jumpDirection = -GravityDirection;
                _AddForce(jumpDirection * (JumpImpulse * (scaleMovement ? AvatarHeight : 1f)) - Vector3.Project(Velocity, jumpDirection));
                OnJumped();
            }
        }

        protected virtual void ApplyGravity()
        {
            _AddForce(Gravity * (GravityStrength * (scaleMovement ? AvatarHeight : 1f) * DeltaTime));
        }
        
        protected virtual void ApplyGroundSnap()
        {
            if (!groundSnap) return;
            if (!WasWalkable || IsGrounded || IsJumping) return;
            
            Vector3 basePos = transform.position;
            float r = Controller.radius + Controller.skinWidth;
            Vector3 origin = basePos + ControllerUp * (r + 0.01f); // Add 0.01f padding to avoid snapping into colliders. Any better way to avoid this?
            float distance = groundSnapHeight + r + 0.01f;

            if (!Physics.Raycast(
                    origin,
                    ControllerDown,
                    out RaycastHit rayHit,
                    distance,
                    CollisionMask))
            {
                return; // Avoid snapping if the center of the player is not above ground.
            }
            
            if (!Physics.SphereCast(
                    origin,
                    r,
                    ControllerDown,
                    out RaycastHit sphereHit,
                    distance,
                    CollisionMask))
            {
                return;
            }
            
            Vector3 normal = sphereHit.normal;
            if (Vector3.Angle(normal, -GravityDirection) > slopeLimit)
            {
                return; // Only snap if the ground is walkable.
            }
            Vector3 point = sphereHit.point;
            Vector3 bottomCapCenter = point + normal * r;

            IsGrounded = true;
            IsWalkable = true;
            GroundUp = normal;
            if (sphereHit.transform)
            {
                Vector3 newFootPos = bottomCapCenter + ControllerDown * (r - Controller.skinWidth);
                transform.position = newFootPos;
                GroundTransform = sphereHit.transform;
                RelativeGroundPosition = GroundTransform.InverseTransformPoint(newFootPos);
                GroundRotation = GroundTransform.rotation;
            }

            if (!jumpAuto)
            {
                HoldJump = false; // Consume.
            }

            Velocity = HoldMove || HoldJump ? Vector3.ProjectOnPlane(Velocity, -GravityDirection) : Vector3.zero;
            
            Color debugColor = IsGrounded ? (!IsSteep ? Color.green : Color.yellow) : Color.red;
            DrawLine(bottomCapCenter, point, debugColor);
            DrawCircle(point, normal, 0.25f, debugColor);
        }
        
        // TODO: Figure out why velocity parameter is * 1.05f, maybe even regular velocity?
        protected virtual void ApplyToPlayer()
        {
            if (MainMenuOpen)
            {
                LocalPlayer.SetVelocity(Vector3.zero);
                return;
            }

            Vector3 transformPos = transform.position;
            
            // TODO: Figure out if it's possible to keep the player grounded 100% of the time.
            bool enableGroundCollider = IsGrounded || ForcePlayerGrounded;
            groundedCollider.enabled = enableGroundCollider;
            groundedCollider.center = transformPos;
            
            // TODO: Figure out why VRChats collider doesn't fully line up with this one.
            Quaternion rot = LookRotation;
            Vector3 pos = transformPos - rot * LocalPlayPosition * CameraScale;
            var orientation = InVR ? VRC_SceneDescriptor.SpawnOrientation.AlignRoomWithSpawnPoint : VRC_SceneDescriptor.SpawnOrientation.Default;
            LocalPlayer.TeleportTo(pos + ControllerDown * Controller.skinWidth, rot, orientation, true);

            bool playerIsMoving = Velocity.magnitude > 0.01f;
            bool groundIsMoving = GroundVelocity.magnitude > 0.01f;
            if (playerIsMoving && !IsGrounded || IsSteep || groundIsMoving)
            {
                var vel = Velocity / AvatarHeight;
                if (InVR && groundIsMoving)
                    vel += GetVROffset() / DeltaTime;

                LocalPlayer.SetVelocity(vel);
            }
        }

        #endregion Controls

        #region Movement Callbacks
        
        [PublicAPI] protected virtual void OnJumped() { }
        
        [PublicAPI] protected virtual void OnGrounded() { }
        
        [PublicAPI] protected virtual void OnTeleported() { }
        
        #endregion Movement Callback
        
        #region Input Methods
        
        [PublicAPI]
        public virtual void _ControllerEnable()
        {
            isActive = true;
            Velocity = LocalPlayer.GetVelocity();
            
            SnapToPlayer();
        }

        [PublicAPI]
        public virtual void _ControllerDisable()
        {
            isActive = false;
        }

        [PublicAPI]
        public virtual void _EnableInputs()
        {
            _SetCanMove(true);
            _SetCanLook(true);
        }
        
        [PublicAPI]
        public virtual void _DisableInputs()
        {
            _SetCanMove(false);
            _SetCanLook(false);
        }

        [PublicAPI]
        public virtual void _SetCanMove(bool canMove)
        {
            InputMoveEnabled = canMove;
        }

        [PublicAPI]
        public virtual void _SetCanLook(bool canLook)
        {
            InputLookEnabled = canLook;
        }
        
        #endregion Input Methods

        #region Player Methods
        
        [PublicAPI]
        public virtual void _Respawn()
        {
            LocalPlayer.Respawn(); // Use OnPlayerRespawn event to set the controller position to the players spawn position.
        }
        
        [PublicAPI]
        public virtual void _Respawn(int spawnsIndex)
        {
            LocalPlayer.Respawn(spawnsIndex);
        }
        
        [PublicAPI]
        public virtual void _SetPosition(Vector3 position)
        {
            transform.position = position;
            Physics.SyncTransforms();
        }

        [PublicAPI]
        public virtual void _TeleportTo(Vector3 position, bool lerpOnRemote = false)
        {
            _TeleportTo(position, LocalPlayer.GetRotation(), lerpOnRemote);
        }

        [PublicAPI]
        public virtual void _TeleportTo(Vector3 position, Quaternion rotation, bool lerpOnRemote = false)
        {
#if !UNITY_EDITOR
            rotation = Quaternion.Euler(0, rotation.eulerAngles.y, 0);
            
            Vector3 playerPos = LocalPlayer.GetPosition();
            Quaternion playerRot = LocalPlayer.GetRotation();
            Quaternion invPlayerRot = Quaternion.Inverse(playerRot);
            
            var originData = LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin);
            Vector3 originPos = originData.position;
            Quaternion originRot = originData.rotation;
            
            Vector3 posOffset = originPos - playerPos;
            Quaternion rotOffset = invPlayerRot * originRot;
            
            Quaternion targetRot = rotation * rotOffset;
            Vector3 targetPos = position + invPlayerRot * rotation * posOffset;
            
            _TeleportTo(targetPos, targetRot, VRC_SceneDescriptor.SpawnOrientation.AlignRoomWithSpawnPoint, lerpOnRemote);
#else //Avoiding problem where ClientSim behaves differently to VRChat
			_TeleportTo(position, rotation, orientation: VRC_SceneDescriptor.SpawnOrientation.Default, lerpOnRemote);
#endif
        }
        
        [PublicAPI]
        public virtual void _TeleportTo(Vector3 position, Quaternion rotation, VRC_SceneDescriptor.SpawnOrientation orientation, bool lerpOnRemote = false)
        {
            LocalPlayer.TeleportTo(position, rotation, orientation, lerpOnRemote);
            SnapToPlayer();
            OnTeleported();
        }

        [PublicAPI]
        public virtual float _GetGravityStrength() => GravityStrength;

        [PublicAPI]
        public virtual void _SetGravityStrength(float strength = 1f)
        {
            GravityStrength = strength;
            LocalPlayer.SetGravityStrength(strength);
        }

        [PublicAPI]
        public virtual float _GetGravityMagnitude() => GravityMagnitude * GravityStrength * (scaleMovement ? AvatarHeight : 1f);

        [PublicAPI]
        public virtual float _GetJumpImpulse() => JumpImpulse;

        [PublicAPI]
        public virtual void _SetJumpImpulse(float impulse = 3f)
        {
            JumpImpulse = impulse;
            LocalPlayer.SetJumpImpulse(impulse);
        }

        [PublicAPI]
        public virtual float _GetJumpHeight()
        {
            float gravityMagnitude = _GetGravityMagnitude();
            if (gravityMagnitude == 0f)
                return 0f;
            
            return (JumpImpulse * JumpImpulse) / (gravityMagnitude * 2f);
        }

        [PublicAPI]
        public virtual void _SetJumpHeight(float height)
        {
            _SetJumpImpulse(Mathf.Sqrt(height * _GetGravityMagnitude() * 2f));
        }

        [PublicAPI]
        public virtual float _GetRunSpeed() => RunSpeed;

        [PublicAPI]
        public virtual void _SetRunSpeed(float speed = 4f)
        {
            RunSpeed = speed;
            LocalPlayer.SetRunSpeed(speed);
        }

        [PublicAPI]
        public virtual float _GetStrafeSpeed() => StrafeSpeed;

        [PublicAPI]
        public virtual void _SetStrafeSpeed(float speed = 2f)
        {
            StrafeSpeed = speed;
            LocalPlayer.SetStrafeSpeed(speed);
        }

        [PublicAPI]
        public virtual float _GetWalkSpeed() => WalkSpeed;

        [PublicAPI]
        public virtual void _SetWalkSpeed(float speed = 2f)
        {
            WalkSpeed = speed;
            LocalPlayer.SetWalkSpeed(speed);
        }

        [PublicAPI]
        public virtual Vector3 _GetPosition() => PlayerPosition;

        [PublicAPI]
        public virtual Quaternion _GetRotation() => PlayerRotation;
        
        [PublicAPI]
        public virtual Vector3 _GetVelocity() => Velocity;

        [PublicAPI]
        public virtual void _SetVelocity(Vector3 velocity)
        {
            Velocity = velocity;
            if (Velocity.y >= _GetGravityMagnitude() * DeltaTime)
                Unground();
        }

        [PublicAPI]
        public virtual bool _IsPlayerGrounded() => IsWalkable;

        [PublicAPI]
        public virtual bool _GetForceGrounded() => ForcePlayerGrounded;

        [PublicAPI]
        public virtual void _SetForceGrounded(bool grounded) => ForcePlayerGrounded = grounded;

        [PublicAPI]
        public virtual void _AddForce(Vector3 force)
        {
            Velocity += force;
        }
        
        [PublicAPI]
        public virtual void _TargetVelocity(Vector3 target, float maxDelta)
        {
            Vector3 movementUp = IsWalkable ? GroundUp : ControllerUp;
            Vector3 flatVelocity = Vector3.ProjectOnPlane(Velocity, movementUp);
            Vector3 deltaVelocity = Vector3.MoveTowards(flatVelocity, target, maxDelta) - flatVelocity;

            if (IsSteep) // movementUp is ControllerUp
            {
                Vector3 groundOut = Vector3.ProjectOnPlane(GroundUp, movementUp).normalized;
                float scalar = Vector3.Dot(groundOut, deltaVelocity);
                scalar = Mathf.Min(0f, scalar);
                deltaVelocity -= scalar * groundOut;
            }
            
            _AddForce(deltaVelocity);
        }
        
        #endregion Player Methods

        #region Utility Methods
        
        [PublicAPI]
        protected void InitializeController()
        {
            if (defaultShape)
            {
                Controller.skinWidth = VRC_SKIN_WIDTH;
                Controller.center = ControllerUp * 0.8f;
                Controller.radius = VRC_RADIUS;
                Controller.height = VRC_HEIGHT;
            }
            else
            {
                Controller.center = height * 0.5f * ControllerUp;
                Controller.radius = radius;
                Controller.height = height;
            }

            Controller.stepOffset = 0f; // Step offset is all kinds of jank.
            Controller.slopeLimit = 180f; // Avoid being unable to walk up steep surfaces, use custom limit instead.
            Controller.minMoveDistance = 0f; // Why isn't this the default?
        }
        
        [PublicAPI]
        protected void SnapToPlayer()
        {
            Vector3 playerPos;
            if (InVR)
            {
                var originData = LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin);
                var headData = LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
                Vector3 originPos = originData.position;
                Vector3 headPos = headData.position;
                PlayRotation = originData.rotation;
                Vector3 newPlayPosition = Quaternion.Inverse(PlayRotation) * (headPos - originPos) / CameraScale;
                newPlayPosition.y = 0f; // Ignore vertical offset.
                LocalPlayPositionDelta = newPlayPosition - LocalPlayPosition;
                LocalPlayPosition = newPlayPosition;

                playerPos = headPos;
                playerPos.y = LocalPlayer.GetPosition().y;
            }
            else
            {
                playerPos = LocalPlayer.GetPosition();
            }

            Unground();
            
            _SetPosition(playerPos);
        }
        
        [PublicAPI]
        protected void Unground()
        {
            WasGrounded = IsGrounded;
            WasSteep = IsSteep;
            WasWalkable = IsWalkable;
            IsGrounded = false;
            IsSteep = false;
            IsWalkable = false;
        }
        
        [PublicAPI]
        protected Vector3 GetVROffset()
        {
            return PlayRotation * LocalPlayPositionDelta * CameraScale;
        }
        
        [PublicAPI]
        protected Vector3 InputDirectionToMovementDirection(Vector3 inputDirection)
        {
            Vector3 worldDirection = InputToWorld * inputDirection;
            return WorldDirectionToMovementDirection(worldDirection);
        }

        [PublicAPI]
        protected Vector3 WorldDirectionToMovementDirection(Vector3 worldDirection)
        {
            Plane movementPlane = new Plane(IsWalkable ? GroundUp : ControllerUp, Vector3.zero);
            movementPlane.Raycast(new Ray(worldDirection, ControllerUp), out float distance);
            Vector3 newDirection = (worldDirection + ControllerUp * distance).normalized;
            return newDirection * worldDirection.magnitude;
        }
        
        #endregion Utility Methods

        #region Debug

        [PublicAPI]
        protected void DrawLine(Vector3 start, Vector3 end, Color color, float duration = 0f)
        {
            if (!debugDrawer)
            {
                return;
            }
            
            #if COMPILER_UDONSHARP && !UNITY_EDITOR
            var drawer = CreateDrawer();
            drawer.transform.position = start;
            drawer.startColor = color;
            drawer.endColor = color;
            drawer.time = duration;
            drawer.AddPosition(end);
            #else
            Debug.DrawLine(start, end, color, duration);
            #endif
        }

        [PublicAPI]
        protected void DrawArrow(Vector3 start, Vector3 end, Color color, float duration = 0f)
        {
            if (!debugDrawer)
            {
                return;
            }

            Vector3 headPos = LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
            
            Vector3 to = end - start;
            Vector3 right = Vector3.Cross(to, headPos - end).normalized;
            Vector3 sideOffset = to.magnitude * 0.05f * right;
            Vector3 backOffset = to * -0.1f;

            Vector3 tipP = end;
            Vector3 startP = start;
            Vector3 midP = end + backOffset;
            Vector3 rightP = midP + sideOffset;
            Vector3 leftP = midP - sideOffset;
            
            #if COMPILER_UDONSHARP && !UNITY_EDITOR
            Vector3[] points = new Vector3[] { startP, midP, leftP, tipP, rightP, midP, };

            var drawer = CreateDrawer();
            drawer.transform.position = points[points.Length - 1];
            drawer.startColor = color;
            drawer.endColor = color;
            drawer.time = duration;
            drawer.AddPositions(points);
            #else
            Debug.DrawLine(startP, midP, color, duration);
            Debug.DrawLine(tipP, leftP, color, duration);
            Debug.DrawLine(tipP, rightP, color, duration);
            Debug.DrawLine(leftP, rightP, color, duration);
            #endif
        }
        
        [PublicAPI]
        protected void DrawCircle(Vector3 center, Vector3 normal, float radius, Color color, float duration = 0f)
        {
            if (!debugDrawer)
            {
                return;
            }
            
            Vector3 right = Vector3.Cross(normal, ControllerUp);
            right = right.sqrMagnitude > 0.0001f ? right.normalized : Vector3.right;
            Vector3 forward = Vector3.Cross(right, normal).normalized;
            
            #if COMPILER_UDONSHARP && !UNITY_EDITOR
            Vector3[] points = new Vector3[16];
            #endif

            float valueToRadian = Mathf.PI * 2f / 16f;
            for (int i = 0; i < 16; i++)
            {
                float currT = i * valueToRadian;
                float currX = Mathf.Sin(currT);
                float currZ = Mathf.Cos(currT);
                Vector3 currP = center + (forward * currX + right * currZ) * radius;

                #if COMPILER_UDONSHARP && !UNITY_EDITOR
                points[i] = currP;
                #else
                float prevT = (i + 1) * valueToRadian;
                float prevX = Mathf.Sin(prevT);
                float prevZ = Mathf.Cos(prevT);
                Vector3 prevP = center + (forward * prevX + right * prevZ) * radius;

                Debug.DrawLine(currP, prevP, color, duration);
                #endif
            }
            
            #if COMPILER_UDONSHARP && !UNITY_EDITOR
            var drawer = CreateDrawer();
            drawer.transform.position = points[points.Length - 1];
            drawer.startColor = color;
            drawer.endColor = color;
            drawer.time = duration;
            drawer.AddPositions(points);
            #endif
        }

        #if COMPILER_UDONSHARP && !UNITY_EDITOR
        protected TrailRenderer CreateDrawer()
        {
            return Instantiate(drawerPrefab).GetComponent<TrailRenderer>();
        }
        #endif
        
        #endregion Debug
    }
}
