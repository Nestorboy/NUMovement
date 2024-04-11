using UnityEngine;
using JetBrains.Annotations;
using VRC.SDKBase;
using VRC.Udon.Common;
using UdonSharp;

namespace Nessie.Udon.Movement
{
    [RequireComponent(typeof(CharacterController))]
    public abstract class AbstractMovement : UdonSharpBehaviour
    {
        public const string PACKAGE_VERSION = "0.4.1";
        
        protected const float VRC_SKIN_WIDTH = 0.005f;
        protected const float VRC_HEIGHT = 1.6f;
        protected const float VRC_RADIUS = 0.2f;

        protected const float CROUCH_SPEED_MULTIPLIER = 0.5f;
        protected const float PRONE_SPEED_MULTIPLIER = 0.1f;

        [SerializeField] protected Transform audioListenerScale;
        
        // Constants
        [PublicAPI] protected CharacterController Controller;
        [PublicAPI] protected VRCPlayerApi LocalPlayer;
        [PublicAPI] protected bool InVR;

        [PublicAPI] protected Vector3 ControllerDown = Vector3.down;
        [PublicAPI] protected Vector3 ControllerUp = Vector3.up;

        [PublicAPI] protected LayerMask CollisionMask;
        
        // Dynamic
        [PublicAPI] protected Vector3 Gravity;
        [PublicAPI] protected Vector3 GravityDirection;
        [PublicAPI] protected float GravityMagnitude;
        [PublicAPI] protected float GravityStrength;

        [PublicAPI] protected float DeltaTime;
        
        // Input
        [PublicAPI] protected bool InputMoveEnabled = true;
        [PublicAPI] protected float InputMoveX;
        [PublicAPI] protected float InputMoveY;
        [PublicAPI] protected bool HoldMove;
        [PublicAPI] protected bool HoldRun;
        [PublicAPI] protected bool HoldJump;
        
        [PublicAPI] protected bool InputLookEnabled = true;
        [PublicAPI] protected float InputLookX;
        [PublicAPI] protected float InputLookY;
        [PublicAPI] protected bool HoldLook;
        
        [PublicAPI] protected Quaternion InputToWorld;
        
        private readonly Collider[] _menuColliders = new Collider[12]; // Buffer for menu collider count.
        [PublicAPI] protected bool MenuOpen;
        [PublicAPI] protected bool MainMenuOpen;
        [PublicAPI] protected bool QuickMenuOpen;
        
        // Player
        [PublicAPI] protected Vector3 PlayerPosition;
        [PublicAPI] protected Quaternion PlayerRotation;
        [PublicAPI] protected Quaternion LookRotation;
        [PublicAPI] protected Stance PlayerStance;
        [PublicAPI] protected float CameraScale;
        [PublicAPI] protected float AvatarHeight = 1f;

        [PublicAPI] protected Vector3 LocalPlayPosition;
        [PublicAPI] protected Quaternion PlayRotation;
        [PublicAPI] protected Vector3 LocalPlayPositionDelta;

        [PublicAPI] protected float WalkSpeed;
        [PublicAPI] protected float StrafeSpeed;
        [PublicAPI] protected float RunSpeed;
        [PublicAPI] protected float JumpImpulse;
        
        // Collision
        private readonly object[] _hits = new object[8]; // The values have to be boxed since the ControllerColliderHit[] type isn't exposed.
        private int _hitCount;
        private int _hitCountTotal;
        
        #region Unity Events
        
        private protected virtual void Start()
        {
            DebugUtilities.Log($"Loaded Nessie's Udon Movement {PACKAGE_VERSION}");
            
            InitializeReferences();

            if (!VRC.SDKBase.Utilities.IsValid(LocalPlayer)) return;

            UpdateCachedVariables();
            ControllerStart();
        }

        private protected virtual void LateUpdate()
        {
            if (!VRC.SDKBase.Utilities.IsValid(LocalPlayer)) return; // Avoid throwing an exception when leaving worlds.
            
            UpdateCachedVariables();
            UpdateInputs();
            ControllerUpdate();
        }
        
        private protected virtual void OnControllerColliderHit(ControllerColliderHit hit) // Cache hits for later as transform.position hasn't updated yet.
        {
            _hitCountTotal++;
            if (_hitCountTotal >= _hits.Length)
            {
                return;
            }
            
            _hits[_hitCount++] = hit;
        }
        
        #endregion Unity Events
        
        #region VRChat Events

        public override void OnAvatarEyeHeightChanged(VRCPlayerApi player, float prevEyeHeightAsMeters)
        {
            if (!player.isLocal) return;

            AvatarHeight = player.GetAvatarEyeHeightAsMeters();
        }

        public override void InputJump(bool value, UdonInputEventArgs args) => HoldJump = value;

        public override void InputMoveHorizontal(float value, UdonInputEventArgs args) => InputMoveX = value;

        public override void InputMoveVertical(float value, UdonInputEventArgs args) => InputMoveY = value;
        
        public override void InputLookHorizontal(float value, UdonInputEventArgs args) => InputLookX = value;

        public override void InputLookVertical(float value, UdonInputEventArgs args) => InputLookY = value;

        #endregion VRChat Events
        
        #region Movement Events
        
        /// <summary>
        /// Called if everything was initialized properly during start.
        /// </summary>
        [PublicAPI]
        protected virtual void ControllerStart() { }
        
        /// <summary>
        /// Called after updating cached variable values in LateUpdate.
        /// </summary>
        [PublicAPI]
        protected virtual void ControllerUpdate() { }
        
        /// <summary>
        /// Called for all points of contact achieved inside of the move function that fit inside of the hit buffer.
        /// </summary>
        /// <param name="hit"></param>
        [PublicAPI]
        protected virtual void ApplyHit(ControllerColliderHit hit) { }
        
        [PublicAPI]
        protected virtual void Move(Vector3 motion)
        {
            _hitCount = 0;
            _hitCountTotal = 0;
            Controller.Move(motion);

            if (_hitCountTotal >= _hits.Length)
            {
                Debug.LogWarning($"Hit count ({_hitCountTotal}) exceeded hit buffer size ({_hits.Length}).");
            }
            
            for (int i = 0; i < _hitCount; i++)
            {
                ControllerColliderHit hit = (ControllerColliderHit)_hits[i];
                ApplyHit(hit);
            }
        }
        
        #endregion Movement Events

        private void InitializeReferences()
        {
            if (!Controller)
            {
                Controller = GetComponent<CharacterController>();
            }

            audioListenerScale = Instantiate(audioListenerScale.gameObject).transform; // Has to be in the scene for the scaling to be applied.
            
            LocalPlayer = Networking.LocalPlayer;
            if (VRC.SDKBase.Utilities.IsValid(LocalPlayer))
            {
                InVR = LocalPlayer.IsUserInVR();
            }
            
            CollisionMask = LayerUtility.GetCollisionMask(gameObject.layer);
        }

        private void UpdateCachedVariables()
        {
            Gravity = Physics.gravity;
            GravityDirection = Gravity.normalized;
            GravityMagnitude = Gravity.magnitude;

            DeltaTime = Time.deltaTime;

            PlayerPosition = LocalPlayer.GetPosition();
            PlayerRotation = LocalPlayer.GetRotation();
            GravityStrength = LocalPlayer.GetGravityStrength();
            WalkSpeed = LocalPlayer.GetWalkSpeed();
            StrafeSpeed = LocalPlayer.GetStrafeSpeed();
            RunSpeed = LocalPlayer.GetRunSpeed();
            JumpImpulse = LocalPlayer.GetJumpImpulse();
            CameraScale = 1f / audioListenerScale.localScale.y;
            
            float upright = (LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position.y - PlayerPosition.y) / AvatarHeight;
            PlayerStance = upright > 0.65f ? Stance.Standing : upright > 0.4f ? Stance.Crouching : Stance.Prone;
            //upright = RemapRange(0f, 1f, 0.0365f, 1.005f, upright);
            //PlayerStance = upright > 1f / 1.52f ? Stance.Standing : upright > 1f / 2.48f ? Stance.Crouching : Stance.Prone;
            
            var originData = LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin);
            PlayRotation = originData.rotation;
            if (InVR)
            {
                var headData = LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
                Vector3 newPlayPosition = Quaternion.Inverse(PlayRotation) * (headData.position - originData.position) / CameraScale;
                newPlayPosition.y = 0f; // Ignore vertical offset.
                LocalPlayPositionDelta = newPlayPosition - LocalPlayPosition;
                LocalPlayPosition = newPlayPosition;
                
                Quaternion headRotation = headData.rotation;
                Vector3 headForward = headRotation * Vector3.forward;
                float scalar = Vector3.Dot(headForward, ControllerUp);
                bool lookingUp = scalar >= 0f;
                float forwardBias = lookingUp ? 52f : -66f;
                Vector3 moveForward = Quaternion.Slerp(
                    headRotation,
                    headRotation * Quaternion.Euler(forwardBias, 0f, 0f),
                    scalar * scalar - 0.2f) * Vector3.forward;
                InputToWorld = Quaternion.LookRotation(Vector3.ProjectOnPlane(moveForward, ControllerUp).normalized);
            }
            else
            {
                InputToWorld = PlayRotation;
            }

            if (InputLookEnabled)
            {
                LookRotation = PlayRotation;
            }
        }

        private void UpdateInputs()
        {
            DetectMenu();
            
            if (InputMoveEnabled && !MainMenuOpen)
            {
                DetectInputMove();
                DetectInputRun();
            }
            else
            {
                InputMoveX = 0f;
                InputMoveY = 0f;
                HoldMove = false;
                HoldRun = false;
                HoldJump = false;
            }

            if (!InVR && MenuOpen)
            {
                HoldJump = false;
            }

            if (InputLookEnabled && (InVR ? !MainMenuOpen : !MenuOpen))
            {
                DetectInputLook();
            }
            else
            {
                InputLookX = 0f;
                InputLookY = 0f;
                HoldLook = false;
            }
        }
        
        private void DetectMenu() // Not perfect, but better than nothing. Breaks if more colliders are introduced or removed.
        {
            int count = Physics.OverlapSphereNonAlloc(PlayerPosition, 10f, _menuColliders, 1 << 19);
            MainMenuOpen = count == 8 || count == 9 || count == 10;
            QuickMenuOpen = count == 11 || count == 12;
            MenuOpen = MainMenuOpen || QuickMenuOpen; // Main Menu: 8, 9, 10 Quick Menu: 11, 12
        }
        
        private void DetectInputMove()
        {
            HoldMove = InputMoveX != 0f || InputMoveY != 0f;
        }
        
        private void DetectInputRun()
        {
            HoldRun = InVR || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        }

        private void DetectInputLook()
        {
            HoldLook = InputLookX != 0f || InputLookY != 0f;
        }
    }
}
