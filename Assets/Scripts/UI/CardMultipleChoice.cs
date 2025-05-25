using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

public class CardMultipleChoice : MonoBehaviour
{
    public GameObject buttonPrefab; // Button 프리팹 (Text 포함)
    public GameObject scrollView;
    public Transform contentParent; // ScrollView의 Content 오브젝트

    private TaskCompletionSource<int> _tcs;
    public static CardMultipleChoice Instance;

    private void Awake()
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

    public void ChoiceStart(int amount)
    {
        StartCoroutine(AddActionClockBuffer(amount));
    }
    /// <summary>
    /// amount 기준으로 [-amount, ..., 0, ..., +amount] 버튼을 생성하고, 
    /// 사용자가 선택한 값을 반환합니다.
    /// </summary>
    public async Task<int> SelectInAmount(int amount)
    {
        ClearButtons();
        scrollView.SetActive(true);
        _tcs = new TaskCompletionSource<int>();

        for (int i = -amount; i <= amount; i++)
        {
            var btnObj = Instantiate(buttonPrefab, contentParent);
            if (btnObj.GetComponent<LayoutElement>() == null)
                btnObj.AddComponent<LayoutElement>();
            var btn = btnObj.GetComponent<Button>();
            var txt = btnObj.GetComponentInChildren<Text>();
            txt.text = i.ToString();

            int value = i; // 클로저 캡처 방지
            btn.onClick.AddListener(() => OnButtonSelected(value));
        }

        // UI 활성화
        
        int result = await _tcs.Task;

        // UI 비활성화 및 버튼 정리
        ClearButtons();
        scrollView.SetActive(false);
        return result;
    }

    
    public IEnumerator AddActionClockBuffer(int amount)
    {
        var task = SelectInAmount(amount); // async Task<int> 호출
        while (!task.IsCompleted)
            yield return null;

        if (task.IsCompletedSuccessfully)
        {
            int result = task.Result;
            CardModeState.Instance.actionClockBuffer += result;
        }
        else if (task.IsFaulted)
        {
            Debug.LogError(task.Exception);
        }
    }

    private void OnButtonSelected(int value)
    {
        _tcs?.TrySetResult(value);
    }

    private void ClearButtons()
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }
    }
}