using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CardAnimationRouter : MonoBehaviour
{
    public static CardAnimationRouter Instance { get; private set; }

    // -------- Camera cinematic (이동/줌 연출) --------
    [Header("Camera Cinematic (Movement)")]
    public bool enableCameraCinematic = true;      // 전체 카메라 시네마틱 on/off
    public bool camMove_Point1 = true;             // Prep 끝(포인트1) 이동/줌
    public bool camMove_Point2 = true;             // N프레임(포인트2) 이동/줌
    public bool camMove_Return = true;             // Attack 종료 시 복귀

    public float cameraDefaultFOV = 60f;           // 디폴트 복귀 FOV
    public float cameraReturnDuration = 0.35f;
    public float cameraPoint1FOV = 15f;            // 포인트1 목표 FOV
    public float cameraPoint2FOV = 40f;            // 포인트2 목표 FOV

    // -------- Impact toggles (효과 믹스) --------
    [Header("Impact at K (피격 시작 프레임)")]
    public bool impactK_Shake = true;
    public bool impactK_PunchFOV = true;
    public bool impactK_Flash = true;

    [Header("Impact at N (히트스톱 시작 프레임)")]
    public bool impactN_Shake = true;
    public bool impactN_Flash = true;

    [Header("Impact After HitStop (해제 직후)")]
    public bool impactAfter_Shake = true;
    public bool impactAfter_PunchFOV = true;

    // -------- Flash settings --------
    [Header("Screen Flash - K")]
    public Color flashK_Color = Color.white;
    public float flashK_FadeIn = 0.015f;
    public float flashK_Hold = 0.02f;
    public float flashK_FadeOut = 0.08f;
    [Range(0f, 1f)] public float flashK_MaxAlpha = 0.55f;

    [Header("Screen Flash - N")]
    public Color flashN_Color = Color.white;
    public float flashN_FadeIn = 0.010f;
    [Tooltip("히트스톱 길이에 비례(hold = min(scale * duration, holdClamp))")]
    public float flashN_HoldScale = 0.4f;
    public float flashN_HoldClamp = 0.05f;
    public float flashN_FadeOut = 0.10f;
    [Range(0f, 1f)] public float flashN_MaxAlpha = 0.75f;

    // -------- DB/Animator 기본 세팅 --------
    [Header("DB 참조")]
    public CardAnimationDB db;

    // (cardCode, hook) → Entry
    private Dictionary<(string, HookType), CardAnimationDB.CardAnimEntry> _map;

    [Header("Animator Layer Index")]
    public int animatorLayer = 0;

    [Header("전환/대기 옵션")]
    [Tooltip("상태 전환 크로스페이드(초)")]
    public float crossFade = 0.05f;
    [Tooltip("Prep 상태 진입 대기 최대(초)")]
    public float prepEnterMaxWait = 0.5f;
    [Tooltip("Attack 상태 진입 대기 최대(초)")]
    public float attackEnterMaxWait = 0.5f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildMap();
    }

    private void BuildMap()
    {
        _map = new Dictionary<(string, HookType), CardAnimationDB.CardAnimEntry>();
        if (db == null || db.entries == null) return;

        foreach (var e in db.entries)
        {
            if (string.IsNullOrEmpty(e.cardCode))
            {
                Debug.LogWarning("[CardAnimationRouter] 빈 cardCode 항목이 있음");
                continue;
            }
            var key = (Normalize(e.cardCode), e.hook);
            if (_map.ContainsKey(key))
            {
                Debug.LogWarning($"[CardAnimationRouter] 중복 매핑: {e.cardCode} / {e.hook}");
                continue;
            }
            _map[key] = e;
        }
    }

    private string Normalize(string s) => (s ?? "").Trim();

    private bool TryGetNormTowardsState(Animator anim, string targetStateName, int layer, out float norm)
    {
        norm = 0f;
        var cur = anim.GetCurrentAnimatorStateInfo(layer);
        if (cur.IsName(targetStateName)) { norm = cur.normalizedTime; return true; }

        if (anim.IsInTransition(layer))
        {
            var next = anim.GetNextAnimatorStateInfo(layer);
            if (next.IsName(targetStateName)) { norm = next.normalizedTime; return true; }
        }
        return false;
    }

    private bool TryGetTargetStateLength(Animator anim, string targetStateName, int layer, out float length)
    {
        var cur = anim.GetCurrentAnimatorStateInfo(layer);
        if (cur.IsName(targetStateName)) { length = cur.length; return true; }

        if (anim.IsInTransition(layer))
        {
            var next = anim.GetNextAnimatorStateInfo(layer);
            if (next.IsName(targetStateName)) { length = next.length; return true; }
        }

        length = 0f;
        return false;
    }

    /// <summary>
    /// 카드 코드 + 훅에 맞는 애니메이션 오케스트레이션 (Prep→PoseHold→Attack→K/N 임팩트/히트스톱).
    /// </summary>
    public void Play(string cardCode, int attackerActorNum, HookType hook, int? victimActorNum = null)
    {
        if (LocalState.Instance == null || LocalState.Instance.PlayerObDic == null)
        {
            Debug.LogError("[CardAnimationRouter] LocalState.Instance.PlayerObDic 없음");
            return;
        }

        if (!LocalState.Instance.PlayerObDic.TryGetValue(attackerActorNum, out var attackerGO) || attackerGO == null)
        {
            Debug.LogError($"[CardAnimationRouter] attacker actor {attackerActorNum} 오브젝트 없음");
            return;
        }

        if (!_TryGetEntry(cardCode, hook, out var entry))
        {
            Debug.LogWarning($"[CardAnimationRouter] 매핑 없음 → card:{cardCode}, hook:{hook}");
            return;
        }

        // victim 추론(1v1)
        int? victimNumResolved = victimActorNum;
        if (victimNumResolved == null)
        {
            foreach (var kv in LocalState.Instance.PlayerObDic)
            {
                if (kv.Key != attackerActorNum) { victimNumResolved = kv.Key; break; }
            }
        }

        GameObject victimGO = null;
        if (victimNumResolved != null)
            LocalState.Instance.PlayerObDic.TryGetValue(victimNumResolved.Value, out victimGO);

        var attackerAnim = attackerGO.GetComponentInChildren<Animator>();
        if (attackerAnim == null)
        {
            Debug.LogError("[CardAnimationRouter] 공격자 Animator 없음");
            return;
        }
        Animator victimAnim = null;
        if (victimGO != null) victimAnim = victimGO.GetComponentInChildren<Animator>();

        // 카메라 디폴트 셋업
        if (enableCameraCinematic && CameraLovesAlisha.Instance != null)
        {
            var cam = CameraLovesAlisha.Instance;
            cam.defaultFOV = cameraDefaultFOV;
            cam.returnDuration = cameraReturnDuration;
            cam.SetDefault(attackerGO.transform);
        }

        StartCoroutine(PlayOrchestrated(attackerAnim, victimAnim, attackerGO, victimGO, entry));
    }

    private bool _TryGetEntry(string cardCode, HookType hook, out CardAnimationDB.CardAnimEntry entry)
    {
        entry = null;
        if (_map == null) BuildMap();

        var key = (Normalize(cardCode), hook);
        if (_map != null && _map.TryGetValue(key, out entry)) return true;

        var fallbackKey = (Normalize(cardCode), HookType.Activate);
        if (_map != null && _map.TryGetValue(fallbackKey, out entry)) return true;

        return false;
    }

    // ================== 오케스트레이션 ==================

    private IEnumerator PlayOrchestrated(
       Animator attackerAnim, Animator victimAnim,
       GameObject attackerGO, GameObject victimGO,
       CardAnimationDB.CardAnimEntry entry)
    {
        // 1) Prep 진입
        if (!string.IsNullOrEmpty(entry.prepTrigger) || !string.IsNullOrEmpty(entry.prepStateName))
        {
            int prepHash = 0;
            if (!string.IsNullOrEmpty(entry.prepStateName))
                prepHash = Animator.StringToHash(entry.prepStateName);

            if (!string.IsNullOrEmpty(entry.prepTrigger))
                attackerAnim.SetTrigger(entry.prepTrigger);
            else if (!string.IsNullOrEmpty(entry.prepStateName))
                attackerAnim.CrossFadeInFixedTime(prepHash, crossFade, animatorLayer);

            // Prep 상태 진입 대기
            yield return WaitForStateEnter(attackerAnim, entry.prepStateName, prepEnterMaxWait);

            // === 카메라 포인트1 이동/줌 ===
            if (enableCameraCinematic && camMove_Point1 && CameraLovesAlisha.Instance != null && !string.IsNullOrEmpty(entry.prepStateName))
            {
                float prepLen = 0f; TryGetTargetStateLength(attackerAnim, entry.prepStateName, animatorLayer, out prepLen);
                float prepNorm = 0f; TryGetNormTowardsState(attackerAnim, entry.prepStateName, animatorLayer, out prepNorm);
                float remain = Mathf.Max(0f, (prepLen > 0f ? (1f - (prepNorm % 1f)) * prepLen : 0.2f));
                CameraLovesAlisha.Instance.MoveToPoint1_Attacker(attackerGO.transform, remain, cameraPoint1FOV);
            }

            // Prep 종료까지 대기
            yield return WaitForStateEnd(attackerAnim, entry.prepStateName);

            // 마지막 프레임 정지 + 백드롭 시작

            if (entry.prepPoseHoldSec > 0f)
            {
                FreezeOnLastFrame(attackerAnim, entry.prepStateName, true);

                // === 백드롭 시작 ===
                if (enableCameraCinematic && entry.usePrepBackdrop && entry.prepBackdropSprite != null && CameraLovesAlisha.Instance != null)
                {
                    CameraLovesAlisha.Instance.BeginPoint1Backdrop(
                        attackerGO,
                        entry.prepBackdropSprite,
                        entry.prepBackdropFadeIn,
                        entry.prepPoseHoldSec // ← 여기! CutLine이 이 시간에 맞춰 자동 스케일
                    );
                }

                // 정지 유지(클로즈업+백드롭 유지)
                yield return new WaitForSecondsRealtime(entry.prepPoseHoldSec);

                // 정지 해제 + 백드롭 종료
                FreezeOnLastFrame(attackerAnim, entry.prepStateName, false);

                if (enableCameraCinematic && entry.usePrepBackdrop && entry.prepBackdropSprite != null && CameraLovesAlisha.Instance != null)
                {
                    CameraLovesAlisha.Instance.EndPoint1Backdrop(entry.prepBackdropFadeOut);
                }
            }
          
        }

        // 2) Attack 진입
        if (!string.IsNullOrEmpty(entry.animatorTrigger))
            attackerAnim.SetTrigger(entry.animatorTrigger);
        else if (!string.IsNullOrEmpty(entry.stateName))
            attackerAnim.CrossFadeInFixedTime(Animator.StringToHash(entry.stateName), crossFade, animatorLayer);

        // Attack 상태 진입 보장
        yield return WaitForStateEnter(attackerAnim, entry.stateName, attackEnterMaxWait);

        // === 카메라 포인트2 이동/줌 (N 프레임까지) ===
        if (enableCameraCinematic && camMove_Point2 && CameraLovesAlisha.Instance != null && victimGO != null)
        {
            int nFrame = (entry.hitStops != null && entry.hitStops.Count > 0) ? entry.hitStops.Min(h => h.frame) : -1;
            if (nFrame >= 0)
            {
                float clipLen = 0f;
                if (entry.clip != null) clipLen = entry.clip.length;
                else if (!TryGetTargetStateLength(attackerAnim, entry.stateName, animatorLayer, out clipLen) || clipLen <= 0f)
                    clipLen = 1f;

                float fps = 0f;
                if (entry.clip != null && entry.clip.frameRate > 0f) fps = entry.clip.frameRate;
                else if (entry.totalFramesOverride > 0 && clipLen > 0f) fps = entry.totalFramesOverride / clipLen;
                else fps = 30f;

                float timeToN = Mathf.Max(0f, nFrame / Mathf.Max(1f, fps));
                CameraLovesAlisha.Instance.MoveToPoint2_Victim(victimGO.transform, timeToN, cameraPoint2FOV);
            }
        }

        // 3) Attack 구간에서 Victim 시작/HitStop 동기화
        yield return StartCoroutine(PlayWithVictimAndHitStops(attackerAnim, victimAnim, attackerGO, victimGO, entry));

        // 4) Attack 종료와 동시에 디폴트로 복귀
        if (enableCameraCinematic && camMove_Return && CameraLovesAlisha.Instance != null)
            CameraLovesAlisha.Instance.ReturnToDefault(cameraReturnDuration, attackerGO.transform);
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
        Debug.LogWarning($"[CardAnimationRouter] 상태 진입 타임아웃: {stateName}");
    }

    private IEnumerator WaitForStateEnd(Animator anim, string stateName)
    {
        if (string.IsNullOrEmpty(stateName)) yield break;

        while (!TryGetNormTowardsState(anim, stateName, animatorLayer, out _))
            yield return null;

        while (true)
        {
            var s = anim.GetCurrentAnimatorStateInfo(animatorLayer);
            if (!anim.IsInTransition(animatorLayer) && s.IsName(stateName) && s.normalizedTime >= 1f)
                break;
            yield return null;
        }
    }

    private void FreezeOnLastFrame(Animator anim, string stateName, bool freeze)
    {
        if (string.IsNullOrEmpty(stateName) || anim == null) return;

        int hash = Animator.StringToHash(stateName);
        if (freeze)
        {
            anim.Play(hash, animatorLayer, 0.999f);
            anim.Update(0f);
            anim.speed = 0f;
        }
        else
        {
            anim.speed = 1f;
        }
    }

    // ================== Attack 구간 동기화 (K/N/AfterShock) ==================

    private IEnumerator PlayWithVictimAndHitStops(
        Animator attackerAnim, Animator victimAnim,
        GameObject attackerGO, GameObject victimGO,
        CardAnimationDB.CardAnimEntry entry)
    {
        if (string.IsNullOrEmpty(entry.stateName))
        {
            Debug.LogWarning($"[CardAnimationRouter] stateName 미지정: {entry.cardCode}/{entry.hook}");
            yield break;
        }

        float clipLen = 0f;
        if (entry.clip != null) clipLen = entry.clip.length;
        else if (!TryGetTargetStateLength(attackerAnim, entry.stateName, animatorLayer, out clipLen) || clipLen <= 0f)
            clipLen = 1f;

        float fps = 0f;
        if (entry.clip != null && entry.clip.frameRate > 0f) fps = entry.clip.frameRate;
        else if (entry.totalFramesOverride > 0 && clipLen > 0f) fps = entry.totalFramesOverride / clipLen;
        else fps = 30f;

        var victimStarts = (entry.victimStartFrames != null)
            ? entry.victimStartFrames.OrderBy(x => x).ToList()
            : new List<int>();
        var victimTargets = new Queue<float>();
        foreach (var f in victimStarts)
        {
            float tSec = f / fps;
            float norm = Mathf.Clamp01(clipLen > 0f ? (tSec / clipLen) : 0f);
            victimTargets.Enqueue(norm);
        }

        var hitStops = (entry.hitStops != null)
            ? entry.hitStops.OrderBy(h => h.frame).ToList()
            : new List<CardAnimationDB.HitStopSpec>();
        int hitIndex = 0;

        float victimMinNorm = ComputeVictimMinNorm(entry);

        while (true)
        {
            if (!TryGetNormTowardsState(attackerAnim, entry.stateName, animatorLayer, out float curNorm))
            {
                yield return null;
                if (!TryGetNormTowardsState(attackerAnim, entry.stateName, animatorLayer, out _)) break;
                continue;
            }

            bool victimTriggeredThisFrame = false;

            // ---- K 프레임: 피격자 애니 시작 ----
            while (victimTargets.Count > 0 && curNorm >= victimTargets.Peek())
            {
                victimTargets.Dequeue();
                if (victimGO != null && victimAnim != null && !string.IsNullOrEmpty(entry.victimAnimatorTrigger))
                {
                    victimAnim.SetTrigger(entry.victimAnimatorTrigger);
                    victimAnim.Update(0f);
                    NudgeVictimHalfFrame(victimAnim, entry);
                    victimTriggeredThisFrame = true;

                    // K 임팩트 토글
                    if (enableCameraCinematic && CameraLovesAlisha.Instance != null)
                    {
                        if (impactK_Shake) CameraLovesAlisha.Instance.ShakeSoft();
                        if (impactK_PunchFOV) CameraLovesAlisha.Instance.PunchSoft();
                        if (impactK_Flash) ScreenFlashFX.Instance?.Flash(flashK_Color, flashK_FadeIn, flashK_Hold, flashK_FadeOut, flashK_MaxAlpha);
                    }
                }
            }

            // ---- N 프레임: 히트스톱 + 임팩트 ----
            if (hitIndex < hitStops.Count)
            {
                var spec = hitStops[hitIndex];
                float specTimeSec = spec.frame / fps;
                float specNorm = Mathf.Clamp01(clipLen > 0f ? (specTimeSec / clipLen) : 0f);

                if (curNorm >= specNorm)
                {
                    bool needDelay = false;

                    if (victimTriggeredThisFrame) needDelay = true;

                    if (!needDelay && spec.pauseVictim && victimAnim != null)
                    {
                        if (TryGetNormTowardsState(victimAnim, entry.victimStateName, animatorLayer, out float vNorm))
                        {
                            if (vNorm < victimMinNorm) needDelay = true;
                        }
                        else
                        {
                            needDelay = true;
                        }
                    }

                    if (needDelay)
                    {
                        yield return new WaitForEndOfFrame();
                        continue;
                    }

                    // N 임팩트(히트스톱 직전)
                    if (enableCameraCinematic && CameraLovesAlisha.Instance != null)
                    {
                        if (impactN_Shake) CameraLovesAlisha.Instance.ShakeHard(spec.duration);
                        if (impactN_Flash)
                        {
                            float hold = Mathf.Min(flashN_HoldScale * spec.duration, flashN_HoldClamp);
                            ScreenFlashFX.Instance?.Flash(flashN_Color, flashN_FadeIn, hold, flashN_FadeOut, flashN_MaxAlpha);
                        }
                    }

                    // 실제 히트스톱
                    var pauseList = new List<Animator>();
                    if (spec.pauseAttacker && attackerAnim != null) pauseList.Add(attackerAnim);
                    if (spec.pauseVictim && victimAnim != null) pauseList.Add(victimAnim);
                    yield return HitStopManager.Instance.HitStop(spec.duration, pauseList, spec.pauseScene);

                    // 애프터쇼크(히트스톱 해제 직후)
                    if (enableCameraCinematic && CameraLovesAlisha.Instance != null)
                    {
                        if (impactAfter_Shake) CameraLovesAlisha.Instance.ShakeSoft();
                        if (impactAfter_PunchFOV) CameraLovesAlisha.Instance.PunchHard();
                    }

                    hitIndex++;
                    continue;
                }
            }

            bool stillTargeting =
                attackerAnim.GetCurrentAnimatorStateInfo(animatorLayer).IsName(entry.stateName) ||
                (attackerAnim.IsInTransition(animatorLayer) &&
                 attackerAnim.GetNextAnimatorStateInfo(animatorLayer).IsName(entry.stateName));
            if (!stillTargeting) break;

            yield return null;
        }
    }

    private float ComputeVictimMinNorm(CardAnimationDB.CardAnimEntry e)
    {
        float vf = (e.victimClip != null && e.victimClip.frameRate > 0f) ? e.victimClip.frameRate : 30f;
        float vlen = (e.victimClip != null && e.victimClip.length > 0f) ? e.victimClip.length : 1f;
        float halfFrameSec = 0.5f / vf; // 반 프레임
        return Mathf.Clamp01(halfFrameSec / vlen);
    }

    private void NudgeVictimHalfFrame(Animator victimAnim, CardAnimationDB.CardAnimEntry e)
    {
        float vf = (e.victimClip != null && e.victimClip.frameRate > 0f) ? e.victimClip.frameRate : 30f;
        float len = (e.victimClip != null && e.victimClip.length > 0f) ? e.victimClip.length : 1f;
        float halfFrameNorm = Mathf.Clamp01((0.5f / vf) / len);

        victimAnim.Play(e.victimStateName, animatorLayer, halfFrameNorm);
        victimAnim.Update(0f);
    }
}
