using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening; // 사용중이므로 지원. 없다면 관련 라인 주석 처리.

public class HitStopManager : MonoBehaviour
{
    public static HitStopManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// 히트스탑: 선택적으로 특정 애니메이터/씬전체 일시정지. 대기는 실시간으로.
    /// </summary>
    public IEnumerator HitStop(float duration, IEnumerable<Animator> animatorsToPause, bool pauseScene)
    {
        float prevTimeScale = Time.timeScale;

        // 애니메이터 속도/트윈 일시정지
        List<(Animator anim, float prevSpeed)> touched = new List<(Animator, float)>();
        if (animatorsToPause != null)
        {
            foreach (var anim in animatorsToPause)
            {
                if (anim == null) continue;
                touched.Add((anim, anim.speed));
                anim.speed = 0f;

                // DOTween: 해당 오브젝트 트윈 일시정지
                DOTween.Pause(anim.gameObject);
            }
        }

        // 씬 전체 멈춤 (옵션)
        if (pauseScene) Time.timeScale = 0f;

        // 실시간 대기
        float endTime = Time.realtimeSinceStartup + Mathf.Max(0f, duration);
        while (Time.realtimeSinceStartup < endTime)
            yield return null;

        // 복구
        if (pauseScene) Time.timeScale = prevTimeScale;

        foreach (var t in touched)
        {
            if (t.anim != null) t.anim.speed = t.prevSpeed;
            if (t.anim != null) DOTween.Play(t.anim.gameObject);
        }
    }
}
