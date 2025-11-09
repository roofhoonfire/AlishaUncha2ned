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

        template = template.Replace("\\n", "\n").Replace("\\t", "\t");

        string result = Token.Replace(template, m =>
        {
            string name = m.Groups[1].Value;

            if (!FieldCache.TryGetValue(name, out var fi))
            {
                fi = typeof(Card).GetField(name, BindingFlags.Instance | BindingFlags.Public);
                FieldCache[name] = fi; // null도 캐시
            }
            if (fi == null) return m.Value;

            object v = fi.GetValue(card);

            bool isTarget = !string.IsNullOrEmpty(placeHolderName) && name == placeHolderName && v != null;

            if (isTarget)
            {
                // int/float/double만 가산
                if (v is int iv)
                {
                    int newVal = iv + delta;

                    // ★ 여기: damage만 색 입힌 문자열로 치환해서 곧바로 반환
                    if (name == "damage")
                        return CardFieldColorizer.GetColoredValue(card.code, "damage", newVal);

                    v = newVal; // damage 외엔 그냥 숫자 업데이트
                }
                else if (v is float fv) v = fv + delta;
                else if (v is double dv) v = dv + delta;
            }

            return Convert.ToString(v, CultureInfo.InvariantCulture) ?? "";
        });

        return result;
    }
}
