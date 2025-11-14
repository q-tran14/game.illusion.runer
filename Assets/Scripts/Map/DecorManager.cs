using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Threading.Tasks;

public class DecorManager : Singleton<DecorManager>
{
    [Header("Player Decor")]
    [SerializeField] private bool usePlayerDecor = false;
    [SerializeField] private DecorLibrary playerDecorLibrary;
    [SerializeField] private int playerDecorIndex = 0;

    [Header("Coin Decor")]
    [SerializeField] private bool useCoinDecor = false;
    [SerializeField] private DecorLibrary coinDecorLibrary;
    [SerializeField] private DecorMode coinDecorMode = DecorMode.Random;
    [SerializeField] private int coinDecorIndex = 0;
    private int coinDecorSequenceIndex = 0;

    public enum DecorMode { None, Single, Random, Sequence }

    // API: Player Decor
    public void SetPlayerDecorLibrary(DecorLibrary library)
    {
        playerDecorLibrary = library;
    }

    public void SetPlayerDecorIndex(int index)
    {
        playerDecorIndex = index;
    }

    public void EnablePlayerDecor(bool enable)
    {
        usePlayerDecor = enable;
    }

    // Get player decor AssetReference (sync - just returns reference)
    public AssetReferenceGameObject GetPlayerDecorAssetRef()
    {
        if (!usePlayerDecor || playerDecorLibrary == null || playerDecorLibrary.Count == 0) 
            return null;
        
        int idx = Mathf.Clamp(playerDecorIndex, 0, playerDecorLibrary.Count - 1);
        return playerDecorLibrary.items[idx];
    }

    // API: Coin Decor
    public void SetCoinDecorLibrary(DecorLibrary library)
    {
        coinDecorLibrary = library;
        coinDecorSequenceIndex = 0;
    }

    public void SetCoinDecorMode(DecorMode mode)
    {
        coinDecorMode = mode;
        coinDecorSequenceIndex = 0;
    }

    public void SetCoinDecorIndex(int index)
    {
        coinDecorIndex = index;
    }

    public void EnableCoinDecor(bool enable)
    {
        useCoinDecor = enable;
    }

    // Get coin decor AssetReference (sync - just returns reference)
    public AssetReferenceGameObject GetCoinDecorAssetRef()
    {
        if (!useCoinDecor || coinDecorLibrary == null || coinDecorLibrary.Count == 0) 
            return null;

        switch (coinDecorMode)
        {
            case DecorMode.Single:
                int idx = Mathf.Clamp(coinDecorIndex, 0, coinDecorLibrary.Count - 1);
                return coinDecorLibrary.items[idx];
            case DecorMode.Random:
                int randomIdx = Random.Range(0, coinDecorLibrary.Count);
                return coinDecorLibrary.items[randomIdx];
            case DecorMode.Sequence:
                if (coinDecorSequenceIndex >= coinDecorLibrary.Count) coinDecorSequenceIndex = 0;
                return coinDecorLibrary.items[coinDecorSequenceIndex++];
            default:
                return null;
        }
    }
}
