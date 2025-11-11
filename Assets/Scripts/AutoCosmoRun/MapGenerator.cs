using System.Collections.Generic;
using UnityEngine;

namespace AutoCosmoRun
{
    public class MapGenerator : MonoBehaviour
    {
        public GameObject cubePrefab;
        public int pathLength = 20;
        public Vector3 startPosition = Vector3.zero;
        public float cubeSpacing = 1.0f;

        private List<GameObject> cubes = new List<GameObject>();
        private FaceDesc lastFaceDesc;
        private Vector3 lastPosition;

        void Start()
        {
            GeneratePath();
        }

        void GeneratePath()
        {
            lastPosition = startPosition;
            lastFaceDesc = new FaceDesc { faceType = FaceType.Top, direction = Direction.Right };

            for (int i = 0; i < pathLength; i++)
            {
                GameObject cube = Instantiate(cubePrefab, lastPosition, Quaternion.identity, transform);
                var tile = cube.GetComponent<CubeTile>();
                if (tile == null) tile = cube.AddComponent<CubeTile>();
                tile.faceDesc = lastFaceDesc;
                cubes.Add(cube);

                // Decide next direction (simple random for demo, can be improved)
                lastFaceDesc = GetNextFaceDesc(lastFaceDesc);
                lastPosition += GetDirectionVector(lastFaceDesc.direction) * cubeSpacing;
            }
        }

        FaceDesc GetNextFaceDesc(FaceDesc current)
        {
            // For demo: randomly pick a direction (not opposite)
            Direction[] possible = new Direction[] { Direction.Right, Direction.Left, Direction.Up, Direction.Down };
            Direction nextDir = possible[Random.Range(0, possible.Length)];
            return new FaceDesc { faceType = FaceType.Top, direction = nextDir };
        }

        Vector3 GetDirectionVector(Direction dir)
        {
            switch (dir)
            {
                case Direction.Up: return Vector3.forward;
                case Direction.Down: return Vector3.back;
                case Direction.Left: return Vector3.left;
                case Direction.Right: return Vector3.right;
                case Direction.LeftUp: return (Vector3.left + Vector3.forward).normalized;
                case Direction.LeftDown: return (Vector3.left + Vector3.back).normalized;
                case Direction.RightUp: return (Vector3.right + Vector3.forward).normalized;
                case Direction.RightDown: return (Vector3.right + Vector3.back).normalized;
                default: return Vector3.forward;
            }
        }
    }
}
