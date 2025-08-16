using System;
using UnityEngine;
using DG.Tweening;

public class CameraLovesAlisha : MonoBehaviour
{
    public static CameraLovesAlisha Instance;

    [Header("Follow(default)")]
    public Transform target;
    public Vector3 offset;
    public float smoothSpeed = 0.125f;

    [Header("Cinematic Defaults")]
    [Tooltip("디폴트로 돌아갈 때 목표 FOV")]
    public float defaultFOV = 60f;
    [Tooltip("디폴트로 돌아갈 때 트윈 시간")]
    public float returnDuration = 0.35f;

    [Header("Shake Presets (Inspector 조절)")]
    [Tooltip("K 프레임(피격 시작)에 쓰는 소프트 쉐이크 XY 강도(월드 유닛)")]
    public Vector2 softXY = new Vector2(0.06f, 0.06f);
    public float softDuration = 0.08f;
    public int softVibrato = 14;
    public float softRandomness = 90f;

    [Tooltip("N 프레임(히트스톱 직전/중)에 쓰는 하드 쉐이크 XY 강도")]
    public Vector2 hardXY = new Vector2(0.10f, 0.10f);
    public float hardDuration = 0.14f;
    public int hardVibrato = 18;
    public float hardRandomness = 90f;

    [Header("FOV Punch Presets (Inspector 조절)")]
    [Tooltip("K 프레임용 펀치 (음수면 줌인)")]
    public float softPunchDelta = -2f;
    public float softPunchDuration = 0.10f;
    [Tooltip("N 프레임 해제 리바운드용 펀치 (양수면 줌아웃)")]
    public float hardPunchDelta = +1.8f;
    public float hardPunchDuration = 0.12f;

    private Camera _cam;
    private bool _cinematicLock = false;
    private Tweener _moveTW, _fovTW, _shakeTW;
    private Transform _defaultTargetCache;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _cam = GetComponentInChildren<Camera>();
        if (_cam == null) _cam = Camera.main;
        ResetTiltZ(); // 혹시 남아있던 기울기 정리
    }

    void LateUpdate()
    {
        if (_cinematicLock) return; // 시네매틱 중엔 자리 고정(트윈이 움직임 제어)
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        desiredPosition.z = transform.position.z;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
    }

    // ---------- Public API ----------

    public void SetDefault(Transform attackerDefault)
    {
        _defaultTargetCache = attackerDefault;
        target = attackerDefault;
    }

    /// <summary> Prep 마지막 프레임 정지 시점까지 공격자에게 클로즈업(FOV=15) </summary>
    public void MoveToPoint1_Attacker(Transform attacker, float durationToPrepEnd, float fovPoint1 = 15f)
        => Focus(attacker, fovPoint1, durationToPrepEnd);

    /// <summary> Attack 시작~N프레임 히트스톱 시작까지 피격자에게 이동(FOV=40) </summary>
    public void MoveToPoint2_Victim(Transform victim, float durationToN, float fovPoint2 = 40f)
        => Focus(victim, fovPoint2, durationToN);

    /// <summary> Attack 종료와 동시에 디폴트(공격자)로 복귀(FOV=60) </summary>
    public void ReturnToDefault(float? durationOverride = null, Transform attackerOverride = null)
    {
        float d = durationOverride ?? returnDuration;
        Transform t = attackerOverride ?? _defaultTargetCache ?? target;
        Focus(t, defaultFOV, d, onComplete: () => { _cinematicLock = false; target = t; });
    }

    // ---- Impact helpers (Inspector 프리셋 사용) ----
    public void ShakeSoft() => ShakeXY(softXY, softDuration, softVibrato, softRandomness);
    public void ShakeHard(float extraDuration = 0f) => ShakeXY(hardXY, hardDuration + extraDuration, hardVibrato, hardRandomness);

    public void PunchSoft() => PunchFOV(softPunchDelta, softPunchDuration);
    public void PunchHard() => PunchFOV(hardPunchDelta, hardPunchDuration);

    // ---------- Internal ----------

    private void Focus(Transform t, float fov, float duration, Action onComplete = null)
    {
        _cinematicLock = true;

        _moveTW?.Kill();
        _fovTW?.Kill();

        Vector3 dest = (t ? t.position : transform.position) + offset;
        dest.z = transform.position.z;

        _moveTW = transform.DOMove(dest, Mathf.Max(0f, duration))
                           .SetEase(Ease.InOutSine)
                           .SetUpdate(true); // 실시간

        if (_cam != null)
        {
            if (_cam.orthographic)
            {
                _fovTW = DOTween.To(() => _cam.orthographicSize, v => _cam.orthographicSize = v, fov, Mathf.Max(0f, duration))
                                .SetEase(Ease.InOutSine).SetUpdate(true);
            }
            else
            {
                _fovTW = DOTween.To(() => _cam.fieldOfView, v => _cam.fieldOfView = v, fov, Mathf.Max(0f, duration))
                                .SetEase(Ease.InOutSine).SetUpdate(true);
            }
        }

        if (onComplete != null)
        {
            DOTween.Sequence().SetUpdate(true)
                  .AppendInterval(Mathf.Max(0f, duration))
                  .AppendCallback(() => onComplete());
        }
    }

    /// <summary> XY 포지션만 흔들림. 회전 변경 없음. </summary>
    public void ShakeXY(Vector2 xyStrength, float duration, int vibrato = 18, float randomness = 90f)
    {
        ResetTiltZ();            // 혹시 남은 기울기 제거
        _shakeTW?.Kill();        // 이전 흔들림 정리
        var strength = new Vector3(xyStrength.x, xyStrength.y, 0f);
        _shakeTW = transform.DOShakePosition(duration, strength, vibrato, randomness, false, true)
                            .SetEase(Ease.Linear)
                            .SetUpdate(true);
    }

    /// <summary> FOV 펀치(살짝 들이대기/되돌리기). delta가 음수면 줌인 펀치. </summary>
    public void PunchFOV(float delta, float duration)
    {
        if (_cam == null) return;
        float start = _cam.orthographic ? _cam.orthographicSize : _cam.fieldOfView;
        float mid = start + delta;

        var seq = DOTween.Sequence().SetUpdate(true).SetEase(Ease.OutQuad);
        if (_cam.orthographic)
        {
            seq.Append(DOTween.To(() => _cam.orthographicSize, v => _cam.orthographicSize = v, mid, duration * 0.5f));
            seq.Append(DOTween.To(() => _cam.orthographicSize, v => _cam.orthographicSize = v, start, duration * 0.5f));
        }
        else
        {
            seq.Append(DOTween.To(() => _cam.fieldOfView, v => _cam.fieldOfView = v, mid, duration * 0.5f));
            seq.Append(DOTween.To(() => _cam.fieldOfView, v => _cam.fieldOfView = v, start, duration * 0.5f));
        }
    }

    private void ResetTiltZ()
    {
        // 화면 기울어짐 방지: Z 회전만 0으로
        var e = transform.eulerAngles;
        e.z = 0f;
        transform.eulerAngles = e;
    }
}
