
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Nessie.Udon.Movement.Editor
{
    [CustomEditor(typeof(CharacterController))]
    public class CharacterControllerInspector : UnityEditor.Editor
    {
        private CharacterController _controller;
        private bool _hasMovement;
        private NUMovement _movement;

        private void OnEnable()
        {
            _controller = (CharacterController)target;
            _hasMovement = _controller.TryGetComponent(out _movement);
        }

        public override void OnInspectorGUI()
        {
            using (new EditorGUI.DisabledScope(_hasMovement && _movement))
            {
                base.OnInspectorGUI();
            }
        }

        private void OnSceneGUI()
        {
            if (!_hasMovement) return;
            
            SerializedObject serializedMovement = new SerializedObject(_movement);
            bool shouldSnap = serializedMovement.FindProperty("groundSnap").boolValue;
            if (!shouldSnap) return;
            
            CompareFunction oldZTest = Handles.zTest;
            Handles.zTest = CompareFunction.LessEqual;
            
            using (new Handles.DrawingScope(Color.cyan))
            {
                float snapHeight = serializedMovement.FindProperty("groundSnapHeight").floatValue;
                Vector3 up = Vector3.up;
                Vector3 right = Vector3.right;
                Vector3 forward = Vector3.forward;
                Vector3 center = _controller.center;
                Vector3 scale = _movement.transform.localScale;
                float radius = _controller.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
                Vector3 bottomCapCenter = _movement.transform.position;
                bottomCapCenter += _movement.transform.rotation * center * scale.y;
                bottomCapCenter -= up * Mathf.Max(0f, Mathf.Abs(scale.y) * _controller.height / 2f - radius);
                
                Vector3[] bodyLinePoints = new Vector3[8];
                for (int i = 0; i < bodyLinePoints.Length; i++)
                {
                    Vector3 p = bottomCapCenter - up * (snapHeight * (i % 2));
                    p += radius * (forward + (right - forward) * (i / 2 % 2) - (right + forward) * (i / 4));
                    bodyLinePoints[i] = p;
                }
                Handles.DrawLines(bodyLinePoints);

                Vector3 snapCapCenter = bottomCapCenter - up * snapHeight;
                Handles.DrawWireDisc(snapCapCenter, up, radius);
                Handles.DrawWireArc(snapCapCenter, right, forward, 180f, radius);
                Handles.DrawWireArc(snapCapCenter, -forward, right, 180f, radius);
            }

            Handles.zTest = oldZTest;
        }
    }
}
