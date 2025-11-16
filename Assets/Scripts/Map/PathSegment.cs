using UnityEngine;

public class PathSegment : MonoBehaviour
{
    public enum FaceType { Top, Back, Right }
    public enum TurnDir { Straight, Backward, Left, Right, UpLeft, DownLeft, UpRight, DownRight }

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
        // ✅ Đồng bộ với MapGenerator - Z = forward
        switch (direction)
        {
            case TurnDir.Straight:   return gridPos + new Vector3(0, 0, 1);   // Thẳng
            case TurnDir.Backward:   return gridPos + new Vector3(0, 0, -1);  // Lùi (Z-)
            case TurnDir.Left:       return gridPos + new Vector3(-1, 0, 0);  // Trái
            case TurnDir.Right:      return gridPos + new Vector3(1, 0, 0);   // Phải
            case TurnDir.UpLeft:     return gridPos + new Vector3(-1, 0, 1);  // Trái-Trên
            case TurnDir.DownLeft:   return gridPos + new Vector3(-1, 0, -1); // Trái-Dưới
            case TurnDir.UpRight:    return gridPos + new Vector3(1, 0, 1);   // Phải-Trên
            case TurnDir.DownRight:  return gridPos + new Vector3(1, 0, -1);  // Phải-Dưới
            default:                 return gridPos + new Vector3(0, 0, 1);
        }
    }

    public Vector3 GetWorldPos(float unit)
    {
        // ✅ Chuyển grid position thành world position
        // gridPos.x = left/right, gridPos.z = forward/back, gridPos.y = up/down (không dùng)
        return new Vector3(
            gridPos.x * unit,      // X world = X grid * khoảng cách
            0,                      // Y world = 0 (tất cả cube ở cùng độ cao)
            gridPos.z * unit        // Z world = Z grid * khoảng cách
        );
    }
}
