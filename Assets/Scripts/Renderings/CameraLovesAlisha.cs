using System;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

using DG.Tweening;
using System.Collections.Generic;
//using UnityEditor.Experimental.GraphView;

public class CameraLovesAlisha : MonoBehaviour
{
    public static CameraLovesAlisha Instance;





    [Header("Follow Depth Lock")]
    [Tooltip("타깃-카메라 간 'forward 방향 거리'를 고정할지 여부")]
    public bool lockFollowDepth = true;

    [Tooltip("고정할 깊이 값(카메라 forward 방향). 씬 시작 시 한 번 보정 가능")]
    public float followDepth = 10f;

    [Tooltip("Awake에서 현재 타깃 기준으로 자동 보정할지")]
    public bool autoCalibrateFollowDepth = true;





    [Header("Point1: Auto-hide marked children")]
    public bool autoHideMarkedChildren = true;

    // 클래스 상단 필드 근처에 추가
    [Header("[MOD] Cutscene 'Wait!' Sign")]
    [Tooltip("Cutscene 캔버스의 'Wait!' 이미지(알파 0~1로 페이드)")]
    public UnityEngine.UI.Image waitImage;
    [Tooltip("Wait! 기본 페이드 인/홀드/아웃 (초)")]
    public float waitFadeIn = 0.08f, waitHold = 0.25f, waitFadeOut = 0.12f;

    private readonly List<CineHideDuringPoint1> _autoHidden = new();
    [Header("Point1 Backdrop & Isolation")]
    [Tooltip("오버레이 카메라(URP, Render Type=Overlay, CullingMask=Cine_Attacker)")]
    public Camera overlayCamera;
    [Tooltip("백드롭 캔버스(Screen Space - Camera, Target=메인 카메라)")]
    public Canvas backdropCanvas;
    [Tooltip("캔버스 안의 풀스크린 Image")]
    public Image backdropImage;
    [Tooltip("공격자만 렌더할 때 쓸 레이어 이름(씬에서 미리 만들어둘 것)")]
    public string isolateLayerName = "Cine_Attacker";
    [Tooltip("오버레이/백드롭 타임스케일 무시")]
    public bool backdropUseRealtime = true;

    [Header("Point1 Framing (Viewport Anchors)")]
    [Tooltip("포인트1에서 인물을 화면의 특정 위치(뷰포트 좌표)에 두는 기능 사용")]
    public bool point1UseFraming = true;

    // flipX == false(오른쪽 바라봄)일 때 인물 위치 (우하단 느낌)
    public Vector2 point1AnchorFacingRight = new Vector2(0.78f, 0.30f);
    // flipX == true(왼쪽 바라봄)일 때 인물 위치 (좌하단 느낌)
    public Vector2 point1AnchorFacingLeft = new Vector2(0.22f, 0.30f);

    // 미세조정(월드 유닛). 필요 없으면 (0,0)
    public Vector2 point1WorldNudge = Vector2.zero;

    [Header("CutLine FX (Point1 전용)")]
    public GameObject cutLineGO;
    public Animator cutLineAnimator;
    public Image cutLineImage;

    [Header("CutLine Facing Angle")]
    [Tooltip("flipX=true(왼쪽), false(오른쪽)일 때 CutLine의 Z 회전각(도)")]
    public float cutLineZRotWhenFacingLeft = 5f;
    public float cutLineZRotWhenFacingRight = -5f;

    [Tooltip("flipX=true(왼쪽), false(오른쪽)일 때 CutLine의 Y 회전각(도)")]
    public float cutLineYRotWhenFacingLeft = 60f;
    public float cutLineYRotWhenFacingRight = -60f;

    [Tooltip("현재 로컬 회전에 위 값을 덧셈(오프셋)할지 여부. 끄면 절대값으로 세팅")]
    public bool cutLineAngleAsOffset = false;

    [Tooltip("Animator Trigger 이름 (Idle→Play)")]
    public string cutLineTrigger = "Trig_Play";

    [Tooltip("Animator Play 상태 이름(트리거가 없을 때 사용)")]
    public string cutLineStateName = "Play";

    [Tooltip("Play에 사용하는 클립(길이 자동 맞춤용, 비워두면 1초 가정)")]
    public AnimationClip cutLineClip;

    [Tooltip("CutLine 애니 길이를 Prep 정지 시간에 맞게 자동 스케일")]
    public bool cutLineMatchHold = true;

    [Tooltip("CutLine Animator를 UnscaledTime으로 갱신 (Prep 정지 실시간 대기와 맞춤)")]
    public bool cutLineUseUnscaled = true;

    [Tooltip("CutLine 이미지 페이드 인/아웃")]
    public float cutLineFadeIn = 0.08f;
    public float cutLineFadeOut = 0.10f;

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

    // [MOD] Guard Prelude - Wait Pause & Shake
    [Header("[MOD] Guard Prelude - Wait Pause & Shake")]
    [Tooltip("Wait!가 뜬 뒤 카메라가 정지할 시간(초)")]
    public float guardWaitPauseSec = 0.6f;
    [Tooltip("정지 동안 Wait 이미지를 흔들 것인지")]
    public bool guardWaitShake = true;
    [Tooltip("스케일 펌핑 강도(0~0.25 권장)")]
    [Range(0f, 0.25f)] public float guardWaitShakeScale = 0.06f;
    [Tooltip("Z축 회전 펀치 각도(도)")]
    [Range(0f, 25f)] public float guardWaitShakeRot = 8f;
    [Tooltip("펀치/쉐이크 진동 횟수")]
    public int guardWaitShakeVibrato = 8;
    [Tooltip("펀치 탄성(0~1)")]
    [Range(0f, 1f)] public float guardWaitShakeElasticity = 0.75f;



    [Header("Point1 Overlay (추가 연출)")]
    [Tooltip("Point1용 오버레이 이미지(UI) - 백드롭과 동일 Canvas 하위에 배치")]
    public Image point1OverlayImage;       // <- 에디터에 같은 캔버스의 Image 할당
    public CanvasGroup point1OverlayGroup; // <- 같은 오브젝트나 상위에 CanvasGroup 할당 권장

    private Tweener _ovTween;


    private Camera _cam;
    private bool _cinematicLock = false;
    private Tweener _moveTW, _fovTW, _shakeTW, _bdFadeTW;
    private Transform _defaultTargetCache;

    private readonly List<(Transform t, int layer)> _layerBackup = new();
    private bool _isIsolationActive = false;
    private int _isolateLayer = -1;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _cam = GetComponentInChildren<Camera>();
        if (_cam == null) _cam = Camera.main;

        if (backdropCanvas != null) backdropCanvas.enabled = false;
        if (overlayCamera != null) overlayCamera.gameObject.SetActive(false);

        _isolateLayer = LayerMask.NameToLayer(isolateLayerName);
        if (_isolateLayer < 0)
            Debug.LogWarning($"[CameraLovesAlisha] 레이어 '{isolateLayerName}' 가 존재하지 않습니다.");

        if (cutLineImage != null)
        {
            var c0 = cutLineImage.color; c0.a = 0f; cutLineImage.color = c0;
        }

        // ★ Wait 이미지는 기본 비활성화(애니메이터가 알아서 켜짐/꺼짐 제어)
        if (waitImage != null) waitImage.gameObject.SetActive(false);

        if (autoCalibrateFollowDepth && target != null && _cam != null)
        {
            var tp = target.position + offset;
            followDepth = Mathf.Max(0.01f, DepthAlongCamera(tp));
        }
    }


    // --- 신규: 카메라-공간 깊이(Forward Dot) 계산 ---
    private float DepthAlongCamera(Vector3 worldPoint)
    {
        if (_cam == null) return 10f;
        Vector3 camToPoint = worldPoint - _cam.transform.position;
        float depth = Vector3.Dot(camToPoint, _cam.transform.forward);
        return Mathf.Max(0.01f, depth);
    }

    void LateUpdate()
    {
        if (_cinematicLock) return;
        if (target == null) return;

        Vector3 targetPoint = target.position + offset;
        float depth = lockFollowDepth ? followDepth : DepthAlongCamera(targetPoint);
        Vector3 desiredCamPos = targetPoint - (_cam.transform.forward * depth);

        Vector3 smoothed = Vector3.Lerp(transform.position, desiredCamPos, smoothSpeed);
        transform.position = smoothed;

        ResetTiltZ(); // 롤 고정 유지(원하면 주석 처리)
    }

    // ---------- Public API ----------

    public void SetDefault(Transform attackerDefault)
    {
        _defaultTargetCache = attackerDefault;
        target = attackerDefault;
    }

    /// <summary> Prep 마지막 프레임 정지 시점까지 공격자에게 클로즈업(FOV=15) </summary>
    public void MoveToPoint1_Attacker(Transform attacker, float durationToPrepEnd, float fovPoint1 = 15f)
    {
        if (point1UseFraming && _cam != null && attacker != null)
        {
            // flipX 탐지
            var sr = attacker.GetComponentInChildren<SpriteRenderer>();
            bool flipX = (sr != null && sr.flipX);

            var anchor = flipX ? point1AnchorFacingLeft : point1AnchorFacingRight;
            FocusWithViewportAnchor(attacker, anchor, point1WorldNudge, fovPoint1, durationToPrepEnd);
        }
        else
        {
            // 기존 방식(정중앙) 유지
            Focus(attacker, fovPoint1, durationToPrepEnd);
        }
    }

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
        _moveTW?.Kill(); _fovTW?.Kill();

        Vector3 targetPoint = (t ? t.position : transform.position) + offset;
        float depth = lockFollowDepth ? followDepth : DepthAlongCamera(targetPoint);
        Vector3 dest = targetPoint - (_cam.transform.forward * depth);

        _moveTW = transform.DOMove(dest, Mathf.Max(0f, duration))
                           .SetEase(Ease.InOutSine)
                           .SetUpdate(true);

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

        if (overlayCamera != null)
        {
            if (_cam.orthographic)
                DOTween.To(() => overlayCamera.orthographicSize, v => overlayCamera.orthographicSize = v, fov, Mathf.Max(0f, duration))
                       .SetEase(Ease.InOutSine).SetUpdate(true);
            else
                DOTween.To(() => overlayCamera.fieldOfView, v => overlayCamera.fieldOfView = v, fov, Mathf.Max(0f, duration))
                       .SetEase(Ease.InOutSine).SetUpdate(true);
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

        if (overlayCamera != null)
        {
            var seq2 = DOTween.Sequence().SetUpdate(true).SetEase(Ease.OutQuad);
            if (_cam.orthographic)
            {
                float start2 = overlayCamera.orthographicSize, mid2 = start2 + delta;
                seq2.Append(DOTween.To(() => overlayCamera.orthographicSize, v => overlayCamera.orthographicSize = v, mid2, duration * 0.5f));
                seq2.Append(DOTween.To(() => overlayCamera.orthographicSize, v => overlayCamera.orthographicSize = v, start2, duration * 0.5f));
            }
            else
            {
                float start2 = overlayCamera.fieldOfView, mid2 = start2 + delta;
                seq2.Append(DOTween.To(() => overlayCamera.fieldOfView, v => overlayCamera.fieldOfView = v, mid2, duration * 0.5f));
                seq2.Append(DOTween.To(() => overlayCamera.fieldOfView, v => overlayCamera.fieldOfView = v, start2, duration * 0.5f));
            }
        }
    }

    // ========== Point1: 공격자만 보이게 + 백드롭 ==========
    public void BeginPoint1Backdrop(GameObject attackerRoot, Sprite bg, float fadeIn = 0.15f, float holdWindowSec = -1f)
    {
        if (_isIsolationActive) return;
        if (overlayCamera == null || backdropCanvas == null || backdropImage == null)
        {
            Debug.LogWarning("[CameraLovesAlisha] overlayCamera/backdropCanvas/backdropImage 세팅 필요");
            return;
        }
        if (_isolateLayer < 0)
        {
            Debug.LogWarning($"[CameraLovesAlisha] '{isolateLayerName}' 레이어가 없습니다.");
            return;
        }

        // 1) 백드롭 표시
        backdropImage.sprite = bg;
        var c = backdropImage.color; c.a = 0f; backdropImage.color = c;
        backdropCanvas.enabled = true;
        _bdFadeTW?.Kill();
        _bdFadeTW = backdropImage.DOFade(1f, Mathf.Max(0f, fadeIn))
            .SetUpdate(backdropUseRealtime);

        // 2) 공격자 레이어 격리
        _layerBackup.Clear();
        SetLayerRecursive(attackerRoot.transform, _isolateLayer, _layerBackup);

        // 3) 오버레이 카메라 ON
        overlayCamera.gameObject.SetActive(true);

        // 4) CutLine 트리거
        TriggerCutLine(holdWindowSec, attackerRoot != null ? attackerRoot.transform : null);

        // 5) 마커 붙은 자식 자동 숨김 (원한다면 주석 해제)
        /*
        if (autoHideMarkedChildren && attackerRoot != null)
        {
            _autoHidden.Clear();
            attackerRoot.GetComponentsInChildren(true, _autoHidden);
            foreach (var h in _autoHidden) h.Hide();
        }
        */
        _isIsolationActive = true;
    }
    public void BeginPoint1Overlay(
      Sprite sprite,
      float fadeInSec,
      float holdSec,
      float maxAlpha,
      Vector2 anchoredPos,
      Vector3 localScale
  )
    {
        if (point1OverlayImage == null) return;

        // 스프라이트/트랜스폼 세팅
        point1OverlayImage.sprite = sprite;
        var rt = point1OverlayImage.rectTransform;
        rt.anchoredPosition = anchoredPos;
        rt.localScale = localScale;

        // 알파 제어 대상
        if (point1OverlayGroup == null)
        {
            // fallback: Image color 알파로 제어
            var c = point1OverlayImage.color;
            c.a = 0f; point1OverlayImage.color = c;

            // 트윈
            _ovTween?.Kill();
            _ovTween = point1OverlayImage.DOFade(maxAlpha, Mathf.Max(0f, fadeInSec));
        }
        else
        {
            point1OverlayGroup.alpha = 0f;
            _ovTween?.Kill();
            _ovTween = point1OverlayGroup.DOFade(maxAlpha, Mathf.Max(0f, fadeInSec));
        }

        point1OverlayImage.gameObject.SetActive(true);
    }

    // === 오버레이 종료 ===
    public void EndPoint1Overlay(float fadeOutSec)
    {
        if (point1OverlayImage == null) return;

        if (point1OverlayGroup == null)
        {
            _ovTween?.Kill();
            _ovTween = point1OverlayImage.DOFade(0f, Mathf.Max(0f, fadeOutSec))
                .OnComplete(() => point1OverlayImage.gameObject.SetActive(false));
        }
        else
        {
            _ovTween?.Kill();
            _ovTween = point1OverlayGroup.DOFade(0f, Mathf.Max(0f, fadeOutSec))
                .OnComplete(() => point1OverlayImage.gameObject.SetActive(false));
        }
    }
    public void HideEmAll(GameObject attackerRoot)
    {
        if (autoHideMarkedChildren && attackerRoot != null)
        {
            _autoHidden.Clear();
            attackerRoot.GetComponentsInChildren(true, _autoHidden);
            foreach (var h in _autoHidden) h.Hide();
        }
    }

    // CameraLovesAlisha.cs 내부 메서드 아무 곳에 추가
    public float ShowGuardWait(float? overridePauseSec = null)
    {
        if (waitImage == null) return 0f;

        // 1) 표시 시간 결정 (인스펙터에서 조절 가능)
        float pause = Mathf.Max(0f, overridePauseSec ?? guardWaitPauseSec);

        // 2) 기존 ShowWaitSign 재활용: hold = pause 로 설정 (타임스케일 무시)
        ShowWaitSign(pause);

        // 3) "두두두둥" 쉐이크 (스케일 + 회전 펀치)
        if (guardWaitShake && pause > 0f)
        {
            var rt = waitImage.rectTransform;
            if (rt != null)
            {
                // 이전 트윈 정리 + 원상복구
                rt.DOKill();
                rt.localScale = Vector3.one;
                rt.localRotation = Quaternion.identity;

                // 스케일 쉐이크
                rt.DOShakeScale(
                    duration: pause,
                    strength: new Vector3(guardWaitShakeScale, guardWaitShakeScale, 0f),
                    vibrato: guardWaitShakeVibrato,
                    randomness: 0f,
                    fadeOut: false
                ).SetUpdate(backdropUseRealtime);

                // 회전 펀치
                rt.DOPunchRotation(
                    punch: new Vector3(0f, 0f, guardWaitShakeRot),
                    duration: pause,
                    vibrato: guardWaitShakeVibrato,
                    elasticity: guardWaitShakeElasticity
                ).SetUpdate(backdropUseRealtime)
                 .OnComplete(() =>
                 {
                     if (rt != null)
                     {
                         rt.localScale = Vector3.one;
                         rt.localRotation = Quaternion.identity;
                     }
                 });
            }
        }

        // 호출자가 이 시간을 ‘실제로 대기’할 수 있도록 반환
        return pause;
    }

    private void TriggerCutLine(float windowSec, Transform attacker)
    {
        if (cutLineGO == null) return;
        // --- 0) flipX 확인 ---
        bool flipX = false;
        if (attacker != null)
        {
            var sr = attacker.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) flipX = sr.flipX;
        }

        // --- 1) CutLine 회전 세팅 ---
        var rt = cutLineGO.transform as RectTransform; // UI라면 RectTransform일 것
        if (rt != null)
        {
            float targetZ = flipX ? cutLineZRotWhenFacingLeft : cutLineZRotWhenFacingRight;
            float targetY = flipX ? cutLineYRotWhenFacingLeft : cutLineYRotWhenFacingRight;

            if (cutLineAngleAsOffset)
            {
                // 기존 로컬 각도에 더해주기
                var e = rt.localEulerAngles;
                rt.localRotation = Quaternion.Euler(e.x, e.y + targetY, e.z + targetZ);
            }
            else
            {
                // 절대값으로 세팅 (X는 건드리지 않음)
                var e = rt.localEulerAngles;
                rt.localRotation = Quaternion.Euler(e.x, targetY, targetZ);
            }
        }

        // 2) 보이게 준비
        cutLineGO.SetActive(true);
        if (cutLineImage != null)
        {
            var c = cutLineImage.color; c.a = 0f; cutLineImage.color = c;
            cutLineImage.DOFade(1f, cutLineFadeIn).SetUpdate(backdropUseRealtime);
        }

        // 3) 애니 길이 매칭/재생
        if (cutLineAnimator != null)
        {
            if (cutLineUseUnscaled) cutLineAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;

            float clipLen = 1f;
            if (cutLineClip != null) clipLen = Mathf.Max(0.01f, cutLineClip.length);

            // Prep 정지 구간(holdWindowSec)에 길이 맞추기 (옵션)
            if (cutLineMatchHold && windowSec > 0.01f && clipLen > 0.01f)
            {
                cutLineAnimator.speed = Mathf.Max(0.01f, clipLen / windowSec);
            }
            else
            {
                cutLineAnimator.speed = 1f;
            }

            if (!string.IsNullOrEmpty(cutLineTrigger))
                cutLineAnimator.SetTrigger(cutLineTrigger);
            else if (!string.IsNullOrEmpty(cutLineStateName))
                cutLineAnimator.Play(cutLineStateName, 0, 0f);
        }
    }

    public void EndPoint1Backdrop(float fadeOut = 0.12f)
    {
        // === 0) CutLine 관련 정리: 항상 수행 ===
        if (cutLineImage != null)
        {
            cutLineImage.DOFade(0f, Mathf.Max(0f, cutLineFadeOut))
                        .SetUpdate(backdropUseRealtime);
        }
        if (cutLineAnimator != null)
        {
            // 혹시 속도 조절해뒀다면 원복
            cutLineAnimator.speed = 1f;
        }

        // === 0.5) 자동 숨김 복구: 항상 수행 ===
        for (int i = 0; i < _autoHidden.Count; i++)
            if (_autoHidden[i] != null) _autoHidden[i].Show();
        _autoHidden.Clear();

        //  셀렉션바는 화면에서 보이지 않게 ‘대기 상태’로
        SelectionBarManager.Instance?.PrepareHiddenStandby();

        // === 1) 백드롭/오버레이/레이어 복구: 실제로 켠 경우에만 수행 ===
        if (_isIsolationActive && overlayCamera != null && backdropCanvas != null && backdropImage != null)
        {
            // 백드롭 페이드아웃
            _bdFadeTW?.Kill();
            _bdFadeTW = backdropImage.DOFade(0f, Mathf.Max(0f, fadeOut))
                .SetUpdate(backdropUseRealtime)
                .OnComplete(() =>
                {
                    if (backdropCanvas != null) backdropCanvas.enabled = false;
                });

            // 오버레이 카메라 끄기
            overlayCamera.gameObject.SetActive(false);

            // 레이어 복구
            RestoreLayers(_layerBackup);
            _layerBackup.Clear();

            _isIsolationActive = false;
        }
    }

    private void SetLayerRecursive(Transform root, int layer, List<(Transform, int)> backup)
    {
        if (root == null) return;
        backup.Add((root, root.gameObject.layer));
        root.gameObject.layer = layer;
        for (int i = 0; i < root.childCount; i++)
            SetLayerRecursive(root.GetChild(i), layer, backup);
    }

    private void RestoreLayers(List<(Transform t, int layer)> backup)
    {
        for (int i = 0; i < backup.Count; i++)
        {
            var (t, l) = backup[i];
            if (t != null) t.gameObject.layer = l;
        }
    }

    public void UnhideAutoHiddenNow()
    {
        // 마커로 숨겨둔 애들은 백드롭 사용 여부와 무관하게 무조건 복구
        for (int i = 0; i < _autoHidden.Count; i++)
            if (_autoHidden[i] != null) _autoHidden[i].Show();
        _autoHidden.Clear();
    }

    private void ResetTiltZ()
    {
        // 화면 기울어짐 방지: Z 회전만 0으로
        var e = transform.eulerAngles;
        e.z = 0f;
        transform.eulerAngles = e;
    }

    /// <summary>
    /// 대상이 주어진 뷰포트 좌표(anchor.x, anchor.y)에 보이도록
    /// 카메라 중심을 계산해서 트윈. (0~1: 좌->우, 하->상)
    /// </summary>
    private void FocusWithViewportAnchor(
        Transform t, Vector2 anchor, Vector2 worldNudge,
        float fov, float duration, Action onComplete = null)
    {
        _cinematicLock = true;
        _moveTW?.Kill(); _fovTW?.Kill();

        Vector3 desiredTarget = (t ? t.position : transform.position) + offset
                                + new Vector3(worldNudge.x, worldNudge.y, 0f);

        float depth = lockFollowDepth ? followDepth : DepthAlongCamera(desiredTarget);

        // 카메라 기준 anchor 지점의 월드 좌표(깊이는 고정/또는 자동)
        Vector3 worldAtAnchor = _cam != null
            ? _cam.ViewportToWorldPoint(new Vector3(anchor.x, anchor.y, depth))
            : desiredTarget;

        Vector3 dest = transform.position + (desiredTarget - worldAtAnchor);

        _moveTW = transform.DOMove(dest, Mathf.Max(0f, duration))
                           .SetEase(Ease.InOutSine)
                           .SetUpdate(true);

        // FOV 트윈(메인/오버레이)
        if (_cam != null)
        {
            if (_cam.orthographic)
                _fovTW = DOTween.To(() => _cam.orthographicSize, v => _cam.orthographicSize = v, fov, Mathf.Max(0f, duration))
                                .SetEase(Ease.InOutSine).SetUpdate(true);
            else
                _fovTW = DOTween.To(() => _cam.fieldOfView, v => _cam.fieldOfView = v, fov, Mathf.Max(0f, duration))
                                .SetEase(Ease.InOutSine).SetUpdate(true);
        }
        if (overlayCamera != null)
        {
            if (_cam != null && _cam.orthographic)
                DOTween.To(() => overlayCamera.orthographicSize, v => overlayCamera.orthographicSize = v, fov, Mathf.Max(0f, duration))
                       .SetEase(Ease.InOutSine).SetUpdate(true);
            else
                DOTween.To(() => overlayCamera.fieldOfView, v => overlayCamera.fieldOfView = v, fov, Mathf.Max(0f, duration))
                       .SetEase(Ease.InOutSine).SetUpdate(true);
        }

        if (onComplete != null)
        {
            DOTween.Sequence().SetUpdate(true)
                  .AppendInterval(Mathf.Max(0f, duration))
                  .AppendCallback(() => onComplete());
        }
    }

    /// <summary>
    /// [MOD] Guard 프롤로그 'Wait!' 연출: 페이드 제거, 즉시 활성화만.
    /// hold 시간은 호출부(ShowGuardWait) 계산용으로 그대로 전달/사용.
    /// </summary>
    public void ShowWaitSign(float? overrideHoldSec = null)
    {
        if (waitImage == null) return;

        // 애니메이터가 알아서 재생/비활성화하므로 즉시 켜주기만 함.
        var go = waitImage.gameObject;
        if (!go.activeSelf) go.SetActive(true);

        // 필요 시, 애니메이터 트리거를 여기서 쏠 수도 있음 (선택)
        // var anim = waitImage.GetComponent<Animator>();
        // if (anim) anim.SetTrigger("Trig_Show"); 
    }


    // 1) 내부용: 로컬 플레이어 트랜스폼 찾아오기
    private Transform ResolveLocalPlayerTransform()
    {
        try
        {
            var ls = LocalState.Instance;
            if (ls != null && ls.PlayerObDic != null &&
                ls.PlayerObDic.TryGetValue(PhotonNetwork.LocalPlayer.ActorNumber, out var go) && go != null)
                return go.transform;
        }
        catch { /* no-op */ }
        return null;
    }

    // 2) 공개용: 로컬 플레이어에게 카메라 복귀
    public void ReturnToLocal(float? durationOverride = null)
    {
        // 없으면 기존 디폴트/현재 타겟으로 폴백
        var t = ResolveLocalPlayerTransform() ?? _defaultTargetCache ?? target;
        ReturnToDefault(durationOverride, t);
    }
}
