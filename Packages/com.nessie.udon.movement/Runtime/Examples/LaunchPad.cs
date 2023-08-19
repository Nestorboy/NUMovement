using UnityEngine;

namespace Nessie.Udon.Movement
{
    [AddComponentMenu("Nessie/Movement/Examples/Launch Pad")]
    public class LaunchPad : AbstractMovementCollider
    {
        [SerializeField] private Transform launchTarget;
        [SerializeField] [Range(1f, 89f)] private float launchAngleDegrees = 45f;
        
        protected override void OnControllerTriggerEnter(NUMovement controller)
        {
            Vector3 position = controller._GetPosition();
            Vector3 launchVelocity = ComputeForce(position, launchTarget.position, launchAngleDegrees, controller._GetGravityMagnitude());
            controller._SetVelocity(launchVelocity);
        }

        private Vector3 ComputeForce(Vector3 source, Vector3 target, float angle, float gravity)
        {
            Vector3 direction = target - source;
            float height = direction.y;
            direction.y = 0f;
            float distance = direction.magnitude;
            float angleRad = angle * Mathf.Deg2Rad;
            float t = Mathf.Tan(angleRad);
            direction.y = distance * t;
            distance += height / t;
            
            float force = Mathf.Sqrt(distance * gravity / Mathf.Sin(angleRad * 2f));
            return force * direction.normalized;
        }

        private void OnDrawGizmosSelected()
        {
            if (!launchTarget) return;
            
            Vector3 gravity = Physics.gravity;
            Vector3 origin = transform.position;
            Vector3 target = launchTarget.position;
            Vector3 force = ComputeForce(origin, target, launchAngleDegrees, gravity.magnitude);
            float travelTime = Vector3.ProjectOnPlane(target - origin, gravity).magnitude / Vector3.ProjectOnPlane(force, gravity).magnitude;
            Vector3[] points = new Vector3[16];
            for (int i = 0; i < points.Length; i++)
            {
                float t = (float)i / (points.Length - 1) * travelTime;
                points[i] = origin + force * t + gravity * 0.5f * t * t;
            }

            for (int i = 0; i < points.Length - 1; i++)
            {
                Gizmos.DrawLine(points[i], points[i + 1]);
            }
        }
    }
}