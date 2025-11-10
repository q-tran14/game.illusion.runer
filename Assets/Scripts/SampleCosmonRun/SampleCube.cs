using System.Collections.Generic;
using UnityEngine;

namespace SampleCosmoRun
{
    // Simple integer 3D position used by CosmoRun layout (x,y,z)
    public struct CubePos
    {
        public int x, y, z;
        public CubePos(int x, int y, int z) { this.x = x; this.y = y; this.z = z; }
    }

    public class SampleCube
    {
        public CubePos Pos { get; private set; }
        private readonly List<SampleCubeFace> faces = new();

        public SampleCube(CubePos pos)
        {
            Pos = pos;
        }

        public IReadOnlyList<SampleCubeFace> Faces => faces;

        public SampleCubeFace AddFace(SampleCubeFace.FaceType type, SampleCubeFace.Direction dir, Transform parent = null, Vector3? boundsCenter = null, Vector3? boundsSize = null)
        {
            var face = new SampleCubeFace(this, type, dir, parent, boundsCenter, boundsSize);
            faces.Add(face);
            return face;
        }

        public void RemoveFace(SampleCubeFace face)
        {
            if (faces.Contains(face))
            {
                faces.Remove(face);
                face.Destroy();
            }
        }

        // Convert cube integer position to a 2D/3D world position similar to CosmoRun's layout
        // unit - base size for a cube
        public Vector3 GetWorldPos(float unit)
        {
            float deg30 = 30f * Mathf.Deg2Rad;
            float width = unit * Mathf.Cos(deg30); // ~0.866
            float height = unit * Mathf.Sin(deg30); // 0.5

            float x = Pos.x * width - Pos.y * width;
            float y = Pos.x * height + Pos.y * height - Pos.z * unit;
            return new Vector3(x, y, 0f);
        }

        // Compute the transform.position for a prefab whose mesh bounds have a given center/size.
        // prefabBoundsCenter and prefabBoundsSize are in the prefab's local space (usually from mesh.bounds.center/size).
        // If the prefab's height (boundsSize.y) equals `unit`, then the scale is 1.
        public Vector3 GetTransformPos(float unit, Vector3 prefabBoundsCenter, Vector3 prefabBoundsSize)
        {
            // base logical world position from layout
            Vector3 baseWorld = GetWorldPos(unit);

            // compute uniform vertical scale factor based on prefab bounds height
            float scale = (prefabBoundsSize.y != 0f) ? (unit / prefabBoundsSize.y) : 1f;

            // apply scaled bounds center as an offset so the prefab's transform pivot aligns with the logical cube position
            Vector3 pivotOffset = prefabBoundsCenter * scale;

            return baseWorld - pivotOffset;
        }
    }
}
