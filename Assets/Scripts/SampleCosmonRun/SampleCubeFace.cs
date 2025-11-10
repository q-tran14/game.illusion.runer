using UnityEngine;

namespace SampleCosmoRun
{
    public class SampleCubeFace
    {
        public enum FaceType { Top, Left, Right }
        public enum Direction { LeftUp, LeftDown, RightUp, RightDown, Straight }

        public FaceType Type { get; private set; }
        public Direction Dir { get; private set; }
        public SampleCube Cube { get; private set; }

        private GameObject go;

        private Vector3 prefabBoundsCenter = new Vector3(0f, 1f, 0f);
        private Vector3 prefabBoundsSize = new Vector3(2f, 2f, 2f);

        public SampleCubeFace(SampleCube cube, FaceType type, Direction dir, Transform parent = null, Vector3? boundsCenter = null, Vector3? boundsSize = null)
        {
            Cube = cube;
            Type = type;
            Dir = dir;
            if (boundsCenter.HasValue) prefabBoundsCenter = boundsCenter.Value;
            if (boundsSize.HasValue) prefabBoundsSize = boundsSize.Value;
            CreateVisual(parent);
        }

        void CreateVisual(Transform parent)
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = $"Face_{Type}_{Dir}";
            if (parent != null) go.transform.SetParent(parent, false);
            // default material and orientation
            var mr = go.GetComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            // set small scale, we'll position by Recreate
            go.transform.localScale = Vector3.one * 1f;
            Recreate(1f);
        }

        // Position the face according to cube world position and unit size
        // Recreate positions using `unit` and prefab bounds that were provided at construction
        public void Recreate(float unit)
        {
            if (go == null) return;
            var world = Cube.GetTransformPos(unit, prefabBoundsCenter, prefabBoundsSize);
            // offset slightly based on face type to simulate different faces
            Vector3 offset = Vector3.zero;
            switch (Type)
            {
                case FaceType.Top: offset = new Vector3(0f, 0.2f, 0f); break;
                case FaceType.Left: offset = new Vector3(-0.3f, -0.1f, 0f); break;
                case FaceType.Right: offset = new Vector3(0.3f, -0.1f, 0f); break;
            }

            go.transform.position = world + offset;
            // rotate quad to face camera-ish (flat)
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            // scale relative to unit
            go.transform.localScale = Vector3.one * unit * 0.6f;
        }

        public void Hide()
        {
            if (go != null) go.SetActive(false);
        }

        public void Show()
        {
            if (go != null) go.SetActive(true);
        }

        public void Destroy()
        {
            if (go != null)
            {
                GameObject.Destroy(go);
                go = null;
            }
        }
    }
}
