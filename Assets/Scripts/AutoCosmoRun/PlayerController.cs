using System.Collections.Generic;
using UnityEngine;

namespace AutoCosmoRun
{
    public class PlayerController : MonoBehaviour
    {
        public float moveSpeed = 5f;
        public List<CubeTile> pathCubes = new List<CubeTile>();
        private int currentIndex = 0;
        private bool isMoving = false;
        private Vector3 targetPosition;

        void Start()
        {
            if (pathCubes.Count > 0)
            {
                transform.position = pathCubes[0].transform.position;
                SetNextTarget();
            }
        }

        void Update()
        {
            if (isMoving)
            {
                MoveToTarget();
            }
        }

        void SetNextTarget()
        {
            if (currentIndex + 1 < pathCubes.Count)
            {
                currentIndex++;
                targetPosition = pathCubes[currentIndex].transform.position;
                isMoving = true;
            }
            else
            {
                isMoving = false;
            }
        }

        void MoveToTarget()
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                isMoving = false;
                // Optionally, you can add logic to auto-turn based on CubeTile's direction
                SetNextTarget();
            }
        }

        // Call this to set the path from MapGenerator
        public void SetPath(List<CubeTile> cubes)
        {
            pathCubes = cubes;
            currentIndex = 0;
            if (pathCubes.Count > 0)
            {
                transform.position = pathCubes[0].transform.position;
                SetNextTarget();
            }
        }
    }
}
