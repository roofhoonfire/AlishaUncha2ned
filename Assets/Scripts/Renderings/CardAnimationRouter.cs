using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CardAnimationRouter : MonoBehaviour
{
    public static CardAnimationRouter Instance { get; private set; }

    [Header("DB 참조")]
    public CardAnimationDB db;

    // (cardCode, hook) → Entry
    private Dictionary<(string, HookType), CardAnimationDB.CardAnimEntry> _map;

    [Header("Animator Layer Index")]
    public int animatorLayer = 0;

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

    /// <summary>
    /// current 또는 전이중 next 상태가 target이면 그 상태의 normalizedTime을 반환.
    /// </summary>
    private bool TryGetNormTowardsState(Animator anim, string targetStateName, int layer, out float norm)
    {
        norm = 0f;

        var cur = anim.GetCurrentAnimatorStateInfo(layer);
        if (cur.IsName(targetStateName))
        {
            norm = cur.normalizedTime; // 0→1
            return true;
        }

        if (anim.IsInTransition(layer))
        {
            var next = anim.GetNextAnimatorStateInfo(layer);
            if (next.IsName(targetStateName))
            {
                norm = next.normalizedTime; // 0→1
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// target 상태의 length(초)를 current/next에서 구한다(없으면 0).
    /// </summary>
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
    /// 카드 코드 + 훅에 맞는 공격 애니메이션을 재생하고, 피격자 애니/히트스탑을 동기화.
    /// victimActorNum을 넘기지 않으면 1v1 전제에서 자동 추론.
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

        // victim 추론(1v1): 키 중 공격자가 아닌 첫 번째
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

        // 공격자 트리거 발화
        if (!string.IsNullOrEmpty(entry.animatorTrigger))
            attackerAnim.SetTrigger(entry.animatorTrigger);

        // 메인 코루틴: 피격자 타이밍 + 히트스탑 동기화
        StartCoroutine(PlayWithVictimAndHitStops(attackerAnim, victimAnim, attackerGO, victimGO, entry));
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

        // 길이/프레임레이트 계산(공격자 기준)
        float clipLen = 0f;
        if (entry.clip != null) clipLen = entry.clip.length;
        else if (!TryGetTargetStateLength(attackerAnim, entry.stateName, animatorLayer, out clipLen) || clipLen <= 0f)
            clipLen = 1f; // 모르면 1초 가정

        float fps = 0f;
        if (entry.clip != null && entry.clip.frameRate > 0f) fps = entry.clip.frameRate;
        else if (entry.totalFramesOverride > 0 && clipLen > 0f) fps = entry.totalFramesOverride / clipLen;
        else fps = 30f; // 보수적 기본

        // 피격자 시작 타이밍(정규화 타임 큐로 변환)
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

        // 히트스탑 타이밍
        var hitStops = (entry.hitStops != null)
            ? entry.hitStops.OrderBy(h => h.frame).ToList()
            : new List<CardAnimationDB.HitStopSpec>();
        int hitIndex = 0;

        // (선택) 이펙트 애니모음(현재 프로젝트에 IEffectAnimatorSource 없으면 null 반환해도 OK)
        var effectAnims = CollectEffectAnimators(attackerGO, victimGO);

        // 피격자가 "반 프레임" 이상 진행했는지 판단 임계값(정규화)
        float victimMinNorm = ComputeVictimMinNorm(entry);

        while (true)
        {
            // 공격자 진행률: current/next 모두 허용
            if (!TryGetNormTowardsState(attackerAnim, entry.stateName, animatorLayer, out float curNorm))
            {
                // 더 이상 해당 상태가 아니면 종료
                yield return null;
                if (!TryGetNormTowardsState(attackerAnim, entry.stateName, animatorLayer, out _)) break;
                continue;
            }

            bool victimTriggeredThisFrame = false;

            // 1) 피격자 애니 시작
            while (victimTargets.Count > 0 && curNorm >= victimTargets.Peek())
            {
                victimTargets.Dequeue();
                if (victimAnim != null && !string.IsNullOrEmpty(entry.victimAnimatorTrigger))
                {
                    victimAnim.SetTrigger(entry.victimAnimatorTrigger);

                    // 즉시 전이 평가(상태 즉시 진입 확정)
                    victimAnim.Update(0f);

                    // ★ 반 프레임만 앞으로 밀어 고정성 확보(0프레임 정지 방지)
                    NudgeVictimHalfFrame(victimAnim, entry);

                    victimTriggeredThisFrame = true;   // 같은 프레임 히트스탑은 다음 틱으로 미룸
                }
            }

            // 2) 히트스탑
            if (hitIndex < hitStops.Count)
            {
                var spec = hitStops[hitIndex];
                float specTimeSec = spec.frame / fps;
                float specNorm = Mathf.Clamp01(clipLen > 0f ? (specTimeSec / clipLen) : 0f);

                if (curNorm >= specNorm)
                {
                    bool needDelay = false;

                    // A) 방금 피격 트리거가 나간 프레임이면 지연
                    if (victimTriggeredThisFrame) needDelay = true;

                    // B) 피격자 정지 대상인데 아직 충분히 진행하지 못했으면 지연
                    if (!needDelay && spec.pauseVictim && victimAnim != null)
                    {
                        if (TryGetNormTowardsState(victimAnim, entry.victimStateName, animatorLayer, out float vNorm))
                        {
                            if (vNorm < victimMinNorm) needDelay = true;
                        }
                        else
                        {
                            // 아직 피격 상태 아님 → 지연
                            needDelay = true;
                        }
                    }

                    if (needDelay)
                    {
                        // 이번 프레임의 Animator 갱신이 끝날 때까지 미룸
                        yield return new WaitForEndOfFrame();
                        continue; // 다음 루프에서 다시 검사
                    }

                    var pauseList = new List<Animator>();
                    if (spec.pauseAttacker && attackerAnim != null) pauseList.Add(attackerAnim);
                    if (spec.pauseVictim && victimAnim != null) pauseList.Add(victimAnim);
                    if (spec.pauseEffects && effectAnims != null && effectAnims.Count > 0)
                        pauseList.AddRange(effectAnims.Where(a => a != null));

                    yield return HitStopManager.Instance.HitStop(spec.duration, pauseList, spec.pauseScene);
                    hitIndex++;
                    continue;
                }
            }

            // 타깃 상태 이탈 시 종료(Idle 복귀 등)
            bool stillTargeting =
                attackerAnim.GetCurrentAnimatorStateInfo(animatorLayer).IsName(entry.stateName) ||
                (attackerAnim.IsInTransition(animimatorLayer) &&
                 attackerAnim.GetNextAnimatorStateInfo(animatorLayer).IsName(entry.stateName));
            if (!stillTargeting) break;

            yield return null;
        }
    }

    /// <summary>
    /// 피격자가 최소 "반 프레임"은 진행했는지 판단하기 위한 정규화 임계값.
    /// victimClip이 없으면 30fps, 길이 1초 가정.
    /// </summary>
    private float ComputeVictimMinNorm(CardAnimationDB.CardAnimEntry e)
    {
        float vf = (e.victimClip != null && e.victimClip.frameRate > 0f) ? e.victimClip.frameRate : 30f;
        float vlen = (e.victimClip != null && e.victimClip.length > 0f) ? e.victimClip.length : 1f;
        float halfFrameSec = 0.5f / vf; // 반 프레임
        return Mathf.Clamp01(halfFrameSec / vlen);
    }

    /// <summary>
    /// 피격 트리거 직후 반 프레임만 진행 위치로 워프(0프레임 정지 방지).
    /// </summary>
    private void NudgeVictimHalfFrame(Animator victimAnim, CardAnimationDB.CardAnimEntry e)
    {
        float vf = (e.victimClip != null && e.victimClip.frameRate > 0f) ? e.victimClip.frameRate : 30f;
        float len = (e.victimClip != null && e.victimClip.length > 0f) ? e.victimClip.length : 1f;
        float halfFrameNorm = Mathf.Clamp01((0.5f / vf) / len);

        victimAnim.Play(e.victimStateName, animatorLayer, halfFrameNorm);
        victimAnim.Update(0f);
    }

    private List<Animator> CollectEffectAnimators(GameObject attackerGO, GameObject victimGO)
    {
        // 프로젝트에 IEffectAnimatorSource 없으면 null/빈 리스트로 두자.
        return null;
    }
}
