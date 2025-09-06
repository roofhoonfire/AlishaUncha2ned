using UnityEngine;
using UnityEngine.EventSystems;

public class SelectedListItem : MonoBehaviour, IPointerClickHandler
{
    private myDeckList _owner;
    private string _name;
    private string _code;

    public void Setup(myDeckList owner, string name, string code)
    {
        _owner = owner;
        _name = name;
        _code = code;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_owner == null || string.IsNullOrEmpty(_code)) return;
        bool ok = _owner.RemoveOne(_code);
        if (!ok)
            Debug.LogWarning($"[SelectedListItem] 제거 실패: '{_name}' ({_code})");
    }
}
