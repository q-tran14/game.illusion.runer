using UnityEngine;

namespace SampleCosmoRun
{
    public static class SampleCubeGroupExtensions
    {
        // Helper to get approximate world bounds of the cube group
        public static Bounds GetWorldBounds(this SampleCubeGroup group)
        {
            var bounds = new Bounds();
            bool first = true;

            foreach (Transform child in group.transform)
            {
                if (first)
                {
                    bounds = new Bounds(child.position, Vector3.one);
                    first = false;
                }
                else
                {
                    bounds.Encapsulate(child.position);
                }
            }
            
            // Add some padding
            bounds.Expand(1f);
            return bounds;
        }

        // Helper to check if a world position is near any cube in the group
        public static bool IsPositionNearCubes(this SampleCubeGroup group, Vector3 worldPos, float threshold = 1f)
        {
            foreach (Transform child in group.transform)
            {
                if (Vector3.Distance(worldPos, child.position) < threshold)
                {
                    return true;
                }
            }
            return false;
        }
    }
}