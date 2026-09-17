using System;
using UnityEngine;

[Serializable]
public class ExpeditionSpawnEntry
{
    public string specimenId;
    public float weight = 1f;
}

[Serializable]
public class ExpeditionData
{
    public int id;
    public string name;
    public string tagline;
    public float scrollSpeed = 3f;
    public int quota = 6;              // specimens to finish the expedition
    public float hazardInterval = 4f;  // base seconds between hazards
    public string[] hazards;           // "Chopper", "Jet", "Balloon"
    public ExpeditionSpawnEntry[] specimens;
    public string backgroundFar;       // sprite asset names
    public string backgroundMid;
}

[Serializable]
public class ExpeditionList
{
    public ExpeditionData[] expeditions;

    public ExpeditionData Get(int id)
    {
        foreach (var e in expeditions) if (e.id == id) return e;
        return expeditions[0];
    }

    public static ExpeditionList Load(TextAsset jsonAsset)
    {
        return JsonUtility.FromJson<ExpeditionList>(jsonAsset.text);
    }
}
