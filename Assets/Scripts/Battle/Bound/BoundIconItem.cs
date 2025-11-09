using UnityEngine;
using System;            // ← 이거 추가

using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class BoundIconItem : MonoBehaviour
{
    [Header("Refs (비워두면 자동 할당)")]
    public Image iconImage;                 // 루트 Image 추천
    public TooltipTriggerUI tooltip;        // 루트 TooltipTriggerUI

    void Awake()
    {
        if (!iconImage) iconImage = GetComponent<Image>();
        // label은 자식에 없을 수 있음 -> 필수 아님
    }

    public void Set(Sprite sp, string desc)
    {
        if (iconImage) iconImage.sprite = sp;
        if (tooltip) tooltip.description = desc ?? string.Empty;
    }

   
}
