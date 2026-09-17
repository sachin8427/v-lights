using UnityEngine;

// All persistence lives here (PlayerPrefs). IAP entitlements are
// ALSO cached here after a successful purchase / restore.
public static class SaveData
{
    public static bool IsCollected(string specimenId) =>
        PlayerPrefs.GetInt("spec_" + specimenId, 0) == 1;

    public static void SetCollected(string specimenId)
    {
        PlayerPrefs.SetInt("spec_" + specimenId, 1);
        PlayerPrefs.Save();
    }

    public static int CollectedCount(SpecimenData[] all)
    {
        int n = 0;
        foreach (var s in all) if (IsCollected(s.id)) n++;
        return n;
    }

    // Highest expedition index unlocked (1-based)
    public static int UnlockedExpeditions
    {
        get => PlayerPrefs.GetInt("exp_unlocked", 1);
        set { PlayerPrefs.SetInt("exp_unlocked", value); PlayerPrefs.Save(); }
    }

    public static bool OwnsProduct(string productId) =>
        PlayerPrefs.GetInt("iap_" + productId, 0) == 1;

    public static void SetOwnsProduct(string productId)
    {
        PlayerPrefs.SetInt("iap_" + productId, 1);
        PlayerPrefs.Save();
    }

    public static int HighScore
    {
        get => PlayerPrefs.GetInt("highscore", 0);
        set { PlayerPrefs.SetInt("highscore", value); PlayerPrefs.Save(); }
    }
}
