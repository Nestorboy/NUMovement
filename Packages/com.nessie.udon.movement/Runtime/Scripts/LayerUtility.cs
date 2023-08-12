using UnityEngine;

namespace Nessie.Udon.Movement
{
    public static class LayerUtility
    {
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
    }
}
