using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BoundLoader
// Start is called before the first frame update

{
    public static Dictionary<string, Bound> boundDataBase = new(); //제약 빡세게 하고 싶으면 리스트로 하덩가 ㅋ



    public static void LoadBoundsFromCSV(TextAsset boundcsv)
    {


        boundDataBase.Clear();

        foreach(var line in boundcsv.text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line)) continue;

            var cols = line.Split(',');
            if (cols.Length < 7)
            {
                Debug.LogWarning($"[BoundLoader] 잘못된 형식의 줄 발견 (컬럼 개수 부족): {line}");
                continue; // 컬럼 개수 부족 → 스킵
            }
            string boundId = cols[0].Trim();
           


            boundDataBase[boundId] = new Bound
            {
              boundName = cols[1].Trim(),
             sprite = CardMetaDatabase.LoadSprite(cols[2]), //정의 확인 후 양식 맞추기
             text = cols[3].Trim(),
             boundPoint = CardDEffectDatabase.TryParseInt(cols[4], 0),
            amount = CardDEffectDatabase.TryParseInt(cols[5], 0),
            boundType = Enum.Parse<BoundType>(cols[6].Trim()),
        };  

        }







    }


}



