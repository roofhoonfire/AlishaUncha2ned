// SelectionBarManager.cs
using UnityEngine;
using DG.Tweening;
using System.Collections;

public class SelectionBarManager : MonoBehaviour
{
    public static SelectionBarManager Instance;

    public Transform CM;
    public Transform MM;
    public Transform JM;

    private bool isActive = false;
    private bool isAnimating = false;

    public float rotationDuration = 0.5f;
    public float overlapDelay = 0.1f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ★ 엔드포인트 복구 직후, “화면엔 안 보이는 대기 상태”로 만들기
    public void PrepareHiddenStandby()
    {
        Debug.Log("오케이 이제 액션 끝낫고 셀렉션 바 보여줄 준비합니다");

        // 애니/트윈 전부 정리
        StopAllCoroutines();
        DOTween.Kill(CM); DOTween.Kill(MM); DOTween.Kill(JM);

        // 숨김 자세로 고정 (Y = -90)
        if (CM) CM.localRotation = Quaternion.Euler(0, -90, 0);
        if (MM) MM.localRotation = Quaternion.Euler(0, -90, 0);
        if (JM) JM.localRotation = Quaternion.Euler(0, -90, 0);

        // 화면에서 아예 안 보이게 (활성/비활성 중 선택)
        if (CM) CM.gameObject.SetActive(false);
        if (MM) MM.gameObject.SetActive(false);
        if (JM) JM.gameObject.SetActive(false);

        // 내부 상태도 “숨김/비애니”로 고정
        isActive = false;
        isAnimating = false;
    }

    public void SetActive()
    {
        if (isAnimating) return;
        isAnimating = true;

        if (!isActive) ActivateSequence();
        else DeactivateSequence();
    }

    private void ActivateSequence()
    {

        Debug.Log("오케이 이제 셀렉션바 보여줍니다");
        // 보이기 직전 자세
        if (CM) CM.localRotation = Quaternion.Euler(0, 90, 0);
        if (MM) MM.localRotation = Quaternion.Euler(0, 90, 0);
        if (JM) JM.localRotation = Quaternion.Euler(0, 90, 0);

        StartCoroutine(RotateWithDelay(CM, Vector3.zero, 0f, Ease.OutBack, true));
        StartCoroutine(RotateWithDelay(MM, Vector3.zero, overlapDelay, Ease.OutBack, true));
        StartCoroutine(RotateWithDelay(JM, Vector3.zero, overlapDelay * 2, Ease.OutBack, true));

        float total = rotationDuration + overlapDelay * 2f;
        StartCoroutine(EndAnimationAfterDelay(total, true));
    }

    private void DeactivateSequence()
    {
        Debug.Log("오케이 이제 셀렉션바 가립니다");
        StartCoroutine(RotateWithDelay(CM, new Vector3(0, -90, 0), 0f, Ease.InBack, false));
        StartCoroutine(RotateWithDelay(MM, new Vector3(0, -90, 0), overlapDelay, Ease.InBack, false));
        StartCoroutine(RotateWithDelay(JM, new Vector3(0, -90, 0), overlapDelay * 2, Ease.InBack, false));

        float total = rotationDuration + overlapDelay * 2f;
        StartCoroutine(EndAnimationAfterDelay(total, false));
    }

    private IEnumerator RotateWithDelay(Transform t, Vector3 rot, float delay, Ease ease, bool activateOnStart)
    {
        if (!t) yield break;
        yield return new WaitForSeconds(delay);
        if (activateOnStart) t.gameObject.SetActive(true);
        t.DOLocalRotate(rot, rotationDuration).SetEase(ease);
    }

    private IEnumerator EndAnimationAfterDelay(float delay, bool afterActivate)
    {
        yield return new WaitForSeconds(delay);

        if (!afterActivate)
        {
            if (CM) CM.gameObject.SetActive(false);
            if (MM) MM.gameObject.SetActive(false);
            if (JM) JM.gameObject.SetActive(false);
        }

        isActive = afterActivate;
        isAnimating = false;
    }
}
