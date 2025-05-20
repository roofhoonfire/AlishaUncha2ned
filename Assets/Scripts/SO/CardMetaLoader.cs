using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CardMetaLoader : MonoBehaviour
{
 
}




public static class CardMetaDatabase //이름은 데이터 베이스지만 실제 메타 데이터는 SO형태로 CardCSVLoader의 SO 변수에 들어감
{
    public static string csvResourceName = "cardsMeta";


    public static CardSO LoadMetaFromCSV()
    {
        var csvAsset = Resources.Load<TextAsset>(csvResourceName);
        if (csvAsset == null)
        {
            Debug.LogError($"[CardCSVLoader] Resources/{csvResourceName}.csv 를 찾을 수 없습니다.");
            return null;
        }

        var lines = csvAsset.text
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 2)
        {
            Debug.LogError("[CardCSVLoader] CSV에 데이터가 없습니다.");
            return null;
        }

        var cards = lines
            .Skip(1)
            .Select((line, idx) => ParseLine(line, idx + 2))
            .Where(c => c != null)
            .ToArray();

        var so = ScriptableObject.CreateInstance<CardSO>();
        so.cards = cards;
        return so;
    }

    private static Card ParseLine(string line, int lineNumber)
    {
        var cols = line.Split(',');
        if (cols.Length < 12)
        {
            Debug.LogWarning($"[CardCSVLoader] {lineNumber}번째 줄 열 부족: {cols.Length}개");
            return null;
        }

        try
        {
            return new Card
            {
                code = cols[0].Trim(),
                name = cols[1].Trim(),
                sprite = LoadSprite(cols[2]),
                animations = LoadAnimations(cols[3]),

                actionClock = TryParseInt(cols[4], 0),
                rumblePoint = TryParseInt(cols[5], 0),
                defense = TryParseInt(cols[6], 0),
                disappear = TryParseInt(cols[7], 0),
                mana = TryParseInt(cols[8], 0),
                tileType = TryParseInt(cols[9], 0),
                zoneIndex = TryParseInt(cols[10], 0),
                cardText = cols[11].Trim(),
                
            };
        }
        catch (Exception e)
        {
            Debug.LogError($"[CardCSVLoader] {lineNumber}번째 줄 파싱 실패: {e.Message}");
            return null;
        }
    }

    private static Sprite LoadSprite(string spritePath)
    {
        if (string.IsNullOrWhiteSpace(spritePath))
            return null;

        var sprite = Resources.Load<Sprite>(spritePath.Trim());
        if (sprite == null)
            Debug.LogWarning($"[CardCSVLoader] Sprite 로드 실패: Resources/{spritePath}");

        return sprite;
    }

    private static List<AnimationClip> LoadAnimations(string animPathsCsv)
    {
        var list = new List<AnimationClip>();
        if (string.IsNullOrWhiteSpace(animPathsCsv))
            return list;

        var paths = animPathsCsv
            .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim());

        foreach (var path in paths)
        {
            var clip = LoadAnimationClip(path);
            if (clip != null)
                list.Add(clip);
        }

        return list;
    }

    private static AnimationClip LoadAnimationClip(string animPath)
    {
        if (string.IsNullOrWhiteSpace(animPath))
            return null;

        var clip = Resources.Load<AnimationClip>(animPath);
        if (clip == null)
            Debug.LogWarning($"[CardCSVLoader] AnimationClip 로드 실패: Resources/{animPath}");

        return clip;
    }

    private static int TryParseInt(string s, int def) =>
        int.TryParse(s.Trim(), out var v) ? v : def;
}
