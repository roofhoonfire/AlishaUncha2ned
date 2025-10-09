using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI; // UGUI Graphic용 (선택)

public class PressAnyKeySequenceExtended : MonoBehaviour
{
    [Header("1) 아무 버튼 시: 알파 0으로 페이드아웃")]
    [SerializeField] private List<SpriteRenderer> fadeOutSprites = new();   // SpriteRenderer용
    [SerializeField] private List<Graphic> fadeOutGraphics = new();   // Image/Text 등 UGUI용 (선택)

    [Header("2) 아무 버튼 시: X를 targetX로 이동")]
    [SerializeField] private List<Transform> moveXTargets = new();

    [Header("3) 아무 버튼 시: 활성화 + X스케일 0→1")]
    [SerializeField] private List<Transform> scaleInXTargets = new();

    [Header("4) 아무 버튼 시: 즉시 비활성화")]
    [SerializeField] private List<GameObject> deactivateImmediately = new();

    [Header("Timings / Options")]
    [SerializeField] private float fadeDuration = 0.6f;
    [SerializeField] private float moveDuration = 0.6f;
    [SerializeField] private float scaleDuration = 0.5f;
    [SerializeField] private float scaleStagger = 0.0f;      // 스케일 인 사이 간격(선택)
    [SerializeField] private float targetX = -4f;       // 이동 목표 X
    [SerializeField] private bool useLocalMove = true;      // 로컬 이동 여부
    [SerializeField] private Ease ease = Ease.OutCubic;
    [SerializeField] private bool deactivateThisOnComplete = true; // 시퀀스 끝나면 이 오브젝트 비활성화

    private bool _triggered;

    private void Awake()
    {
        // 스케일 인 대상들은 시작 시 X-스케일 0으로 맞춰 두면 깔끔 (활성화 시점에 한 번 더 보정함)
        foreach (var t in scaleInXTargets)
        {
            if (t == null) continue;
            var s = t.localScale;
            t.localScale = new Vector3(0f, s.y, s.z);
            if (t.gameObject.activeSelf) t.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (_triggered) return;
        if (Input.anyKeyDown)
        {
            _triggered = true;
            Run();
        }
    }

    private void Run()
    {
        // 4) 즉시 비활성화
        foreach (var go in deactivateImmediately)
        {
            if (go != null) go.SetActive(false);
        }

        var seq = DOTween.Sequence();

        // 1) 페이드아웃 (동시에 진행)
        foreach (var sr in fadeOutSprites)
        {
            if (sr == null) continue;
            seq.Join(sr.DOFade(0f, fadeDuration).SetEase(ease));
        }
        foreach (var g in fadeOutGraphics)
        {
            if (g == null) continue;
            seq.Join(g.DOFade(0f, fadeDuration).SetEase(ease));
        }

        // 2) X 이동 (동시에 진행)
        foreach (var tr in moveXTargets)
        {
            if (tr == null) continue;
            if (useLocalMove)
                seq.Join(tr.DOLocalMoveX(targetX, moveDuration).SetEase(ease));
            else
                seq.Join(tr.DOMoveX(targetX, moveDuration).SetEase(ease));
        }

        // 3) 활성화 + X스케일 0→1 (페이드/이동 끝난 뒤에 순차 또는 동시로 진행)
        if (scaleInXTargets.Count > 0)
        {
            float delay = 0f;
            foreach (var t in scaleInXTargets)
            {
                if (t == null) continue;

                // 각 대상마다 활성화 & 초기 스케일 보정
                seq.AppendCallback(() =>
                {
                    var s = t.localScale;
                    t.localScale = new Vector3(0f, s.y, s.z);
                    t.gameObject.SetActive(true);
                });

                // 스케일 인
                seq.Append(t.DOScaleX(1f, scaleDuration).SetEase(Ease.OutBack));

                // 다음 대상과의 간격
                if (scaleStagger > 0f)
                    seq.AppendInterval(scaleStagger);
            }
        }

        seq.OnComplete(() =>
        {
            if (deactivateThisOnComplete)
                gameObject.SetActive(false);
        });
    }
}
