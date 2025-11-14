# Hướng Dẫn Setup Addressables

## 1. Cài Đặt Addressables Package

1. Mở **Window → Package Manager**
2. Chọn **Unity Registry** (góc trên bên trái)
3. Tìm **Addressables** trong danh sách
4. Click **Install**

## 2. Khởi Tạo Addressables

1. Mở **Window → Asset Management → Addressables → Groups**
2. Nếu chưa có, click **Create Addressables Settings**
3. Một cửa sổ **Addressables Groups** sẽ xuất hiện

## 3. Đánh Dấu Prefabs Làm Addressable

### Đối Với Path Cube Decor Models:

1. Chọn tất cả prefab models trong thư mục của bạn (ví dụ: `/Assets/Models/PathCubes/`)
2. Trong Inspector, tìm **Addressable** checkbox (ngay dưới tên file)
3. ✅ Check **Addressable**
4. Đặt tên **Address** (key để load):
   - Có thể để tên mặc định (tên file)
   - Hoặc đặt tên có ý nghĩa: `PathCube_City_01`, `PathCube_Forest_02`, v.v.

### Đối Với Player Decor Models:

1. Chọn prefab player models trong `/Assets/Models/Players/`
2. Check **Addressable**
3. Đặt Address: `Player_Default`, `Player_Robot`, v.v.

### Đối Với Coin Decor Models:

1. Chọn prefab coin models trong `/Assets/Models/Coins/`
2. Check **Addressable**
3. Đặt Address: `Coin_Gold`, `Coin_Diamond`, v.v.

## 4. Tổ Chức Groups (Khuyến Nghị)

Trong cửa sổ **Addressables Groups**, tạo các group riêng để dễ quản lý:

1. Click **Create → Group → Packed Assets**
2. Đặt tên group:
   - `PathCubeDecors` - cho path cube models
   - `PlayerDecors` - cho player models
   - `CoinDecors` - cho coin models

3. Kéo các assets vào group tương ứng:
   - Chọn assets trong Project → kéo vào group trong Addressables Groups window

## 5. Cấu Hình DecorLibrary

### Tạo DecorLibrary Mới:

1. Right-click trong Project → **Create → Runner → Decor Library**
2. Đặt tên (ví dụ: `CityPathCubes`, `PlayerModels`, `CoinModels`)
3. Chọn DecorLibrary trong Inspector

### Thêm AssetReferences:

1. Trong Inspector, tìm **Items** (List)
2. Tăng **Size** lên số lượng models bạn muốn
3. Với mỗi element:
   - Click vào ô **None (Asset Reference Game Object)**
   - Trong popup, chọn prefab từ danh sách Addressable assets
   - Hoặc kéo thả prefab từ Project vào ô

**Lưu ý:** Chỉ các assets đã được đánh dấu Addressable mới xuất hiện trong danh sách này.

## 6. Cấu Hình EnvironmentProfile

1. Mở các **EnvironmentProfile** ScriptableObjects đã có (hoặc tạo mới)
2. Trong **Path Cube Libraries** array:
   - Gán các **DecorLibrary** đã tạo ở bước 5
   - Ví dụ: Environment "City" → gán `CityPathCubes` library

## 7. Cấu Hình DecorManager

1. Tìm **DecorManager** trong scene (hoặc prefab)
2. Trong Inspector:
   - **Player Decor Library**: Gán DecorLibrary chứa player models
   - **Coin Decor Library**: Gán DecorLibrary chứa coin models
   - Check **Use Player Decor** / **Use Coin Decor** để enable

## 8. Sử Dụng Async Loading Trong Code

### Trong MapGenerator:

```csharp
// Thay vì:
var cube = ObjectPool.Instance.Get(); // sync

// Dùng:
var cube = await ObjectPool.Instance.GetAsync(); // async
```

### SpawnNextCube() Cần Async:

Tìm method `SpawnNextCube()` và thêm `async Task`:

```csharp
private async Task SpawnNextCube()
{
    var prefabObj = await ObjectPool.Instance.GetAsync(); // Async
    if (prefabObj == null)
    {
        Debug.LogWarning("[MapGenerator] Pool exhausted.");
        return;
    }
    // ... rest of code
}
```

### Update() Loop:

```csharp
private async void Update()
{
    float dist = Vector3.Distance(player.position, activeCubes[^1].transform.position);
    if (dist < distPlayerAndLastCube) 
    {
        await SpawnNextCube(); // Async call
    }
    // ... rest
}
```

## 9. Build Settings (Quan Trọng!)

### Trước Khi Build Game:

1. Mở **Window → Asset Management → Addressables → Groups**
2. Click **Build → New Build → Default Build Script**
3. Đợi build hoàn tất (sẽ tạo folder `ServerData` và catalog files)

**Lưu ý:** Phải build Addressables mỗi khi thay đổi assets trước khi build game.

## 10. Test Trong Editor

1. Press Play
2. Kiểm tra Console:
   - Không có lỗi "Asset not found"
   - Models load correctly
3. Nếu thấy lỗi:
   - Kiểm tra asset đã được mark Addressable chưa
   - Kiểm tra Address key đúng chưa
   - Rebuild Addressables (bước 9)

## 11. Giải Phóng Memory (Tùy Chọn)

Nếu muốn tối ưu memory hơn nữa, có thể release assets khi không dùng:

```csharp
// Trong DecorLibrary
public void ReleaseAll()
{
    foreach (var assetRef in items)
    {
        if (assetRef != null && assetRef.IsValid())
        {
            assetRef.ReleaseAsset();
        }
    }
}
```

Gọi khi switch environment hoặc clear map:

```csharp
// Trong MapGenerator.ClearMap()
activeEnvironment?.pathCubeLibraries?.ForEach(lib => lib?.ReleaseAll());
```

## Lợi Ích Đạt Được

✅ **Load Time Nhanh Hơn**: Chỉ load assets khi cần, không load hết lúc khởi động  
✅ **Memory Thấp Hơn**: Tự động unload assets không dùng  
✅ **No Lag Spikes**: Async loading không block main thread  
✅ **Dễ Scale**: Thêm models mới chỉ cần mark Addressable, không cần sửa code  
✅ **Remote Content**: Sau này có thể tải assets từ server (DLC, updates)  

## Troubleshooting

### "Asset failed to load"
→ Kiểm tra asset đã mark Addressable chưa, rebuild Addressables

### "RuntimeKeyIsValid() returns false"
→ AssetReference trong Inspector bị mất link, reassign lại

### Models không hiện
→ Kiểm tra `usePlayerDecor` / `useCoinDecor` flags trong DecorManager

### Build game bị thiếu assets
→ Phải build Addressables trước khi build game (bước 9)
