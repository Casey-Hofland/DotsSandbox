using Unity.Mathematics;

namespace CaseyDeCoder.KDCollections
{
    public struct KDQueryNode
    {
        public KDNode node;
        public float3 tempClosestPoint;

        public void Set(KDNode node, float3 tempClosestPoint)
        {
            this.node = node;
            this.tempClosestPoint = tempClosestPoint;
        }
    }
}
