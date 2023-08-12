using System;
using UnityEditor;
using UnityEngine;

namespace Nessie.Udon.Movement.Editor
{
    public static class ControllerLayerUtility
    {
        private const int BUILTIN_LAYER_COUNT = 8;
        private const int VRCHAT_LAYER_COUNT = 14;
        private const int RESERVED_LAYER_COUNT = BUILTIN_LAYER_COUNT + VRCHAT_LAYER_COUNT;

        private const string CONTROLLER_LAYER_NAME = "PlayerController";
        
        public static LayerMask ControllerCollisionMask => LayerUtility.GetCollisionMask("PlayerLocal") & ~(1 << LayerMask.NameToLayer("MirrorReflection")); // Identical to PlayerLocal, but ignore MirrorReflection to prevent collider flight.

        public static bool IsControllerLayer(int layer)
        {
            string layerName = LayerMask.LayerToName(layer);
            if (string.IsNullOrEmpty(layerName))
            {
                return false;
            }

            return LayerUtility.GetCollisionMask(layer) == ControllerCollisionMask;
        }
        
        public static bool TrySetControllerLayer(GameObject gameObject, bool useCustomLayer)
        {
            int newLayer;
            if (useCustomLayer)
            {
                newLayer = GetControllerLayer();
                if (newLayer < 0)
                {
                    return false;
                }
            }
            else
            {
                newLayer = LayerMask.NameToLayer("PlayerLocal");
            }
            
            Undo.RecordObject(gameObject, "Layer Change");
            PrefabUtility.RecordPrefabInstancePropertyModifications(gameObject);
            gameObject.layer = newLayer;
            
            return true;
        }
        
        private static int GetControllerLayer()
        {
            int foundLayer = GetCompatibleLayer();
            if (foundLayer >= 0)
            {
                return foundLayer;
            }

            if (!EditorUtility.DisplayDialog("Add new layer?", $"Unable to find suitable layer for controller.\nDo you want to add the {CONTROLLER_LAYER_NAME} layer?", "Add Layer", "Cancel"))
            {
                return -1;
            }
            
            return AddControllerLayer();
        }

        private static int AddControllerLayer()
        {
            int newLayer = GetNextFreeLayer();
            if (newLayer < 0)
            {
                Debug.LogError("Unable to add a new layer since every single one is already in use.");
                return -1;
            }
            
            LayerUtility.SetLayerNameUndo(newLayer, CONTROLLER_LAYER_NAME);
            LayerUtility.SetCollisionMask(newLayer, ControllerCollisionMask & ~(1 << newLayer));
            return newLayer;
        }

        private static int GetCompatibleLayer()
        {
            for (int i = RESERVED_LAYER_COUNT; i < 32; i++)
            {
                string layerName = LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(layerName) && LayerUtility.GetCollisionMask(i) == ControllerCollisionMask)
                {
                    return i;
                }
            }
            
            return -1;
        }

        private static int GetNextFreeLayer()
        {
            for (int i = RESERVED_LAYER_COUNT; i < 32; i++)
            {
                string layerName = LayerMask.LayerToName(i);
                if (string.IsNullOrEmpty(layerName))
                {
                    return i;
                }
            }
            
            return -1;
        }
    }
}
