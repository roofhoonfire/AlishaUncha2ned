using UnityEngine;
using DG.Tweening;
using System.Collections;
using Photon.Pun;


public class SelectionBarManager : MonoBehaviour
{
    public Transform CM;
    public Transform MM;
    public Transform JM;

    private bool isActive = false;
    private bool isAnimating = false;

    public float rotationDuration = 0.5f; // 회전 시간
    public float overlapDelay = 0.1f;      // 오버랩 간격 (다음 오브젝트가 조금 늦게 시작)

    public static SelectionBarManager Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public  void SetActive()
    {
         if ( !isAnimating)
         isAnimating = true;
       
       if (!isActive)
         ActivateSequence();
     else
        DeactivateSequence();
      
    }

    private void ActivateSequence()
    {
        // 초기 rotation 설정만 하고 비활성화 유지
        CM.localRotation = Quaternion.Euler(0, 90, 0);
        MM.localRotation = Quaternion.Euler(0, 90, 0);
        JM.localRotation = Quaternion.Euler(0, 90, 0);

        // 회전 시작 시점에 활성화
        StartCoroutine(RotateWithDelay(CM, Vector3.zero, 0f, Ease.OutBack, true));
        StartCoroutine(RotateWithDelay(MM, Vector3.zero, overlapDelay, Ease.OutBack, true));
        StartCoroutine(RotateWithDelay(JM, Vector3.zero, overlapDelay * 2, Ease.OutBack, true));

        float totalExpectedTime = rotationDuration + overlapDelay * 2;
        StartCoroutine(EndAnimationAfterDelay(totalExpectedTime, true));
    }

    private void DeactivateSequence()
    {
        // 회전만 실행, 비활성화는 이후 처리
        StartCoroutine(RotateWithDelay(CM, new Vector3(0, -90, 0), 0f, Ease.InBack, false));
        StartCoroutine(RotateWithDelay(MM, new Vector3(0, -90, 0), overlapDelay, Ease.InBack, false));
        StartCoroutine(RotateWithDelay(JM, new Vector3(0, -90, 0), overlapDelay * 2, Ease.InBack, false));

        float totalExpectedTime = rotationDuration + overlapDelay * 2;
        StartCoroutine(EndAnimationAfterDelay(totalExpectedTime, false));
    }

    private IEnumerator RotateWithDelay(Transform target, Vector3 targetRotation, float delay, Ease ease, bool activateOnStart)
    {
        yield return new WaitForSeconds(delay);

        if (activateOnStart)
            target.gameObject.SetActive(true);

        target.DOLocalRotate(targetRotation, rotationDuration).SetEase(ease);
    }

    private IEnumerator EndAnimationAfterDelay(float delay, bool afterActivate)
    {
        yield return new WaitForSeconds(delay);

        if (!afterActivate)
        {
            // 회전 후 비활성화
            CM.gameObject.SetActive(false);
            MM.gameObject.SetActive(false);
            JM.gameObject.SetActive(false);
        }

        isActive = afterActivate;
        isAnimating = false;
    }
}
