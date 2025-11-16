using UnityEngine;

public class PathSegment : MonoBehaviour
{
    public enum FaceType { Top, Back, Right }
    public enum TurnDir { Straight, Backward, Left, Right, Up, Down }

    public Vector3 gridPos;                         // Tọa độ lưới logic (x, y, z)
    public TurnDir direction;                       // Hướng player đang đi
    public FaceType faceType = FaceType.Top;        // Hướng bề mặt: Mặt phẳng hướng lên trên
    public PathSegment next;                        // Plane tiếp theo
    public PathSegment endPathCorner;               // Plane rẽ sang hướng mới
    public void Init(Vector3 grid, TurnDir dir, FaceType face)
    {
        gridPos = grid;
        direction = dir;
        faceType = face;
        ChangeFace(face);
    }
    
    public void ChangeFace(FaceType face)
    {
        switch (face)
        {
            case FaceType.Back:
                transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
                break;
            case FaceType.Right:
                transform.rotation = Quaternion.Euler(-90f, -90f, 0f);
                break;
            default:
                transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                break;
        }
    }

    public Vector3 GetNextGridPos()
    {
        switch (direction)
        {
            case TurnDir.Straight:   return gridPos + new Vector3(0, 0, 1);   // Z+
            case TurnDir.Backward:   return gridPos + new Vector3(0, 0, -1);  // Z-
            case TurnDir.Left:       return gridPos + new Vector3(-1, 0, 0);  // X-
            case TurnDir.Right:      return gridPos + new Vector3(1, 0, 0);   // X+
            case TurnDir.Up:         return gridPos + new Vector3(0, 1, 0);   // Y+
            case TurnDir.Down:       return gridPos + new Vector3(0, -1, 0);  // Y-
            default:                 return gridPos + new Vector3(0, 0, 1);
        }
    }

    public Vector3 GetWorldPos(float unitX, float unitY, float unitZ)
    {
        // Chuyển grid position thành world position (3D)
        return new Vector3(
            gridPos.x * unitX,
            gridPos.y * unitY,
            gridPos.z * unitZ
        );
    }
}
