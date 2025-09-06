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
        public string bound;        // data.bounds[...] 문자열 키
        public Sprite sprite;       // 대응 스프라이트
        [TextArea] public string description; // ★ 설명 문자열
    }

    [SerializeField] public List<Entry> entries = new();

    // 키 → 엔트리 전체를 바로 찾게
    Dictionary<string, Entry> _map;

    void OnEnable()
    {
        _map = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in entries)
        {
            var key = Normalize(e.bound);
            if (string.IsNullOrEmpty(key)) continue;
            _map[key] = e;
        }
    }

    static string Normalize(string s) => string.IsNullOrWhiteSpace(s) ? "" : s.Trim();

    // ★ 엔트리 통째로 반환
    public bool TryGet(string bound, out Entry entry)
    {
        if (_map == null) OnEnable();
        if (string.IsNullOrWhiteSpace(bound)) { entry = default; return false; }
        return _map.TryGetValue(Normalize(bound), out entry);
    }

    // 기존 호환: 스프라이트만
    public bool TryGetSprite(string bound, out Sprite sprite)
    {
        if (TryGet(bound, out var e)) { sprite = e.sprite; return true; }
        sprite = null; return false;
    }

    // 필요시: 설명만
    public bool TryGetDescription(string bound, out string desc)
    {
        if (TryGet(bound, out var e)) { desc = e.description; return true; }
        desc = null; return false;
    }
}
