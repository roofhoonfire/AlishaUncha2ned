using UnityEngine;

public class CardApplyDeferrer : MonoBehaviour
{
    private EachCardInfo _info;
    private Animator _anim;
    private string _idleStateName = "Card_Actioin_Idle";
    private int _layerIndex = 0;

    // 옵션: Init을 실제로 거쳤는지 확인
    private string _requireInitStateName = null;
    private bool _sawInit = false;
    private float _bindTime;

    public void Bind(
        EachCardInfo info,
        Animator anim,
        string idleStateName = "Card_Actioin_Idle",
        int layerIndex = 0,
        string requireInitStateName = null)
    {
        _info = info;
        _anim = anim;
        _idleStateName = idleStateName;
        _layerIndex = layerIndex;
        _requireInitStateName = requireInitStateName;
        _bindTime = Time.time;

        TryApplyIfReady(immediate: true);
    }

    private bool _applied;

    private void Update()
    {
        if (_applied || _anim == null) return;

        // Init을 실제로 봤는지(현재/다음 스테이트 판별)
        if (!string.IsNullOrEmpty(_requireInitStateName) && !_sawInit)
        {
            var cur = _anim.GetCurrentAnimatorStateInfo(_layerIndex);
            var next = _anim.IsInTransition(_layerIndex)
                ? _anim.GetNextAnimatorStateInfo(_layerIndex)
                : default;

            if (cur.IsName(_requireInitStateName) || next.IsName(_requireInitStateName))
                _sawInit = true;

            // 혹시 바인드가 너무 늦어 Init을 못 봤다면(예: 0.5초 경과) 안전 해제
            if (!_sawInit && Time.time - _bindTime > 0.5f)
                _sawInit = true;
        }

        TryApplyIfReady(immediate: false);
    }

    private void TryApplyIfReady(bool immediate)
    {
        if (_anim.IsInTransition(_layerIndex)) return;

        var st = _anim.GetCurrentAnimatorStateInfo(_layerIndex);
        if (st.IsName(_idleStateName))
        {
            if (string.IsNullOrEmpty(_requireInitStateName) || _sawInit)
                ApplyNow();
        }
        else if (immediate && _info != null && _anim == null)
        {
            ApplyNow();
        }
    }

    private void ApplyNow()
    {
        if (_applied) return;
        _applied = true;
        _info?.ApplyCardData();
        Destroy(this);
    }
}
