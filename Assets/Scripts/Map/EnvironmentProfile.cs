using UnityEngine;

[CreateAssetMenu(fileName = "EnvironmentProfile", menuName = "Runner/Environment Profile", order = 11)]
public class EnvironmentProfile : ScriptableObject
{
    [Header("Identity")]
    public string environmentName = "Default";
    
    [Header("Path Cubes")]
    [Tooltip("Danh sách các DecorLibrary chứa path cubes cùng concept/theme cho môi trường này.")]
    public DecorLibrary[] pathCubeLibraries;
    
    [Tooltip("Chế độ chọn cube: Single = dùng 1 library cố định, Random = random library mỗi cube, Mix = trộn tất cả vào 1 pool.")]
    public PathSelectionMode selectionMode = PathSelectionMode.Mix;
    
    [Tooltip("Index của library được chọn khi dùng Single mode.")]
    public int selectedLibraryIndex = 0;
    
    public enum PathSelectionMode
    {
        Single,   // Chỉ dùng 1 library trong list
        Random,   // Mỗi cube random chọn 1 library
        Mix       // Gộp tất cả libraries thành 1 pool lớn rồi random
    }
    
    public int GetTotalPathCubeCount()
    {
        if (pathCubeLibraries == null || pathCubeLibraries.Length == 0) return 0;
        int total = 0;
        foreach (var lib in pathCubeLibraries)
        {
            if (lib != null) total += lib.Count;
        }
        return total;
    }
}
