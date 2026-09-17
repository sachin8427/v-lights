using System;
using UnityEngine;

// One entry from specimen_catalog.json
[Serializable]
public class SpecimenData
{
    public string id;
    public string name;
    public int expedition;
    public string rarity;   // Common, Uncommon, Rare, Legendary
    public int points;
    public float beamTime;  // seconds of beam contact to capture (risk/reward knob)
    public string sprite;   // sprite asset name, e.g. "specimen_tacotruck"
    public string flavor;   // Field Guide flavor text
}

[Serializable]
public class SpecimenCatalog
{
    public SpecimenData[] specimens;

    public SpecimenData Get(string id)
    {
        foreach (var s in specimens) if (s.id == id) return s;
        return null;
    }

    public static SpecimenCatalog Load(TextAsset jsonAsset)
    {
        return JsonUtility.FromJson<SpecimenCatalog>(jsonAsset.text);
    }
}
