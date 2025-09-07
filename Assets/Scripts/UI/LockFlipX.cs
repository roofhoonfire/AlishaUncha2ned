using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class LockFlipX : MonoBehaviour
{
    [Tooltip("flipX를 이 값으로 고정합니다.")]
    public bool lockedValue = false;

    [Tooltip("true면 잠금 해제(테스트용). 보통은 false로 둠.")]
    public bool allowChange = false;

    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (!allowChange) sr.flipX = lockedValue;
    }

    void LateUpdate()
    {
        // 매 프레임 끝에서 강제 복구: 외부 코드가 바꿔도 즉시 되돌림
        if (!allowChange && sr.flipX != lockedValue)
            sr.flipX = lockedValue;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // 에디터에서 값 바꿀 때도 즉시 반영
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr != null && !allowChange) sr.flipX = lockedValue;
    }
#endif
}
