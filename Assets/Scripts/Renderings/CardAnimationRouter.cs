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
        CardAnimationDB.CardAnimEntry entry,
        HookType hook, HitResolution? forcedOutcome)
    {
        // 미스 여부를 한 번만 계산해 재사용
        bool missFlow = (hook == HookType.Activate
                         && forcedOutcome.HasValue
                         && forcedOutcome.Value == HitResolution.Missed);

        // === Prep ===
        if (!string.IsNullOrEmpty(entry.prepTrigger) || !string.IsNullOrEmpty(entry.prepStateName))
        {
            int prepHash = 0;
            if (!string.IsNullOrEmpty(entry.prepStateName))
                prepHash = Animator.StringToHash(entry.prepStateName);

            if (!string.IsNullOrEmpty(entry.prepTrigger))
                attackerAnim.SetTrigger(entry.prepTrigger);
            else if (!string.IsNullOrEmpty(entry.prepStateName))
                attackerAnim.CrossFadeInFixedTime(prepHash, crossFade, animatorLayer);

            // Prep 상태 진입
            yield return WaitForStateEnter(attackerAnim, entry.prepStateName, prepEnterMaxWait);

            // 포인트1 카메라
            if (enableCameraCinematic && camMove_Point1 && CameraLovesAlisha.Instance != null && !string.IsNullOrEmpty(entry.prepStateName))
            {
                float prepLen = 0f; TryGetTargetStateLength(attackerAnim, entry.prepStateName, animatorLayer, out prepLen);
                float prepNorm = 0f; TryGetNormTowardsState(attackerAnim, entry.prepStateName, animatorLayer, out prepNorm);
                float remain = Mathf.Max(0f, (prepLen > 0f ? (1f - (prepNorm % 1f)) * prepLen : 0.2f));
                CameraLovesAlisha.Instance.MoveToPoint1_Attacker(attackerGO.transform, remain, cameraPoint1FOV);
            }

            // Prep 종료까지
            yield return WaitForStateEnd(attackerAnim, entry.prepStateName);

            // 포즈 홀드 + 백드롭
            if (entry.prepPoseHoldSec > 0f)
            {
                FreezeOnLastFrame(attackerAnim, entry.prepStateName, true);

                if (enableCameraCinematic && entry.usePrepBackdrop && entry.prepBackdropSprite != null && CameraLovesAlisha.Instance != null)
                {
                    CameraLovesAlisha.Instance.BeginPoint1Backdrop(
                        attackerGO, entry.prepBackdropSprite,
                        entry.prepBackdropFadeIn,
                        entry.prepPoseHoldSec
                    );
                }

                yield return new WaitForSecondsRealtime(entry.prepPoseHoldSec);

                FreezeOnLastFrame(attackerAnim, entry.prepStateName, false);

                if (enableCameraCinematic && entry.usePrepBackdrop && entry.prepBackdropSprite != null && CameraLovesAlisha.Instance != null)
                {
                    CameraLovesAlisha.Instance.EndPoint1Backdrop(entry.prepBackdropFadeOut);
                }
            }
        }

        // ★ 포인트1 시퀀스가 끝난 '직후' — Miss면 곧바로 복귀 트윈 시작
        if (missFlow && enableCameraCinematic && camMove_Return && CameraLovesAlisha.Instance != null)
        {
            CameraLovesAlisha.Instance.ReturnToDefault(cameraReturnDuration, attackerGO.transform);
        }

        // === Attack 진입 ===
        if (!string.IsNullOrEmpty(entry.animatorTrigger))
            attackerAnim.SetTrigger(entry.animatorTrigger);
        else if (!string.IsNullOrEmpty(entry.stateName))
            attackerAnim.CrossFadeInFixedTime(Animator.StringToHash(entry.stateName), crossFade, animatorLayer);

        // Attack 상태 진입 보장
        yield return WaitForStateEnter(attackerAnim, entry.stateName, attackEnterMaxWait);

        // Miss가 아닐 때만 포인트2 이동 (피격자 쪽으로)
        if (!missFlow && enableCameraCinematic && camMove_Point2 && CameraLovesAlisha.Instance != null && victimGO != null)
        {
            int nFrame = (entry.hitStops != null && entry.hitStops.Count > 0) ? entry.hitStops.Min(h => h.frame) : -1;
            if (nFrame >= 0)
            {
                float clipLen = 0f;
                if (entry.clip != null) clipLen = entry.clip.length;
                else if (!TryGetTargetStateLength(attackerAnim, entry.stateName, animatorLayer, out clipLen) || clipLen <= 0f)
                    clipLen = 1f;

                float fps = (entry.clip != null && entry.clip.frameRate > 0f) ? entry.clip.frameRate :
                            (entry.totalFramesOverride > 0 && clipLen > 0f) ? entry.totalFramesOverride / clipLen : 30f;

                float timeToN = Mathf.Max(0f, nFrame / Mathf.Max(1f, fps));
                CameraLovesAlisha.Instance.MoveToPoint2_Victim(victimGO.transform, timeToN, cameraPoint2FOV);
            }
        }

        // Attack 구간 오케스트레이션
        yield return StartCoroutine(
            PlayWithVictimAndHitStops(attackerAnim, victimAnim, attackerGO, victimGO, entry, hook, forcedOutcome)
        );

        // 종료 복귀: Miss가 아닐 때만 (Miss는 포인트1 끝나고 이미 복귀 중)
        if (!missFlow && enableCameraCinematic && camMove_Return && CameraLovesAlisha.Instance != null)
            CameraLovesAlisha.Instance.ReturnToDefault(cameraReturnDuration, attackerGO.transform);
    }
    private IEnumerator PlayWithVictimAndHitStops(
    Animator attackerAnim, Animator victimAnim,
    GameObject attackerGO, GameObject victimGO,
    CardAnimationDB.CardAnimEntry entry,
    HookType hook, HitResolution? forcedOutcome)
    {
        if (string.IsNullOrEmpty(entry.stateName))
        {
            Debug.LogWarning($"[CardAnimationRouter] stateName 미지정: {entry.cardCode}/{entry.hook}");
            yield break;
        }

        // 클립/프레임 기본
        float clipLen = 0f;
        if (entry.clip != null) clipLen = entry.clip.length;
        else if (!TryGetTargetStateLength(attackerAnim, entry.stateName, animatorLayer, out clipLen) || clipLen <= 0f)
            clipLen = 1f;

        float fps = (entry.clip != null && entry.clip.frameRate > 0f) ? entry.clip.frameRate :
                    (entry.totalFramesOverride > 0 && clipLen > 0f) ? entry.totalFramesOverride / clipLen : 30f;

        // Miss면 공격 상태 끝날 때까지만 대기
        bool missFlow = (hook == HookType.Activate && forcedOutcome.HasValue && forcedOutcome.Value == HitResolution.Missed);
        if (missFlow)
        {
            // ← WaitForStateEnd는 더 안전한 구현으로 교체되어 있다고 가정(상태 이탈도 종료로 인정)
            yield return WaitForStateEnd(attackerAnim, entry.stateName);
            yield break;
        }

        // Defend 여부
        bool defendFlow = (hook == HookType.Activate && forcedOutcome.HasValue && forcedOutcome.Value == HitResolution.Defended);

        // Victim 시작 타이밍 큐
        var victimStarts = (entry.victimStartFrames != null) ? entry.victimStartFrames.OrderBy(x => x).ToList() : new List<int>();
        var victimTargets = new Queue<float>();
        foreach (var f in victimStarts)
        {
            float tSec = f / fps;
            float norm = Mathf.Clamp01(clipLen > 0f ? (tSec / clipLen) : 0f);
            victimTargets.Enqueue(norm);
        }

        // HitStop 스펙
        var hitStops = (entry.hitStops != null) ? entry.hitStops.OrderBy(h => h.frame).ToList() : new List<CardAnimationDB.HitStopSpec>();
        int hitIndex = 0;

        float victimMinNorm = ComputeVictimMinNorm(entry);

        // ★ 추가: 하드 세이프티(클립길이 + 2초)
        float loopStart = Time.realtimeSinceStartup;
        float hardCap = Mathf.Max(clipLen, 0.3f) + 2f;

        while (true)
        {
            // ★ 추가: 목표 state를 떠났으면 즉시 종료
            if (HasLeftState(attackerAnim, entry.stateName, animatorLayer))
                break;

            if (!TryGetNormTowardsState(attackerAnim, entry.stateName, animatorLayer, out float curNorm))
            {
                // 한 프레임 기다린 후에도 이미 떠났으면 종료
                yield return null;
                if (HasLeftState(attackerAnim, entry.stateName, animatorLayer)) break;
                continue;
            }

            bool victimTriggeredThisFrame = false;

            // ---- K 프레임: 피격자 애니 시작 ----
            while (victimTargets.Count > 0 && curNorm >= victimTargets.Peek())
            {
                victimTargets.Dequeue();
                if (victimGO != null && victimAnim != null)
                {
                    // Defend면 방어용 상태/트리거 우선
                    string vTrig = defendFlow && !string.IsNullOrEmpty(entry.victimDefendTrigger) ? entry.victimDefendTrigger : entry.victimAnimatorTrigger;
                    string vState = defendFlow && !string.IsNullOrEmpty(entry.victimDefendStateName) ? entry.victimDefendStateName : entry.victimStateName;

                    if (!string.IsNullOrEmpty(vTrig))
                        victimAnim.SetTrigger(vTrig);
                    else if (!string.IsNullOrEmpty(vState))
                        victimAnim.Play(vState, animatorLayer, 0f);

                    victimAnim.Update(0f);
                    NudgeVictimHalfFrame(victimAnim, vState, entry.victimClip);

                    victimTriggeredThisFrame = true;

                    // K 임팩트
                    if (enableCameraCinematic && CameraLovesAlisha.Instance != null)
                    {
                        if (impactK_Shake) CameraLovesAlisha.Instance.ShakeSoft();
                        if (impactK_PunchFOV) CameraLovesAlisha.Instance.PunchSoft();
                        if (impactK_Flash) ScreenFlashFX.Instance?.Flash(flashK_Color, flashK_FadeIn, flashK_Hold, flashK_FadeOut, flashK_MaxAlpha);
                    }
                }
            }

            // ---- N 프레임: 히트스톱 ----
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
                        string targetStateForCheck = defendFlow
                            ? (string.IsNullOrEmpty(entry.victimDefendStateName) ? entry.victimStateName : entry.victimDefendStateName)
                            : entry.victimStateName;

                        if (TryGetNormTowardsState(victimAnim, targetStateForCheck, animatorLayer, out float vNorm))
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

                    // N 임팩트
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

                    // 해제 임팩트
                    if (enableCameraCinematic && CameraLovesAlisha.Instance != null)
                    {
                        if (impactAfter_Shake) CameraLovesAlisha.Instance.ShakeSoft();
                        if (impactAfter_PunchFOV) CameraLovesAlisha.Instance.PunchHard();
                    }

                    hitIndex++;
                    continue;
                }
            }

            // ★ 추가: 하드 세이프티(예상치 못한 루프 방지)
            if (Time.realtimeSinceStartup - loopStart > hardCap)
            {
                Debug.LogWarning($"[CardAnimationRouter] safety break in attack loop: {entry.cardCode}/{entry.hook}");
                break;
            }

            yield return null;
        }
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

        // 1) 우선 해당 state에 '한 번이라도' 진입할 때까지 대기(최대 5초)
        float safetyEnd = Time.realtimeSinceStartup + 5f;
        while (Time.realtimeSinceStartup < safetyEnd)
        {
            if (TryGetNormTowardsState(anim, stateName, animatorLayer, out _))
                break;
            yield return null;
        }
        if (Time.realtimeSinceStartup >= safetyEnd)
        {
            Debug.LogWarning($"[CardAnimationRouter] WaitForStateEnd: never entered {stateName}, safety exit.");
            yield break;
        }

        // 2) 종료 조건 확대:
        //    - (a) target state에서 normalizedTime >= 1 && not in transition
        //    - (b) target state를 '떠났음'(현재/다음 모두 target 아님)
        safetyEnd = Time.realtimeSinceStartup + 10f; // 2차 안전망
        while (Time.realtimeSinceStartup < safetyEnd)
        {
            var cur = anim.GetCurrentAnimatorStateInfo(animatorLayer);
            bool inTransition = anim.IsInTransition(animatorLayer);

            bool inTargetNow = cur.IsName(stateName);
            bool nextIsTarget = inTransition && anim.GetNextAnimatorStateInfo(animatorLayer).IsName(stateName);

            // (a) 클립 자연 종료
            if (inTargetNow && !inTransition && cur.normalizedTime >= 1f)
                yield break;

            // (b) 대상 state를 떠남
            if (!inTargetNow && !nextIsTarget)
                yield break;

            yield return null;
        }

        Debug.LogWarning($"[CardAnimationRouter] WaitForStateEnd: timeout on {stateName}, forced exit.");
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

    private float ComputeVictimMinNorm(CardAnimationDB.CardAnimEntry e)
    {
        float vf = (e.victimClip != null && e.victimClip.frameRate > 0f) ? e.victimClip.frameRate : 30f;
        float vlen = (e.victimClip != null && e.victimClip.length > 0f) ? e.victimClip.length : 1f;
        float halfFrameSec = 0.5f / vf; // 반 프레임
        return Mathf.Clamp01(halfFrameSec / vlen);
    }

    // (defend/damage 모두 지원: 선택된 상태명 기준으로 반 프레임 밀기)
    private void NudgeVictimHalfFrame(Animator victimAnim, string chosenStateName, AnimationClip clipOverride = null)
    {
        float vf = (clipOverride != null && clipOverride.frameRate > 0f) ? clipOverride.frameRate : 30f;
        float len = (clipOverride != null && clipOverride.length > 0f) ? clipOverride.length : 1f;
        float halfFrameNorm = Mathf.Clamp01((0.5f / vf) / len);

        if (!string.IsNullOrEmpty(chosenStateName))
        {
            victimAnim.Play(chosenStateName, animatorLayer, halfFrameNorm);
            victimAnim.Update(0f);
        }
    }

    // 외부 API
    public IEnumerator PlayCo(string cardCode, int attackerActorNum, HookType hook, int? victimActorNum = null, HitResolution? forcedOutcome = null)
    {
        if (LocalState.Instance == null || LocalState.Instance.PlayerObDic == null) yield break;
        if (!LocalState.Instance.PlayerObDic.TryGetValue(attackerActorNum, out var attackerGO) || attackerGO == null) yield break;

        if (!_TryGetEntry(cardCode, hook, out var entry))
            yield break;

        GameObject victimGO = null;
        if (victimActorNum == null)
        {
            foreach (var kv in LocalState.Instance.PlayerObDic)
                if (kv.Key != attackerActorNum) { victimGO = kv.Value; break; }
        }
        else
        {
            LocalState.Instance.PlayerObDic.TryGetValue(victimActorNum.Value, out victimGO);
        }

        var attackerAnim = attackerGO.GetComponentInChildren<Animator>();
        var victimAnim = victimGO ? victimGO.GetComponentInChildren<Animator>() : null;

        if (enableCameraCinematic && CameraLovesAlisha.Instance != null)
        {
            var cam = CameraLovesAlisha.Instance;
            cam.defaultFOV = cameraDefaultFOV;
            cam.returnDuration = cameraReturnDuration;
            cam.SetDefault(attackerGO.transform);
        }

        yield return StartCoroutine(
            PlayOrchestrated(attackerAnim, victimAnim, attackerGO, victimGO, entry, hook, forcedOutcome)
        );

        // 백드롭 페이드아웃 시간 대기(있다면)
        if (enableCameraCinematic && entry.usePrepBackdrop && entry.prepBackdropSprite != null)
        {
            float fadeOut = Mathf.Max(0f, entry.prepBackdropFadeOut);
            if (fadeOut > 0f) yield return new WaitForSecondsRealtime(fadeOut);
        }

        // 카메라 복귀 트윈 시간 대기(있다면) — 트윈 자체는 PlayOrchestrated에서 이미 시작됨
        if (enableCameraCinematic && camMove_Return && CameraLovesAlisha.Instance != null)
        {
            float ret = Mathf.Max(0f, cameraReturnDuration);
            if (ret > 0f) yield return new WaitForSecondsRealtime(ret);
        }
    }

    // 레거시 호환
    public void Play(string cardCode, int attackerActorNum, HookType hook, int? victimActorNum = null)
    {
        StartCoroutine(PlayCo(cardCode, attackerActorNum, hook, victimActorNum));
    }

    private bool HasLeftState(Animator anim, string stateName, int layer)
    {
        var cur = anim.GetCurrentAnimatorStateInfo(layer);
        if (cur.IsName(stateName)) return false;
        if (anim.IsInTransition(layer))
        {
            var next = anim.GetNextAnimatorStateInfo(layer);
            if (next.IsName(stateName)) return false;
        }
        return true;
    }
}
