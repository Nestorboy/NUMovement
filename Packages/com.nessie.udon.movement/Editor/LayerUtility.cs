using UnityEditor;
using UnityEngine;

namespace Nessie.Udon.Movement.Editor
{
    public static class LayerUtility
    {
        private const string TM_PATH = "ProjectSettings/TagManager.asset";
        private const string TM_PROPS_LAYERS = "layers";
        private const string TM_PROPS_TAGS = "tags";
        
        public static LayerMask GetCollisionMask(string layerName) => GetCollisionMask(LayerMask.NameToLayer(layerName));
        
        public static LayerMask GetCollisionMask(int layerIndex)
        {
            int mask = 0;
            if (layerIndex < 0)
            {
                return mask;
            }
            
            for (int i = 0; i < 32; i++)
            {
                if (!Physics.GetIgnoreLayerCollision(layerIndex, i))
                {
                    mask |= 1 << i;
                }
            }
            
            return mask;
        }

        public static void SetCollisionMask(string layerName, int mask) => SetCollisionMask(LayerMask.NameToLayer(layerName), mask);
        
        public static void SetCollisionMask(int layerIndex, int mask)
        {
            if (layerIndex < 0)
            {
                return;
            }

            for (int i = 0; i < 32; i++)
            {
                bool ignoreLayer = !(((mask >> i) & 1) > 0);
                Physics.IgnoreLayerCollision(layerIndex, i, ignoreLayer);
            }
        }
        
        public static string GetLayerName(int index)
        {
            SerializedObject serializedObject = GetTagManagerSerializedObject();
            SerializedProperty layersProp = serializedObject.FindProperty(TM_PROPS_LAYERS);
            return layersProp.GetArrayElementAtIndex(index).stringValue;
        }
        
        public static void SetLayerName(int index, string name)
        {
            SerializedObject serializedObject = GetTagManagerSerializedObject();
            SerializedProperty layersProp = serializedObject.FindProperty(TM_PROPS_LAYERS);
            layersProp.GetArrayElementAtIndex(index).stringValue = name;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
        
        public static void SetLayerNameUndo(int index, string name)
        {
            SerializedObject serializedObject = GetTagManagerSerializedObject();
            SerializedProperty layersProp = serializedObject.FindProperty(TM_PROPS_LAYERS);
            layersProp.GetArrayElementAtIndex(index).stringValue = name;
            serializedObject.ApplyModifiedProperties();
        }

        private static SerializedObject GetTagManagerSerializedObject() => new SerializedObject(AssetDatabase.LoadAssetAtPath<Object>(TM_PATH));
    }
}
