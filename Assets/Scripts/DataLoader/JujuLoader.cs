using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class JujuLoader
// Start is called before the first frame update

{
    public static Dictionary<string, Juju> jujuDataBase = new(); //제약 빡세게 하고 싶으면 리스트로 하덩가 ㅋ



    public static void LoadJujuFromCSV(TextAsset jujucsv)
    {


        jujuDataBase.Clear();

        foreach(var line in jujucsv.text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line)) continue;

            var cols = line.Split(',');
            if (cols.Length < 11)
            {
                Debug.LogWarning($"[BoundLoader] 잘못된 형식의 줄 발견 (컬럼 개수 부족): {line}");
                continue; // 컬럼 개수 부족 → 스킵
            }
            string jujuId = cols[0].Trim();



            jujuDataBase[jujuId] = new Juju
            {
                type = Enum.Parse<JujuType>(cols[1].Trim()),
                jujuName = cols[2].Trim(), //정의 확인 후 양식 맞추기
                sprite = CardMetaDatabase.LoadSprite(cols[3]),
                addingCard = cols[5].Trim(),
                boundPoint = CardDEffectDatabase.TryParseInt(cols[6], 0),
                onlyOnce = CardDEffectDatabase.TryParseInt(cols[7], 0),
                require = cols[8].Trim(),
                isUsed = CardDEffectDatabase.TryParseInt(cols[9], 0),
                Text = cols[10].Trim(),
                jujuCode = jujuId,
        };  

        }







    }


}



