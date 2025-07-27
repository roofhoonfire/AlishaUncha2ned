using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public class ElementRenderer : MonoBehaviour
{
    public static ElementRenderer Instance;
    public GameObject firePrefab;
    public GameObject icePrefab;
    public GameObject windPrefab;
    public GameObject earthPrefab;

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
    public Transform GetElementHolder(int actorNum)
    {
        if (LocalState.Instance.PlayerObDic.TryGetValue(actorNum, out var playerOb))
        {
            return playerOb.transform.Find("Elements");
        }
        return null;
    }
    public void RenderElements(int actorNum, List<apProp> elements)
    {
        Transform holder = GetElementHolder(actorNum);
        if (holder == null)
        {
            Debug.LogWarning($"ElementHolder not found for actor {actorNum}");
            return;
        }

        // 기존 요소 제거
        foreach (Transform child in holder)
        {
            Destroy(child.gameObject);
        }

        // 새 요소 생성
        float offsetX = 0.5f;
        Vector3 targetScale = new Vector3(0.5f, 0.5f, 1f);

        for (int i = 0; i < elements.Count && i < 4; i++)
        {
            GameObject prefabToInstantiate = elements[i] switch
            {
                apProp.elem_fire => firePrefab,
                apProp.elem_ice => icePrefab,
                apProp.elem_wind => windPrefab,
                apProp.elem_earth => earthPrefab,
                _ => null
            };

            if (prefabToInstantiate == null) continue;

            GameObject newElement = Instantiate(prefabToInstantiate, holder);
            newElement.transform.localPosition = new Vector3(i * offsetX, 0, 0);

            // DOTween 등장 연출
            newElement.transform.localScale = Vector3.zero;
            newElement.transform.DOScale(targetScale, 0.3f).SetEase(Ease.OutBack);
        }
    }
    public void RenderElementsFromDiffs(List<LocalRenderingManager.RenderDiff> diffs)
    {
        foreach (var diff in diffs)
        {
            if (diff.changedFields.ContainsKey("elements") &&
                LocalRenderingStatic.localRenderingDatas.TryGetValue(diff.actorNum, out var data))
            {
                RenderElements(diff.actorNum, data.elements);
            }
        }
    }
}
