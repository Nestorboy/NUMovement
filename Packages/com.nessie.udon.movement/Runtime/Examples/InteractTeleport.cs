using UdonSharp;
using UnityEngine;

namespace Nessie.Udon.Movement
{
    [AddComponentMenu("Nessie/Movement/Examples/Interact Teleport")]
    public class InteractTeleport : UdonSharpBehaviour
    {
        [SerializeField] private NUMovement movement;
        [SerializeField] private Transform teleportTarget;
        
        public override void Interact()
        {
            movement._TeleportTo(teleportTarget.position, teleportTarget.rotation);
        }

        private void OnDrawGizmosSelected()
        {
            if (!teleportTarget) return;
            
            Gizmos.DrawLine(transform.position, teleportTarget.position);
        }
    }
}
