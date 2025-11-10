using System.Collections.Generic;
using UnityEngine;

namespace SampleCosmoRun
{
    // A lightweight CubeGroup that manages a chain of faces (tail = current play path)
    public class SampleCubeGroup : MonoBehaviour
    {
        [SerializeField] private float unit = 2f;
        [Header("Prefab bounds (mesh) - used to align transforms to cube logical pos")]
        [Tooltip("The mesh bounds center in the prefab local space (example: (0,1,0) for a 2x2x2 cube with pivot at bottom)")]
        public Vector3 PrefabBoundsCenter = new Vector3(0f, 1f, 0f);
        [Tooltip("The mesh bounds size in the prefab local space (example: (2,2,2) for a 2x2x2 cube)")]
        public Vector3 PrefabBoundsSize = new Vector3(2f, 2f, 2f);
        private readonly List<SampleCube> cubes = new();

        private SampleCubeFace tailFace;

        // Public API to get number of cubes and last cube position
        public int CubeCount => cubes.Count;

        public Vector3? GetLastCubePosition()
        {
            if (cubes.Count == 0) return null;
            var lastCube = cubes[cubes.Count - 1];
            return lastCube.GetTransformPos(unit, PrefabBoundsCenter, PrefabBoundsSize);
        }

        public void InitCubes(int length)
        {
            Clear();
            // create a starting column at (0,0,0)
            var startPos = new CubePos(0, 0, 0);
            var startCube = CreateCube(startPos);
            startCube.AddFace(SampleCubeFace.FaceType.Top, SampleCubeFace.Direction.Straight, transform, PrefabBoundsCenter, PrefabBoundsSize);
            cubes.Add(startCube);

            for (int i = 1; i < length; i++)
            {
                AddRandomFace();
            }
        }

        public SampleCube CreateCube(CubePos pos)
        {
            var cube = new SampleCube(pos);
            return cube;
        }

        // Append a new face at a neighbor position (very simple random neighbor)
        public void AddRandomFace()
        {
            var last = cubes.Count > 0 ? cubes[cubes.Count - 1] : CreateCube(new CubePos(0, 0, 0));
            // pick one of six neighbor offsets similar to CosmoRun
            var choices = new List<CubePos>
            {
                new CubePos(last.Pos.x + 1, last.Pos.y, last.Pos.z),
                new CubePos(last.Pos.x - 1, last.Pos.y, last.Pos.z),
                new CubePos(last.Pos.x, last.Pos.y + 1, last.Pos.z),
                new CubePos(last.Pos.x, last.Pos.y - 1, last.Pos.z),
                new CubePos(last.Pos.x + 1, last.Pos.y + 1, last.Pos.z),
                new CubePos(last.Pos.x - 1, last.Pos.y - 1, last.Pos.z)
            };

            var pick = choices[Random.Range(0, choices.Count)];
            var cube = CreateCube(pick);
            // add a top face for visual path, pass prefab bounds so transform aligns correctly
            cube.AddFace(SampleCubeFace.FaceType.Top, SampleCubeFace.Direction.Straight, transform, PrefabBoundsCenter, PrefabBoundsSize);
            cubes.Add(cube);
        }

        public void RemoveTailFace()
        {
            if (cubes.Count == 0) return;
            var first = cubes[0];
            // remove first cube
            foreach (var f in first.Faces)
            {
                f.Hide();
            }
            cubes.RemoveAt(0);
        }

        public void Clear()
        {
            foreach (var c in cubes)
            {
                foreach (var f in c.Faces)
                {
                    f.Destroy();
                }
            }
            cubes.Clear();
        }

        // Simple editor helper to rebuild visuals
        public void RecreateAll()
        {
            foreach (var c in cubes)
            {
                foreach (var f in c.Faces)
                {
                    f.Recreate(unit);
                }
            }
        }
    }
}
