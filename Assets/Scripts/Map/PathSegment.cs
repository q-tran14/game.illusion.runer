using System.Collections.Generic;
using UnityEngine;
public enum FaceType { Top, Back, Right }
public enum TurnDir { Straight, Backward, Left, Right, Up, Down }

public class PathSegment : MonoBehaviour
{
    public Dictionary<TurnDir, Vector3> dirDic = new Dictionary<TurnDir, Vector3>(){
        { TurnDir.Straight, Vector3.forward },
        { TurnDir.Backward, Vector3.back },
        { TurnDir.Left, Vector3.left },
        { TurnDir.Right, Vector3.right },
        { TurnDir.Up, Vector3.up },
        { TurnDir.Down, Vector3.down }
    };

    public Vector3 gridPos;                         // Tọa độ lưới logic (x, y, z)
    public TurnDir direction;                       // Hướng player đang đi
    public FaceType faceType = FaceType.Top;        // Hướng bề mặt: Mặt phẳng hướng lên trên
    public PathSegment next;                        // Plane tiếp theo
    public PathSegment previous;                    // Plane trước đó
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
            case TurnDir.Straight: return gridPos + new Vector3(0, 0, 1);   // Thẳng
            case TurnDir.Backward: return gridPos + new Vector3(0, 0, -1);  // Lùi (Z-)
            case TurnDir.Left: return gridPos + new Vector3(-1, 0, 0);  // Trái
            case TurnDir.Right: return gridPos + new Vector3(1, 0, 0);   // Phải
            case TurnDir.Up: return gridPos + new Vector3(0, 1, 0);      // Lên trên
            case TurnDir.Down: return gridPos + new Vector3(0, -1, 0);    // Xuống dưới
            default: return gridPos + new Vector3(0, 0, 1);
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

    public Vector3 GetDirectionVector(TurnDir direct)
    {
        if (dirDic.ContainsKey(direct)) return dirDic[direct];
        return Vector3.forward; // Mặc định về phía trước
    }
}
