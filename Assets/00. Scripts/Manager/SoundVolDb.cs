using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SoundVolDb", menuName = "Sound/Sound Vol Db")]
public sealed class SoundVolDb : ScriptableObject
{
    public const string ResPath = "SoundVolDb";

    [Serializable]
    public sealed class Item
    {
        public int id;
        public string type;
        public string desc;
        public string path;
        [Range(0f, 1f)] public float baseVol = 1f;
        [Range(0f, 1f)] public float vol = 1f;
    }

    [SerializeField] private List<Item> items = new List<Item>();

    private Dictionary<int, Item> map;

    public bool TryGetVol(int id, out float vol)
    {
        vol = 1f;
        BuildMap();

        if (map == null || !map.TryGetValue(id, out Item item) || item == null)
            return false;

        vol = Mathf.Clamp01(item.vol);
        return true;
    }

    public void Sync(IList<SoundTable> src)
    {
        Dictionary<int, Item> oldMap = new Dictionary<int, Item>();
        for (int i = 0; i < items.Count; i++)
        {
            Item item = items[i];
            if (item == null || item.id <= 0 || oldMap.ContainsKey(item.id))
                continue;

            oldMap.Add(item.id, item);
        }

        items.Clear();
        if (src == null)
        {
            map = null;
            return;
        }

        for (int i = 0; i < src.Count; i++)
        {
            SoundTable row = src[i];
            if (row == null || row.ID <= 0)
                continue;

            float nextBaseVol = Mathf.Clamp01(row.soundVolume);
            if (!oldMap.TryGetValue(row.ID, out Item item))
            {
                item = new Item();
                item.vol = nextBaseVol;
            }
            else if (Mathf.Approximately(item.vol, item.baseVol))
            {
                // Unchanged items follow the latest table default.
                item.vol = nextBaseVol;
            }

            item.id = row.ID;
            item.type = row.typeDesc;
            item.desc = row.desc;
            item.path = row.soundPath;
            item.baseVol = nextBaseVol;
            item.vol = Mathf.Clamp01(item.vol);

            items.Add(item);
        }

        items.Sort((a, b) => a.id.CompareTo(b.id));
        map = null;
    }

    private void OnEnable()
    {
        map = null;
    }

    private void OnValidate()
    {
        map = null;
    }

    private void BuildMap()
    {
        if (map != null)
            return;

        map = new Dictionary<int, Item>();
        for (int i = 0; i < items.Count; i++)
        {
            Item item = items[i];
            if (item == null || item.id <= 0 || map.ContainsKey(item.id))
                continue;

            map.Add(item.id, item);
        }
    }
}
