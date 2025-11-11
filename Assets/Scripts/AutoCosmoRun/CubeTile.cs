using UnityEngine;

public enum FaceType
{
    Top,
    Left,
    Right
}

public enum Direction
{
    Up,
    Down,
    Left,
    Right,
    LeftUp,
    LeftDown,
    RightUp,
    RightDown
}

[System.Serializable]
public struct FaceDesc
{
    public FaceType faceType;
    public Direction direction;
}

public class CubeTile : MonoBehaviour
{
    public FaceDesc faceDesc;

    // Optionally, you can visualize the direction in the editor
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector3 dir = Vector3.zero;
        switch (faceDesc.direction)
        {
            case Direction.Up: dir = Vector3.forward; break;
            case Direction.Down: dir = Vector3.back; break;
            case Direction.Left: dir = Vector3.left; break;
            case Direction.Right: dir = Vector3.right; break;
            case Direction.LeftUp: dir = (Vector3.left + Vector3.forward).normalized; break;
            case Direction.LeftDown: dir = (Vector3.left + Vector3.back).normalized; break;
            case Direction.RightUp: dir = (Vector3.right + Vector3.forward).normalized; break;
            case Direction.RightDown: dir = (Vector3.right + Vector3.back).normalized; break;
        }
        Gizmos.DrawLine(transform.position, transform.position + dir * 0.5f);
    }
}