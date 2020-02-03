using Unity.Mathematics;

namespace CaseyDeCoder.KDCollections
{
    [System.Obsolete("KDBounds is Obsolete. Use Unity.Physics.Aabb instead!")]
    public struct KDBounds
    {
        public float3 min;
        public float3 max;

        public float3 Size => max - min;

        public KDBounds(float3 min, float3 max)
        {
            this.min = min;
            this.max = max;
        }

        public float3 ClosestPoint(float3 point)
        {
            for(int axis = 0; axis < 3; ++axis)
            {
                if(point[axis] < min[axis])
                    point[axis] = min[axis];
                else if(point[axis] > max[axis])
                    point[axis] = max[axis];
            }

            return point;
        }
    }
}

