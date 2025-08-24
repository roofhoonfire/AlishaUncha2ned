using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Anim/Animation Trigger Manager")]
public class AnimTriggerManager : MonoBehaviour
{
    public static AnimTriggerManager Instance { get; private set; }

    [System.Serializable]
    public class Receiver
    {
        [Tooltip("식별용 라벨 (선택)")]
        public string label;

        [Tooltip("트리거를 받을 대상 오브젝트 (Animator가 여기 또는 자식에 있음)")]
        public GameObject target;

        [Tooltip("Animator가 자식에 있다면 켜두세요")]
        public bool searchInChildren = true;

        [Tooltip("이 대상에게 허용할 트리거 이름들 (빈 리스트면 모든 트리거 허용)")]
        public List<string> allowedTriggers = new List<string>();

        // 런타임 캐시
        [HideInInspector] public Animator cachedAnimator;
        [HideInInspector] public HashSet<int> triggerHashSet = new HashSet<int>();
    }

    [Header("등록된 수신자들")]
    [SerializeField] private List<Receiver> receivers = new List<Receiver>();

    [Header("정책")]
    [Tooltip("true면 allowedTriggers에 없는 트리거는 거부")]
    [SerializeField] private bool strictMode = false;

    [Tooltip("같은 트리거 난사 방지 디바운스(초). 0이면 비활성")]
    [SerializeField, Min(0f)] private float defaultCooldown = 0.05f;

    [Tooltip("경고/로그 출력")]
    [SerializeField] private bool verboseLog = false;

    // AnimatorID + TriggerHash 합성키 -> 마지막 발사 시간
    private readonly Dictionary<long, float> _lastFired = new Dictionary<long, float>();
    private readonly Dictionary<int, Receiver> _animIdToReceiver = new Dictionary<int, Receiver>();
    private readonly Dictionary<string, Receiver> _labelToReceiver = new Dictionary<string, Receiver>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        ResolveAll();
    }

    private void OnEnable() => ResolveAll();

    /// <summary>인스펙터 설정을 바탕으로 Animator/해시를 전부 캐싱</summary>
    public void ResolveAll()
    {
        _animIdToReceiver.Clear();
        _labelToReceiver.Clear();

        foreach (var r in receivers)
        {
            r.cachedAnimator = null;
            r.triggerHashSet.Clear();

            if (r.target == null) continue;

            var anim = r.target.GetComponent<Animator>();
            if (anim == null && r.searchInChildren)
                anim = r.target.GetComponentInChildren<Animator>(true);

            if (anim == null)
            {
                if (verboseLog)
                    Debug.LogWarning($"[AnimTriggerManager] Animator not found for receiver '{r.label}' on '{r.target?.name}'");
                continue;
            }

            r.cachedAnimator = anim;
            foreach (var name in r.allowedTriggers)
            {
                if (!string.IsNullOrEmpty(name))
                    r.triggerHashSet.Add(Animator.StringToHash(name));
            }

            _animIdToReceiver[anim.GetInstanceID()] = r;

            if (!string.IsNullOrEmpty(r.label))
            {
                // 동일 라벨 중복은 마지막 등록으로 덮어씀
                _labelToReceiver[r.label] = r;
            }
        }
    }

    // =========================
    // 공개 API
    // =========================

    /// <summary>Animator 참조로 트리거 발사</summary>
    public bool Fire(Animator anim, string triggerName, float? cooldown = null)
    {
        if (anim == null || string.IsNullOrEmpty(triggerName)) return false;
        int trigHash = Animator.StringToHash(triggerName);
        return Fire(anim, trigHash, cooldown);
    }

    /// <summary>GameObject로 트리거 발사 (Animator를 내부에서 찾아 1회 캐싱된 것 사용)</summary>
    public bool Fire(GameObject target, string triggerName, float? cooldown = null)
    {
        if (target == null || string.IsNullOrEmpty(triggerName)) return false;
        var anim = GetAnimatorFromRegistered(target);
        if (anim == null) anim = target.GetComponentInChildren<Animator>(true);
        if (anim == null) { if (verboseLog) Debug.LogWarning($"[AnimTriggerManager] No Animator on '{target.name}'"); return false; }

        int trigHash = Animator.StringToHash(triggerName);
        return Fire(anim, trigHash, cooldown);
    }

    /// <summary>라벨로 등록한 대상에게 트리거 발사</summary>
    public bool FireByLabel(string receiverLabel, string triggerName, float? cooldown = null)
    {
        if (string.IsNullOrEmpty(receiverLabel) || string.IsNullOrEmpty(triggerName)) return false;
        if (!_labelToReceiver.TryGetValue(receiverLabel, out var r) || r.cachedAnimator == null) return false;

        int trigHash = Animator.StringToHash(triggerName);
        return Fire(r.cachedAnimator, trigHash, cooldown);
    }

    /// <summary>런타임에 외부에서 대상/허용트리거를 등록하거나 갱신</summary>
    public void RegisterOrUpdate(GameObject target, IEnumerable<string> allowedTriggers, string label = null, bool searchInChildren = true)
    {
        if (target == null) return;

        var anim = target.GetComponent<Animator>();
        if (anim == null && searchInChildren)
            anim = target.GetComponentInChildren<Animator>(true);
        if (anim == null) return;

        var r = new Receiver
        {
            label = label,
            target = target,
            searchInChildren = searchInChildren,
            cachedAnimator = anim,
            allowedTriggers = new List<string>()
        };

        if (allowedTriggers != null)
        {
            foreach (var n in allowedTriggers)
            {
                if (!string.IsNullOrEmpty(n))
                {
                    r.allowedTriggers.Add(n);
                    r.triggerHashSet.Add(Animator.StringToHash(n));
                }
            }
        }

        _animIdToReceiver[anim.GetInstanceID()] = r;
        if (!string.IsNullOrEmpty(label)) _labelToReceiver[label] = r;

        if (verboseLog) Debug.Log($"[AnimTriggerManager] Registered '{label ?? anim.gameObject.name}'");
    }

    // =========================
    // 내부
    // =========================

    private bool Fire(Animator anim, int trigHash, float? cooldown)
    {
        if (anim == null) return false;

        // strict 모드면 인스펙터에서 허용한 트리거만 통과
        if (strictMode)
        {
            if (!_animIdToReceiver.TryGetValue(anim.GetInstanceID(), out var r) || r.cachedAnimator == null)
            {
                if (verboseLog) Debug.LogWarning($"[AnimTriggerManager] StrictMode: receiver not registered for '{anim.gameObject.name}'");
                return false;
            }
            if (r.triggerHashSet.Count > 0 && !r.triggerHashSet.Contains(trigHash))
            {
                if (verboseLog) Debug.LogWarning($"[AnimTriggerManager] StrictMode: trigger not allowed '{anim.gameObject.name}' : {trigHash}");
                return false;
            }
        }

        // 디바운스
        float cd = cooldown ?? defaultCooldown;
        if (cd > 0f)
        {
            long key = ComposeKey(anim.GetInstanceID(), trigHash);
            float now = Time.time;
            if (_lastFired.TryGetValue(key, out var last) && (now - last) < cd)
                return false;
            _lastFired[key] = now;
        }

        anim.ResetTrigger(trigHash);
        anim.SetTrigger(trigHash);

        if (verboseLog) Debug.Log($"[AnimTriggerManager] Fire → {anim.gameObject.name} :: {trigHash}");
        return true;
    }

    private Animator GetAnimatorFromRegistered(GameObject target)
    {
        // 같은 Animator를 가리키는지 검사
        foreach (var kv in _animIdToReceiver)
        {
            var r = kv.Value;
            if (r != null && r.cachedAnimator != null && r.cachedAnimator.gameObject == target)
                return r.cachedAnimator;
        }
        return null;
    }

    private static long ComposeKey(int animId, int trigHash)
        => ((long)animId << 32) ^ (uint)trigHash;
}
