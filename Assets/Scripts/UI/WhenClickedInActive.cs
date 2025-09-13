using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class WhenClickedInActive : MonoBehaviour
{
    [Header("대상 영역(비워두면 자기 RectTransform)")]
    [SerializeField] private RectTransform targetRect;

    [Header("같이 ‘안쪽’으로 취급할 추가 영역들(옵션)")]
    [SerializeField] private List<RectTransform> additionalInsideRects = new();

    [Header("ESC로 닫기 허용")]
    [SerializeField] private bool closeOnEscape = true;

    private Canvas _parentCanvas;
    private Camera _eventCam;

    private void Awake()
    {
        if (!targetRect) targetRect = transform as RectTransform;

        _parentCanvas = GetComponentInParent<Canvas>();
        if (_parentCanvas != null)
        {
            // Screen Space - Overlay면 카메라 필요 없음(null)
            _eventCam = (_parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                ? null
                : _parentCanvas.worldCamera;
        }
    }

    private void Update()
    {
        // 마우스 / 터치 시작 시점만 체크
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
        {
            Vector2 pos = Input.mousePosition;
            TryCloseIfOutside(pos);
        }

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Vector2 pos = Input.GetTouch(0).position;
            TryCloseIfOutside(pos);
        }

        if (closeOnEscape && Input.GetKeyDown(KeyCode.Escape))
        {
            gameObject.SetActive(false);
        }
    }

    private void TryCloseIfOutside(Vector2 screenPos)
    {
        if (IsInsideAnyRect(screenPos)) return; // 안쪽이면 유지
        gameObject.SetActive(false);            // 바깥이면 닫기!
    }

    private bool IsInsideAnyRect(Vector2 screenPos)
    {
        if (targetRect &&
            RectTransformUtility.RectangleContainsScreenPoint(targetRect, screenPos, _eventCam))
            return true;

        if (additionalInsideRects != null)
        {
            foreach (var r in additionalInsideRects)
            {
                if (!r) continue;
                if (RectTransformUtility.RectangleContainsScreenPoint(r, screenPos, _eventCam))
                    return true;
            }
        }
        return false;
    }
}
