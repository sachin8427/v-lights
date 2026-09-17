using System;
using UnityEngine;
using UnityEngine.Purchasing;

// Unity IAP (Package Manager: com.unity.purchasing). Three non-consumables.
// Product IDs MUST match App Store Connect exactly.
// Test with a Sandbox Apple ID via TestFlight / Xcode run.
public class IAPManager : MonoBehaviour, IStoreListener
{
    public static IAPManager I { get; private set; }

    public const string RainbowLightsId = "vlights_rainbow_lights"; // $0.99 ship skin
    public const string GoldenBeamId    = "vlights_golden_beam";    // $1.99 wider/faster beam
    public const string UnlockAllId     = "vlights_unlock_all";      // $2.99 all expeditions

    IStoreController _store;
    IExtensionProvider _extensions;

    public event Action<string> OnPurchaseSucceeded;
    public event Action<string> OnPurchaseFailedEvent;

    void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
        Init();
    }

    public bool Owns(string productId) => SaveData.OwnsProduct(productId);

    void Init()
    {
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        builder.AddProduct(RainbowLightsId, ProductType.NonConsumable);
        builder.AddProduct(GoldenBeamId, ProductType.NonConsumable);
        builder.AddProduct(UnlockAllId, ProductType.NonConsumable);
        UnityPurchasing.Initialize(this, builder);
    }

    public void Buy(string productId)
    {
        if (_store == null) { Debug.LogWarning("IAP not initialized yet"); return; }
        _store.InitiatePurchase(productId);
    }

    // Call from a "Restore Purchases" button (required by App Store review)
    public void Restore()
    {
        if (_extensions == null) return;
        _extensions.GetExtension<IAppleExtensions>().RestoreTransactions(OnRestored);
    }
    void OnRestored(bool ok) => Debug.Log("IAP restore: " + ok);

    // --- IStoreListener ---
    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        _store = controller; _extensions = extensions;
        Debug.Log("IAP initialized");
    }
    public void OnInitializeFailed(InitializationFailureReason error) =>
        Debug.LogError("IAP init failed: " + error);
    public void OnInitializeFailed(InitializationFailureReason error, string message) =>
        Debug.LogError($"IAP init failed: {error} — {message}");

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        string id = args.purchasedProduct.definition.id;
        SaveData.SetOwnsProduct(id);
        OnPurchaseSucceeded?.Invoke(id);
        Debug.Log("IAP purchased: " + id);
        return PurchaseProcessingResult.Complete;
    }
    public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
    {
        OnPurchaseFailedEvent?.Invoke(product.definition.id);
        Debug.LogWarning($"IAP failed {product.definition.id}: {reason}");
    }
}
