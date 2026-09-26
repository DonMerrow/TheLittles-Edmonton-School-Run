using System.Collections;
using UnityEngine;

/// <summary>
/// Optional supporter purchase for The Littles. The complete game remains free;
/// supporters receive a warm cosmetic glow. RevenueCat owns the transaction,
/// entitlement, and restore flow.
/// </summary>
public sealed class RevenueCatSupporterStore : Purchases.UpdatedCustomerInfoListener
{
    private const string ApiKeyResource = "RevenueCatPublicKey";
    private const string SupporterEntitlement = "supporter";
    private const string SupporterPreference = "thelittles_revenuecat_supporter";

    private Purchases purchases;
    private Purchases.Package supporterPackage;
    private string status = "Connecting to RevenueCat…";
    private bool panelOpen;
    private bool requestInProgress;
    private GUIStyle titleStyle;
    private GUIStyle bodyStyle;
    private GUIStyle buttonStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (FindFirstObjectByType<RevenueCatSupporterStore>() != null) return;
        TextAsset keyAsset = Resources.Load<TextAsset>(ApiKeyResource);
        string publicKey = keyAsset != null ? keyAsset.text.Trim() : string.Empty;
        if (string.IsNullOrWhiteSpace(publicKey))
        {
            Debug.LogWarning("RevenueCat supporter store is disabled: Resources/RevenueCatPublicKey.txt is missing.");
            return;
        }

        GameObject storeObject = new("RevenueCat supporter store");
        DontDestroyOnLoad(storeObject);
        RevenueCatSupporterStore listener = storeObject.AddComponent<RevenueCatSupporterStore>();
        Purchases store = storeObject.AddComponent<Purchases>();
        store.revenueCatAPIKeyGoogle = publicKey;
        store.listener = listener;
        store.productIdentifiers = System.Array.Empty<string>();
        listener.purchases = store;
#endif
    }

    private IEnumerator Start()
    {
        // Purchases configures itself in Start. Waiting one frame guarantees
        // its native wrapper exists before the first RevenueCat request.
        yield return null;
        if (purchases == null) purchases = GetComponent<Purchases>();
        if (purchases == null) yield break;
        purchases.SetDebugLogsEnabled(Debug.isDebugBuild);
        RefreshOfferings();
        if (PlayerPrefs.GetInt(SupporterPreference, 0) == 1) ApplySupporterReward();
    }

    public override void CustomerInfoReceived(Purchases.CustomerInfo customerInfo)
    {
        UpdateSupporterState(customerInfo);
    }

    private void RefreshOfferings()
    {
        status = "Loading the supporter pack…";
        purchases.GetOfferings((offerings, error) =>
        {
            if (error != null)
            {
                status = "Store unavailable. Please try again later.";
                Debug.LogWarning("RevenueCat offerings failed: " + error);
                return;
            }

            supporterPackage = offerings?.Current?.AvailablePackages != null &&
                offerings.Current.AvailablePackages.Count > 0
                ? offerings.Current.AvailablePackages[0]
                : null;
            status = supporterPackage != null
                ? "The full game stays free. This optional pack adds a warm community glow."
                : "No supporter offering is configured yet.";
        });
    }

    private void PurchaseSupporterPack()
    {
        if (requestInProgress || supporterPackage == null) return;
        requestInProgress = true;
        status = "Opening the secure purchase screen…";
        purchases.PurchasePackage(supporterPackage, result =>
        {
            requestInProgress = false;
            if (result == null)
            {
                status = "The purchase did not return a result.";
                return;
            }
            if (result.UserCancelled)
            {
                status = "Purchase cancelled. Nothing was charged.";
                return;
            }
            if (result.Error != null)
            {
                status = "Purchase could not be completed.";
                Debug.LogWarning("RevenueCat purchase failed: " + result.Error);
                return;
            }
            UpdateSupporterState(result.CustomerInfo);
        });
    }

    private void RestoreSupporterPack()
    {
        if (requestInProgress) return;
        requestInProgress = true;
        status = "Restoring purchases…";
        purchases.RestorePurchases((customerInfo, error) =>
        {
            requestInProgress = false;
            if (error != null)
            {
                status = "Restore could not be completed.";
                Debug.LogWarning("RevenueCat restore failed: " + error);
                return;
            }
            UpdateSupporterState(customerInfo);
            if (!IsSupporter(customerInfo)) status = "No supporter purchase was found.";
        });
    }

    private void UpdateSupporterState(Purchases.CustomerInfo customerInfo)
    {
        if (!IsSupporter(customerInfo)) return;
        PlayerPrefs.SetInt(SupporterPreference, 1);
        PlayerPrefs.Save();
        status = "Thank you! Your neighbourhood supporter glow is active.";
        ApplySupporterReward();
    }

    private static bool IsSupporter(Purchases.CustomerInfo customerInfo) =>
        customerInfo?.Entitlements?.Active != null &&
        customerInfo.Entitlements.Active.ContainsKey(SupporterEntitlement);

    private static void ApplySupporterReward()
    {
        DannySpark danny = FindFirstObjectByType<DannySpark>();
        if (danny == null || danny.transform.Find("Supporter community glow") != null) return;
        GameObject glow = new("Supporter community glow");
        glow.transform.SetParent(danny.transform, false);
        glow.transform.localPosition = new Vector3(0f, 1.05f, 0f);
        Light light = glow.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.63f, 0.22f);
        light.range = 5.5f;
        light.intensity = 1.25f;
        light.shadows = LightShadows.None;
    }

    private void OnGUI()
    {
        if (!Application.isMobilePlatform) return;
        float scale = Mathf.Clamp(Screen.height / 720f, 0.78f, 1.5f);
        buttonStyle ??= new GUIStyle(GUI.skin.button)
        {
            fontSize = Mathf.RoundToInt(18f * scale), fontStyle = FontStyle.Bold,
            wordWrap = true
        };
        titleStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(22f * scale), fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white }
        };
        bodyStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(16f * scale), wordWrap = true,
            alignment = TextAnchor.UpperCenter, normal = { textColor = Color.white }
        };

        Rect openButton = new(Screen.width - 215f * scale, 120f * scale, 195f * scale, 52f * scale);
        if (!panelOpen)
        {
            if (GUI.Button(openButton, "SUPPORT THE LITTLES", buttonStyle)) panelOpen = true;
            return;
        }

        float width = Mathf.Min(Screen.width - 32f, 520f * scale);
        float height = Mathf.Min(Screen.height - 32f, 390f * scale);
        Rect panel = new((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
        GUI.Box(panel, GUIContent.none);
        GUI.Label(new Rect(panel.x + 20f, panel.y + 18f, panel.width - 40f, 42f * scale),
            "NEIGHBOURHOOD SUPPORTER PACK", titleStyle);
        GUI.Label(new Rect(panel.x + 28f, panel.y + 72f * scale, panel.width - 56f, 112f * scale),
            status, bodyStyle);

        string price = supporterPackage?.StoreProduct?.PriceString;
        string purchaseLabel = string.IsNullOrEmpty(price) ? "SUPPORT" : "SUPPORT  " + price;
        bool oldEnabled = GUI.enabled;
        GUI.enabled = oldEnabled && !requestInProgress && supporterPackage != null;
        if (GUI.Button(new Rect(panel.x + 30f, panel.yMax - 150f * scale,
                panel.width - 60f, 50f * scale), purchaseLabel, buttonStyle))
            PurchaseSupporterPack();
        GUI.enabled = oldEnabled && !requestInProgress;
        if (GUI.Button(new Rect(panel.x + 30f, panel.yMax - 92f * scale,
                panel.width * 0.5f - 35f, 44f * scale), "RESTORE", buttonStyle))
            RestoreSupporterPack();
        GUI.enabled = oldEnabled;
        if (GUI.Button(new Rect(panel.center.x + 5f, panel.yMax - 92f * scale,
                panel.width * 0.5f - 35f, 44f * scale), "CLOSE", buttonStyle))
            panelOpen = false;
    }
}
