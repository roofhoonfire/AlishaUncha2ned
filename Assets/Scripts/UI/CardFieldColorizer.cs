using UnityEngine;

// 사용: tmp.text = CardFieldColorizer.GetColoredValue(cardCode, "defense", 12);
public static class CardFieldColorizer
{
    // 취향대로 바꿔도 됨
    private const string BLUE = "#FFE880";
    private const string RED = "#FF5A5A";
    private const string NEUTRAL = "#FFFFFF";

    // fieldname: "actionClock" | "defense" | "damage" | "rumblePoint"
    public static string GetColoredValue(string cardcode, string fieldname, int val)
    {
        if (string.IsNullOrEmpty(cardcode) || string.IsNullOrEmpty(fieldname))
            return val.ToString();

        var card = CardCSVLoader.Instance?.GetCardByCode(cardcode);
        if (card == null) return val.ToString();

        // 카드 원본 값 가져오기
        int baseVal;
        switch (fieldname)
        {
            case "actionClock": baseVal = card.actionClock; break;
            case "defense": baseVal = card.defense; break;
            case "damage": baseVal = card.damage; break;
            case "rumblePoint": baseVal = card.rumblePoint; break;
            default: return val.ToString(); // 허용 외 필드명
        }

        // 색 결정
        bool toBlue;
        if (fieldname == "actionClock")
        {
            // 읽어온 데이터(원본)가 val보다 크면 파랑, 작으면 빨강
            toBlue = baseVal > val;
        }
        else
        {
            // defense/damage/rumblePoint: 원본이 val보다 작으면 파랑, 크면 빨강
            toBlue = baseVal < val;
        }

        string color = (baseVal == val) ? NEUTRAL : (toBlue ? BLUE : RED);
        return $"<color={color}>{val}</color>";
    }
}
