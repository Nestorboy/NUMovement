using UnityEditor;
using UnityEngine;
using UdonSharpEditor;

namespace Nessie.Udon.Movement.Editor
{
    [CustomEditor(typeof(NUMovement))]
    public class NUMovementInspector : UnityEditor.Editor
    {
        private NUMovement _controller;

        private void OnEnable()
        {
            _controller = (NUMovement)target;
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
            {
                return;
            }

            DrawLayerToggle();
            UdonSharpGUI.DrawVariables(_controller);
        }

        private void DrawLayerToggle()
        {
            using (new EditorGUI.DisabledScope(!UpdateLayers.AreLayersSetup()))
            {
                GameObject controllerGO = _controller.gameObject;
                bool onControllerLayer = ControllerLayerUtility.IsControllerLayer(controllerGO.layer);
                
                EditorGUI.BeginChangeCheck();
                bool useCustomLayer = EditorGUILayout.Toggle(new GUIContent("Use Custom Layer", "Use a custom layer to prevent avatar flight."), onControllerLayer);
                if (EditorGUI.EndChangeCheck())
                {
                    ControllerLayerUtility.TrySetControllerLayer(controllerGO, useCustomLayer);
                }
            }
        }
    }
}
