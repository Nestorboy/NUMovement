using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UdonSharp;

public class AvatarScalingWrapper : UdonSharpBehaviour
{
    private const string PPlayer = "p_Player";
    private const string PBool = "p_Bool";
    private const string PFloat = "p_Float";
    private const string RBool = "r_Bool";
    private const string RFloat = "r_Float";
    
    [SerializeField] private UdonBehaviour avatarScalingWrapper;

    public bool IsAvailable => avatarScalingWrapper.enabled;

    public bool _GetManualAvatarScalingAllowed(VRCPlayerApi player)
    {
        avatarScalingWrapper.SetProgramVariable(PPlayer, player);
        avatarScalingWrapper.SendCustomEvent(nameof(_GetManualAvatarScalingAllowed));
        return IsAvailable && (bool)avatarScalingWrapper.GetProgramVariable(RBool);
    }
    
    public void _SetManualAvatarScalingAllowed(VRCPlayerApi player, bool allowed)
    {
        avatarScalingWrapper.SetProgramVariable(PPlayer, player);
        avatarScalingWrapper.SetProgramVariable(PBool, allowed);
        avatarScalingWrapper.SendCustomEvent(nameof(_SetManualAvatarScalingAllowed));
    }

    public float _GetAvatarEyeHeightMinimumAsMeters(VRCPlayerApi player)
    {
        avatarScalingWrapper.SetProgramVariable(PPlayer, player);
        avatarScalingWrapper.SendCustomEvent(nameof(_GetAvatarEyeHeightMinimumAsMeters));
        return IsAvailable ? (float)avatarScalingWrapper.GetProgramVariable(RFloat) : 0f;
    }
    
    public void _SetAvatarEyeHeightMinimumByMeters(VRCPlayerApi player, float height)
    {
        avatarScalingWrapper.SetProgramVariable(PPlayer, player);
        avatarScalingWrapper.SetProgramVariable(PFloat, height);
        avatarScalingWrapper.SendCustomEvent(nameof(_SetAvatarEyeHeightMinimumByMeters));
    }

    public float _GetAvatarEyeHeightMaximumAsMeters(VRCPlayerApi player)
    {
        avatarScalingWrapper.SetProgramVariable(PPlayer, player);
        avatarScalingWrapper.SendCustomEvent(nameof(_GetAvatarEyeHeightMaximumAsMeters));
        return IsAvailable ? (float)avatarScalingWrapper.GetProgramVariable(RFloat) : 0f;
    }
    
    public void _SetAvatarEyeHeightMaximumByMeters(VRCPlayerApi player, float height)
    {
        avatarScalingWrapper.SetProgramVariable(PPlayer, player);
        avatarScalingWrapper.SetProgramVariable(PFloat, height);
        avatarScalingWrapper.SendCustomEvent(nameof(_SetAvatarEyeHeightMaximumByMeters));
    }

    public float _GetAvatarEyeHeightAsMeters(VRCPlayerApi player)
    {
        avatarScalingWrapper.SetProgramVariable(PPlayer, player);
        avatarScalingWrapper.SendCustomEvent(nameof(_GetAvatarEyeHeightAsMeters));
        return IsAvailable ? (float)avatarScalingWrapper.GetProgramVariable(RFloat) : 1f;
    }
    
    public void _SetAvatarEyeHeightByMeters(VRCPlayerApi player, float height)
    {
        avatarScalingWrapper.SetProgramVariable(PPlayer, player);
        avatarScalingWrapper.SetProgramVariable(PFloat, height);
        avatarScalingWrapper.SendCustomEvent(nameof(_SetAvatarEyeHeightByMeters));
    }
    
    public void _SetAvatarEyeHeightByMultiplier(VRCPlayerApi player, float multiplier)
    {
        avatarScalingWrapper.SetProgramVariable(PPlayer, player);
        avatarScalingWrapper.SetProgramVariable(PFloat, multiplier);
        avatarScalingWrapper.SendCustomEvent(nameof(_SetAvatarEyeHeightByMultiplier));
    }
}
