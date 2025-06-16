using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CardDEffectDatabase
{
    // actionId → 그 카드에 해당하는 효과 리스트
    private static Dictionary<string, List<CardEffect>> effectDataBase = new();

    // 게임 시작 시 한 번만 호출
    public static void LoadEffectFromCSV(TextAsset csv)
    {
        effectDataBase.Clear();
        foreach (var line in csv.text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line)) continue;
            var cols = line.Split(',');
            string cardId = cols[0].Trim();
            HookType hook = Enum.Parse<HookType>(cols[1].Trim());
            EffectType et = Enum.Parse<EffectType>(cols[2].Trim());
            int amt = TryParseInt(cols[3], 0);
            int pla = TryParseInt(cols[4], 0);



            if (!effectDataBase.ContainsKey(cardId))
                effectDataBase[cardId] = new List<CardEffect>();
           effectDataBase[cardId].Add(new CardEffect(hook, et, amt,pla)); //이거 주석 왜 되잇냐 // <==먼소리지 
        }
    }

    public static List<CardEffect> GetEffects(string cardcode)
        => effectDataBase.TryGetValue(cardcode, out var list)
           ? new List<CardEffect>(list)
           : new List<CardEffect>();



    public static int TryParseInt(string value, int defaultValue) //이거 Csvloader.cs에 중복, 나중에 합치던가
    {
        return int.TryParse(value, out int result) ? result : defaultValue;
    }

}

//→ CardDatabase.LoadFromCSV(Resources.Load<TextAsset>("CardEffects")); 한번의 호출이 필요

