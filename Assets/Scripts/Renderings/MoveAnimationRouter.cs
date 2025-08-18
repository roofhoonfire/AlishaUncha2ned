using System.Collections;
using UnityEngine;

public class MoveAnimationRouter : MonoBehaviour
{
    public static MoveAnimationRouter Instance { get; private set; }
    [Header("Animator Layer")]
    public int animatorLayer = 0;

    [Header("DASH (가로 이동 우선)")]
    public string dashTrigger;                 // 선택사항
    public string dashStateName = "Alisha_Dash";
    public AnimationClip dashClip;             // 있으면 프레임 계산에 사용
    [Tooltip("몇 번째 스프라이트(프레임)에서 실제 위치를 옮길지")]
    public int dashWarpFrame = 4;
    [Tooltip("클립이 없거나 길이를 알 수 없을 때 총 프레임 수 강제 지정(0이면 무시)")]
    public int dashTotalFramesOverride = 0;

    [Header("JUMP (세로 이동 우선)")]
    public string jumpTrigger;                 // 선택사항
    public string jumpStateName = "Alisha_Jump";
    public AnimationClip jumpClip;             // 있으면 프레임 계산에 사용
    public int jumpWarpFrame = 4;
    public int jumpTotalFramesOverride = 0;

    [Header("Fallbacks")]
    public float defaultClipLen = 1f;          // 길이 모를 때 1초 가정
    public float defaultFPS = 30f;             // 프레임률 모를 때 30fps 가정
    public float stateEnterMaxWait = 0.5f;     // 상태 진입 보장 최대 대기

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // 외부에서 호출: 이동 액션 재생 + 워프 타이밍 제어
    public IEnumerator PlayMoveCo(int actorNum, LocalRenderingData data1, LocalRenderingData data2, ActionData action)
    {
        // 대상 플레이어 오브젝트/애니메이터
        if (LocalState.Instance == null || LocalState.Instance.PlayerObDic == null) yield break;
        if (!LocalState.Instance.PlayerObDic.TryGetValue(actorNum, out var actorGO) || actorGO == null) yield break;

        var anim = actorGO.GetComponentInChildren<Animator>();
        if (anim == null) { yield break; }

        // 이번 액션에서 실제 이동한 data 추출
        LocalRenderingData actorData = null;
        if (data1 != null && data1.actorNum == actorNum) actorData = data1;
        else if (data2 != null && data2.actorNum == actorNum) actorData = data2;
        if (actorData == null) yield break;

        // 목적지(World) 계산
        var targetTf = CharpointOf(actorData.curpos);
        if (targetTf == null) yield break;
        Vector3 startPos = actorGO.transform.position;
        Vector3 targetPos = targetTf.position;

        // 대시/점프 선택: 가로가 더 길면 Dash, 세로가 더 길면 Jump
        Vector3 delta = targetPos - startPos;
        bool useDash = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y);

        string stateName = useDash ? dashStateName : jumpStateName;
        string trigger = useDash ? dashTrigger : jumpTrigger;
        AnimationClip clip = useDash ? dashClip : jumpClip;
        int warpFrame = useDash ? dashWarpFrame : jumpWarpFrame;
        int framesOverride = useDash ? dashTotalFramesOverride : jumpTotalFramesOverride;

        // 애니 시작
        if (!string.IsNullOrEmpty(trigger)) anim.SetTrigger(trigger);
        else if (!string.IsNullOrEmpty(stateName)) anim.Play(stateName, animatorLayer, 0f);
        else yield break;

        // Attack/Move 상태 진입 보장
        yield return WaitForStateEnter(anim, stateName, stateEnterMaxWait);

        // 프레임 기준 → 노멀라이즈드 타임 임계 계산
        float clipLen = ClipLen(anim, stateName, clip);
        float fps = FPS(clip, framesOverride, clipLen);
        float warpNorm = Mathf.Clamp01((FrameToSeconds(warpFrame, fps) / Mathf.Max(clipLen, 0.0001f)));

        // 워프 수행 여부
        bool warped = false;

        // 상태가 끝날 때까지 루프
        while (true)
        {
            if (!TryGetNormTowardsState(anim, stateName, animatorLayer, out float curNorm))
            {
                // 상태를 잃었으면 종료
                break;
            }

            if (!warped && curNorm >= warpNorm)
            {
                actorGO.transform.position = targetPos; // ★ 실제 이동(순간 워프)
                warped = true;
            }

            // 상태 유지 여부 점검
            var s = anim.GetCurrentAnimatorStateInfo(animatorLayer);
            bool still = s.IsName(stateName) || (anim.IsInTransition(animatorLayer) && anim.GetNextAnimatorStateInfo(animatorLayer).IsName(stateName));
            if (!still) break;

            yield return null;
        }

        // 혹시 애니가 너무 빨리 끝나서 워프 못 했으면 안전 보정
        if (!warped) actorGO.transform.position = targetPos;
    }

    // ---------- 내부 유틸 ----------
    private static Transform CharpointOf(int tileIndex)
    {
        if (GridManagement.Instance == null) return null;
        if (!GridManagement.Instance.tileObjects.TryGetValue(tileIndex, out var tile) || tile == null) return null;
        var cp = tile.transform.Find("charpoint");
        return cp != null ? cp : tile.transform;
    }

    private float ClipLen(Animator anim, string stateName, AnimationClip clipOverride)
    {
        if (clipOverride != null && clipOverride.length > 0f) return clipOverride.length;
        var cur = anim.GetCurrentAnimatorStateInfo(animatorLayer);
        if (cur.IsName(stateName)) return Mathf.Max(cur.length, defaultClipLen);
        if (anim.IsInTransition(animatorLayer))
        {
            var next = anim.GetNextAnimatorStateInfo(animatorLayer);
            if (next.IsName(stateName)) return Mathf.Max(next.length, defaultClipLen);
        }
        return defaultClipLen;
    }

    private float FPS(AnimationClip clipOverride, int framesOverride, float clipLen)
    {
        if (clipOverride != null && clipOverride.frameRate > 0f) return clipOverride.frameRate;
        if (framesOverride > 0 && clipLen > 0f) return framesOverride / clipLen;
        return defaultFPS;
    }

    private float FrameToSeconds(int frame, float fps)
    {
        frame = Mathf.Max(0, frame);
        fps = Mathf.Max(1f, fps);
        return frame / fps;
    }

    private IEnumerator WaitForStateEnter(Animator anim, string stateName, float maxWaitSec)
    {
        if (string.IsNullOrEmpty(stateName)) yield break;
        float end = Time.realtimeSinceStartup + Mathf.Max(0.01f, maxWaitSec);
        while (Time.realtimeSinceStartup < end)
        {
            if (TryGetNormTowardsState(anim, stateName, animatorLayer, out _))
                yield break;
            yield return null;
        }
        Debug.LogWarning($"[MoveAnimationRouter] 상태 진입 타임아웃: {stateName}");
    }

    private bool TryGetNormTowardsState(Animator anim, string targetStateName, int layer, out float norm)
    {
        norm = 0f;
        if (string.IsNullOrEmpty(targetStateName)) return false;

        var cur = anim.GetCurrentAnimatorStateInfo(layer);
        if (cur.IsName(targetStateName)) { norm = cur.normalizedTime; return true; }

        if (anim.IsInTransition(layer))
        {
            var next = anim.GetNextAnimatorStateInfo(layer);
            if (next.IsName(targetStateName)) { norm = next.normalizedTime; return true; }
        }
        return false;
    }
}
