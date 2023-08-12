using UnityEngine;
using UdonSharp;

namespace Nessie.Udon.Movement
{
    public abstract class AbstractMovementCollider : UdonSharpBehaviour
    {
        protected virtual void OnCollisionEnter(Collision collision)
        {
            if (!TryGetController(collision.collider, out NUMovement controller)) return;
            OnControllerCollisionEnter(controller, collision);
        }

        protected virtual void OnCollisionExit(Collision other)
        {
            if (!TryGetController(other.collider, out NUMovement controller)) return;
            OnControllerCollisionExit(controller, other);
        }

        protected virtual void OnCollisionStay(Collision collisionInfo)
        {
            if (!TryGetController(collisionInfo.collider, out NUMovement controller)) return;
            OnControllerCollisionStay(controller, collisionInfo);
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            if (!TryGetController(other, out NUMovement controller)) return;
            OnControllerTriggerEnter(controller);
        }

        protected virtual void OnTriggerExit(Collider other)
        {
            if (!TryGetController(other, out NUMovement controller)) return;
            OnControllerTriggerExit(controller);
        }

        protected virtual void OnTriggerStay(Collider other)
        {
            if (!TryGetController(other, out NUMovement controller)) return;
            OnControllerTriggerStay(controller);
        }

        private bool TryGetController(Collider collider, out NUMovement controller)
        {
            controller = null;
            if (!collider || collider.GetType() != typeof(CharacterController)) return false;
            controller = collider.GetComponent<NUMovement>();
            return controller;
        }
        
        protected virtual void OnControllerCollisionEnter(NUMovement controller, Collision collision) { }
        
        protected virtual void OnControllerCollisionExit(NUMovement controller, Collision collision) { }
        
        protected virtual void OnControllerCollisionStay(NUMovement controller, Collision collision) { }
        
        protected virtual void OnControllerTriggerEnter(NUMovement controller) { }
        
        protected virtual void OnControllerTriggerExit(NUMovement controller) { }
        
        protected virtual void OnControllerTriggerStay(NUMovement controller) { }
    }
}
