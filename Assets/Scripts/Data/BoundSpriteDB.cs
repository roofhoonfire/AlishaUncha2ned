// BoundSpriteDB.cs
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Bound Sprite DB", fileName = "DB/BoundSpriteDB")]
public class BoundSpriteDB : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public string bound;   // data.bounds[...] 의 문자열
        public Sprite sprite;  // 대응 이미지
    }

    [SerializeField] public List<Entry> entries = new();

    Dictionary<string, Sprite> _map;

    void OnEnable()
    {
        _map = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in entries)
        {
            var key = Normalize(e.bound);
            if (string.IsNullOrEmpty(key)) continue;
            _map[key] = e.sprite;
        }
    }

    static string Normalize(string s) => string.IsNullOrWhiteSpace(s) ? "" : s.Trim();

    public bool TryGetSprite(string bound, out Sprite sprite)
    {
        sprite = null;
        if (_map == null) OnEnable();
        if (string.IsNullOrWhiteSpace(bound)) return false;
        return _map.TryGetValue(Normalize(bound), out sprite);
    }
}
