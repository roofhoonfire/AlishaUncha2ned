// BoundImageConductor.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BoundImageConductor : MonoBehaviour
{
    [Header("DB & Targets")]
    [SerializeField] private BoundSpriteDB database;
    [SerializeField] private Image targetImage; // 비우면 GetComponent<Image>() 사용

    [Header("Animation Watch (선택)")]
    [Tooltip("이미지 오브젝트의 Animator 트리거 이름")]
    [SerializeField] private string triggerName = "example";

    [Tooltip("트리거로 진입하는 상태의 Tag (예: BoundSwap). 이 태그 구간이 끝나면 교체한다.")]
    [SerializeField] private string watchStateTag = "BoundSwap";

    [SerializeField] private int animatorLayer = 0;
    [SerializeField] private float maxWaitSeconds = 3f; // 안전 타임아웃

    // 내부 상태
    private readonly List<string> _bounds = new();
    private int _index = -1;
    private bool _initialized = false;
    private Coroutine _running;

    public bool IsInitialized => _initialized;
    public int CurrentIndex => _index;

    void Awake()
    {
        if (targetImage == null) targetImage = GetComponent<Image>();
    }

    /// <summary>
    /// 1) 외부에서 data.bounds를 그대로 넘겨 초기화.
    /// 2) 내부 index = 0으로 설정.
    /// 3) SO 매핑으로 이미지 즉시 세팅.
    /// 4) bounds[0]의 문자열을 리턴(없으면 null).
    /// </summary>
    public string InitBounds(IList<string> boundsFromData)
    {
        _bounds.Clear();
        if (boundsFromData != null) _bounds.AddRange(boundsFromData);

        _initialized = true;
        _index = 0;

        string key = (_bounds.Count > 0) ? _bounds[0] : null;
        ApplySpriteImmediate(key); // 초기화는 애니 없이 즉시

        return key;
    }

    /// <summary>
    /// 새로운 index가 현재 index와 다르면:
    /// - 애니메이터 트리거 발동 → 태그 상태 종료 시점에 스프라이트 교체 → 내부 index 갱신
    /// 같으면 아무 일도 하지 않음.
    /// </summary>
    public void UpdateByIndex(int newIndex)
    {
        if (!_initialized) { Debug.LogWarning("[BoundImageConductor] Not initialized."); return; }
        if (newIndex < 0 || newIndex >= _bounds.Count) { Debug.LogWarning("[BoundImageConductor] Index out of range."); return; }
        if (newIndex == _index) return; // 조건 2: 동일 인덱스 → 아무 일도 없음

        string key = _bounds[newIndex];
        if (!TryGetSprite(key, out var newSprite))
        {
            Debug.LogWarning($"[BoundImageConductor] No sprite mapped for '{key}'.");
            return;
        }

        var anim = (targetImage != null) ? targetImage.GetComponent<Animator>() : null;
        bool canWatch = anim != null && HasTrigger(anim, triggerName) && !string.IsNullOrEmpty(watchStateTag);

        if (_running != null) StopCoroutine(_running);

        if (canWatch)
            _running = StartCoroutine(CoSwapAfterAnim(anim, newSprite, newIndex));
        else
        {
            // 애니 감시 불가 → 즉시 교체(폴백)
            ApplySpriteImmediate(newSprite);
            _index = newIndex;
        }
    }

    // ------ 내부 유틸 ------

    bool TryGetSprite(string key, out Sprite s)
    {
        s = null;
        if (database == null) return false;
        return database.TryGetSprite(key, out s);
    }

    void ApplySpriteImmediate(string key)
    {
        Debug.Log("그래 이건 문제 없거덩? 뚜뚜");
        if (targetImage == null) return;
        Debug.Log("타겟이미지 널아님 뚜뚜");

        if (string.IsNullOrWhiteSpace(key)) { targetImage.sprite = null; return; }

        Debug.Log($"{key}스트링 널아님 뚜뚜");

        if (TryGetSprite(key, out var sp))
        {
            targetImage.sprite = sp;

            Debug.Log($"{sp} 잘 넣어줌  뚜뚜");

        }


        else targetImage.sprite = null;
    }

    void ApplySpriteImmediate(Sprite sp)
    {
        if (targetImage == null) return;
        targetImage.sprite = sp;
    }

    IEnumerator CoSwapAfterAnim(Animator anim, Sprite newSprite, int newIndex)
    {
        anim.ResetTrigger(triggerName);
        anim.SetTrigger(triggerName);

        int tagHash = Animator.StringToHash(watchStateTag);
        float deadline = Time.time + maxWaitSeconds;

        // 태그 상태로 '진입'할 때까지
        bool entered = false;
        while (Time.time < deadline)
        {
            var st = anim.GetCurrentAnimatorStateInfo(animatorLayer);
            if (st.tagHash == tagHash) { entered = true; break; }
            yield return null;
        }
        if (!entered) { ApplySpriteImmediate(newSprite); _index = newIndex; yield break; }

        // 태그 상태가 '끝날 때까지'
        while (Time.time < deadline)
        {
            var st = anim.GetCurrentAnimatorStateInfo(animatorLayer);

            // (a) 같은 태그 상태에서 거의 끝(>=1) & 전이 아님 → 종료
            if (st.tagHash == tagHash && st.normalizedTime >= 0.999f && !anim.IsInTransition(animatorLayer))
                break;

            // (b) 태그 상태를 벗어났고 전이도 아님 → 종료
            if (st.tagHash != tagHash && !anim.IsInTransition(animatorLayer))
                break;

            yield return null;
        }

        ApplySpriteImmediate(newSprite);
        _index = newIndex;
        _running = null;
    }

    bool HasTrigger(Animator anim, string name)
    {
        foreach (var p in anim.parameters)
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == name)
                return true;
        return false;
    }
}
