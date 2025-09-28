using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))] // 그래픽 레이캐스트 보장
public class BoundOpIconClickable : MonoBehaviour, IPointerClickHandler
{
    private BoundImageConductor _host;
    private int _index;

    public void Init(BoundImageConductor host, int index)
    {
        _host = host;
        _index = index;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _host?.BeginEditOpGuess(_index); // 로컬 UI 편집 시작
    }
}
