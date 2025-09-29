using System.Collections;
using TMPro;
using UnityEngine;
using Photon.Pun;

public class LocalCycleManager : MonoBehaviour
{
    public static LocalCycleManager Instance { get; private set; }

    [Header("Refs (assign in Inspector)")]
    [SerializeField] private TextMeshProUGUI cycleText;   // 배너에 출력할 TMPUGUI
    [SerializeField] private TextMeshProUGUI UIcycleText;   // UI에 출력할 TMPUGUI

    [SerializeField] private GameObject CycleBarObject;   // 배너 루트 오브젝트
    [SerializeField] private Animator cycleBarAnimator;   // 없으면 런타임에 CycleBarObject에서 탐색
    [SerializeField] private int CurrentCycle;            // 현재 싸이클(표시용/디버그)

    [Header("Timing (seconds)")]
    [Tooltip("2단계: 텍스트 표시까지 대기 N초")]
    [SerializeField] private float delayBeforeCycleText = 0.5f;  // N

    [Tooltip("3단계: 타이핑(전체 글자 등장까지) K초")]
    [SerializeField] private float typingDuration = 0.5f;        // K

    [Tooltip("4단계: 타이핑 완료 후 J초 대기")]
    [SerializeField] private float afterTypingDelay = 0.8f;      // J

    [Header("Animator")]
    [Tooltip("끝낼 때 쏠 트리거 이름")]
    [SerializeField] private string barEndTrigger = "Bar_End";

    [Tooltip("애니메이터 레이어 인덱스")]
    [SerializeField] private int animatorLayer = 0;

    [Tooltip("애니 종료 대기 최대 시간(초)")]
    [SerializeField] private float endAnimMaxWait = 3f;

    [Header("Misc")]
    [Tooltip("타임스케일 무시(연출이 시간정지 중에도 재생되도록)")]
    [SerializeField] private bool useUnscaledTime = true;

    [Tooltip("씬 전환 시에도 유지할지")]
    [SerializeField] private bool dontDestroyOnLoad = false;



    private Coroutine _routine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);
    }

    private void OnValidate()
    {
        if (cycleBarAnimator == null && CycleBarObject != null)
            cycleBarAnimator = CycleBarObject.GetComponentInChildren<Animator>(true);
    }

    /// <summary>
    /// 외부 호출 API: 싸이클 배너 연출을 실행한다.
    /// newcycle==0 → "{faceoffCount}번째 페이스 오프"
    /// newcycle==내 ActorNumber → "나의 싸이클"
    /// 그 외 → "상대의 싸이클"
    /// </summary>
    public void PlayCycleBanner(int newcycle, int faceoffCount)
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(PlayCycleBannerCo(newcycle, faceoffCount));
    }

    private IEnumerator PlayCycleBannerCo(int newcycle, int faceoffCount)
    {
        string temp_text;
        if (newcycle == 0)
            temp_text = $"{faceoffCount}번째 페이스 오프";

       else if (newcycle == -1)
            temp_text = $"쇼다운";

        else
        {

            int my = PhotonNetwork.LocalPlayer.ActorNumber;
            if (newcycle == my)
                temp_text = "나의 싸이클";

            else
                temp_text = "상대의 싸이클";

        }
        UIcycleText.text = temp_text;

        if (cycleText != null)
        {
            cycleText.maxVisibleCharacters = 0;
            cycleText.SetText(string.Empty);
            cycleText.ForceMeshUpdate(true, true); // 즉시 메쉬 갱신
        }

        // 1) 배너 활성화
        if (CycleBarObject != null && !CycleBarObject.activeSelf)
            CycleBarObject.SetActive(true);

        if (cycleBarAnimator == null && CycleBarObject != null)
            cycleBarAnimator = CycleBarObject.GetComponentInChildren<Animator>(true);

        CurrentCycle = newcycle;

        // 2) N초 대기 후 텍스트 출력 시작
        if (delayBeforeCycleText > 0f)
            yield return WaitForSecondsX(delayBeforeCycleText);

        // 문구 결정
        string msg = BuildCycleText(newcycle, faceoffCount);

        // 3) 타이핑 연출 (K초)
        if (cycleText != null)
        {
            cycleText.text = msg;
            cycleText.ForceMeshUpdate();
            int total = cycleText.textInfo.characterCount;
            cycleText.maxVisibleCharacters = 0;

            if (typingDuration <= 0f || total <= 0)
            {
                cycleText.maxVisibleCharacters = total; // 즉시 출력
            }
            else
            {
                float t = 0f;
                while (t < typingDuration)
                {
                    t += DeltaTimeX();
                    int vis = Mathf.Clamp(Mathf.FloorToInt(Mathf.Lerp(0, total, t / typingDuration)), 0, total);
                    cycleText.maxVisibleCharacters = vis;
                    yield return null;
                }
                cycleText.maxVisibleCharacters = total;
            }
        }

        // 4) 타이핑 완료 후 J초 대기 → 텍스트 삭제 → 애니 트리거 → 애니 종료 대기 → 비활성화
        if (afterTypingDelay > 0f)
            yield return WaitForSecondsX(afterTypingDelay);

        if (cycleText != null)
        {
            cycleText.text = string.Empty;
            cycleText.maxVisibleCharacters = 0;
        }

        if (cycleBarAnimator != null && !string.IsNullOrEmpty(barEndTrigger))
        {
            cycleBarAnimator.ResetTrigger(barEndTrigger);
            cycleBarAnimator.SetTrigger(barEndTrigger);
            yield return WaitForAnimationEnd(cycleBarAnimator, animatorLayer, endAnimMaxWait);
        }

        if (CycleBarObject != null)
            CycleBarObject.SetActive(false);

        // 5) 모든 것이 끝난 뒤에만 로그
        Debug.Log("다끝");

        _routine = null;

       

        Overmind.Instance.SubmintRenderingDone(PhotonNetwork.LocalPlayer.ActorNumber);
            }

    private string BuildCycleText(int newcycle, int faceoffCount)
    {
        if (newcycle == 0)
            return $"{faceoffCount}번째 페이스 오프";
        else if (newcycle == -1)
            return $"쇼다운";

        int my = PhotonNetwork.LocalPlayer.ActorNumber;
        if (newcycle == my)
            return "나의 싸이클";

        return "상대의 싸이클";
    }

    /// <summary> 애니메이션 1사이클 종료(또는 상태 전환)까지 대기. 상태/태그 몰라도 동작하도록 범용. </summary>
    private IEnumerator WaitForAnimationEnd(Animator anim, int layer, float maxWait)
    {
        if (anim == null) yield break;

        // 트리거 직후 새 상태 진입 유예
        yield return null;
        yield return null;

        float start = TimeX();
        AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(layer);
        int watchedHash = info.fullPathHash;

        // 우선 "새 상태로 바뀔 때"까지 기다린 뒤 그 상태의 1사이클을 본다
        float phase1Deadline = start + maxWait * 0.33f;
        while (TimeX() < phase1Deadline)
        {
            if (!anim.IsInTransition(layer))
            {
                var cur = anim.GetCurrentAnimatorStateInfo(layer);
                if (cur.fullPathHash != watchedHash) { watchedHash = cur.fullPathHash; break; }
            }
            yield return null;
        }

        // 본 상태 1사이클(or 전환) 완료 대기
        float deadline = start + maxWait;
        while (TimeX() < deadline)
        {
            if (anim.IsInTransition(layer)) { yield return null; continue; }
            info = anim.GetCurrentAnimatorStateInfo(layer);

            // 다른 상태로 바뀌면 직전 상태 종료로 간주
            if (info.fullPathHash != watchedHash) break;

            // 1사이클 종료
            if (info.normalizedTime >= 0.99f) break;

            yield return null;
        }
    }

    // --- 유틸: 스케일 무시 옵션 ---
    private float DeltaTimeX() => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    private float TimeX() => useUnscaledTime ? Time.unscaledTime : Time.time;

    // CS0030 해결: IEnumerator로 만들어서 호출부에서 yield return으로 사용
    private IEnumerator WaitForSecondsX(float t)
    {
        if (useUnscaledTime)
            yield return new WaitForSecondsRealtime(t);
        else
            yield return new WaitForSeconds(t);
    }
}
