using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

public static class CardTextFormatter
{
    private static readonly Regex Token = new Regex(@"\{([a-zA-Z_][a-zA-Z0-9_]*)\}",
        RegexOptions.Compiled);

    private static readonly Dictionary<string, FieldInfo> FieldCache = new();

    // 기존
    public static string Resolve(string template, Card card)
        => Resolve(template, card, null, 0);

    // 확장: placeHolderName와 같으면 +delta
    public static string Resolve(string template, Card card, string placeHolderName, int delta)
    {
        if (string.IsNullOrEmpty(template)) return string.Empty;
        if (card == null) return template;

        // CSV 이스케이프 복원
        template = template.Replace("\\n", "\n").Replace("\\t", "\t");

        string result = Token.Replace(template, m =>
        {
            string name = m.Groups[1].Value;

            if (!FieldCache.TryGetValue(name, out var fi))
            {
                fi = typeof(Card).GetField(name, BindingFlags.Instance | BindingFlags.Public);
                FieldCache[name] = fi; // null도 캐시
            }

            if (fi == null)
            {
                Debug.LogWarning($"[CardTextFormatter] Unknown placeholder '{{{name}}}' in cardText (code={card.code})");
                return m.Value; // 알 수 없으면 원문 유지
            }

            object v = fi.GetValue(card);

            // 지정한 플레이스홀더만 +delta 처리
            if (!string.IsNullOrEmpty(placeHolderName) && name == placeHolderName && v != null)
            {
                // int/float/double만 가산 처리
                if (v is int iv) v = iv + delta;
                else if (v is float fv) v = fv + delta;
                else if (v is double dv) v = dv + delta;
                // 다른 타입이면 그냥 원값 그대로
            }

            Debug.Log($"된거야 데미지 추가가 {v}");
            return Convert.ToString(v, CultureInfo.InvariantCulture) ?? "";
        });

        return result;
    }
}
