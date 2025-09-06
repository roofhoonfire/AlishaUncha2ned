using UnityEngine;
using UnityEngine.EventSystems;

public class TooltipTriggerUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [TextArea] public string description;
    [SerializeField] private float showDelay = 0f; // 필요시 지연
    private bool _hovering;
    private float _hoverStart;

    public void OnPointerEnter(PointerEventData eventData)
    {
        _hovering = true;
        _hoverStart = Time.unscaledTime;
        if (showDelay <= 0f)
            TooltipSystem.Instance?.Show(description, eventData.position);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (!_hovering) return;

        if (showDelay > 0f)
        {
            if (Time.unscaledTime - _hoverStart >= showDelay &&
                TooltipSystem.Instance != null)
            {
                // 지연 후 최초 표시
                if (!IsShowing())
                    TooltipSystem.Instance.Show(description, eventData.position);
            }
        }

        TooltipSystem.Instance?.Move(eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hovering = false;
        TooltipSystem.Instance?.Hide();
    }

    bool IsShowing()
    {
        // 간단 체크: 시스템 내부에서 인스턴스 활성 여부를 노출해도 되고, 여기선 null 체크로 대충
        return true; // 필요하면 시스템에 bool 프로퍼티 추가해서 정확히 판정
    }

    void OnDisable()
    {
        if (_hovering)
        {
            _hovering = false;
            TooltipSystem.Instance?.Hide();
        }
    }
}
