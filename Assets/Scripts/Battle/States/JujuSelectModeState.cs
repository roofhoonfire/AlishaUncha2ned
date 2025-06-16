using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class JujuSelectModeState : MonoBehaviour
{
    // Start is called before the first frame update
    public static JujuSelectModeState Instance;
    public bool isActive = false;
    private Coroutine _selectJujuCoroutine;
    private int actorNum;
  public   GameObject jujuPrefab;
    public RectTransform jujuContentArea; // ScrollView 안의 Content
    public GraphicRaycaster raycaster;


    public List<GameObject> spawnedJuju = new List<GameObject>();
    void Awake()
    {
        actorNum = PhotonNetwork.LocalPlayer.ActorNumber;
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

    }
    public void StopSelectJujuLoop()
    {
        if (_selectJujuCoroutine != null)
        {
            StopCoroutine(_selectJujuCoroutine);
            _selectJujuCoroutine = null;
        }

       
        
            isActive = false;
            ClearAllJuju();
            Overmind.Instance.SubmitJuju(actorNum);
    }
    public void SetActive(bool active, int actorNum, string boundCode)
    {
        if (active == isActive) return; //이미중복 코루틴 시작 방지
        isActive = active;
        if (isActive) StartSelectJujuLoop(boundCode);
        else StopSelectJujuLoop();
    }

    private void StartSelectJujuLoop(string boundCode)
    {
        if (_selectJujuCoroutine == null) {
            PopulateJuju(boundCode);

            _selectJujuCoroutine = StartCoroutine(SelectJujuLoop());
        }
    }


    private IEnumerator SelectJujuLoop()
    {
        while(true)
        {
            SelectJuju();
            yield return null;
        }
    }


    void SelectJuju()
    {
        if (Input.GetMouseButtonDown(0))
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            List<RaycastResult> results = new List<RaycastResult>();
            raycaster.Raycast(pointerData, results);

            foreach (var result in results)
            {
                GameObject clicked = result.gameObject;

                foreach (var juju in spawnedJuju)
                {
                    if (clicked == juju || clicked.transform.IsChildOf(juju.transform))
                    {
                        var data = JujuLoader.jujuDataBase[juju.GetComponent<EachJujuInfo>().jujuCode]; // 또는 저장해둔 map 사용
                        data.Apply();
                        
                        StopSelectJujuLoop();
                    }
                }
            }
        }



    }

    void PopulateJuju(string boundCode)
    {
        spawnedJuju.Clear();

        var localData = LocalState.Instance.localPlayers[actorNum];
        int boundPointThreshold = BoundLoader.boundDataBase[boundCode].boundPoint;

        // 필터링
        List<string> filteredJujuCodes = new List<string>();
        foreach (var jujuCode in localData.JujuCode)
        {
            if (JujuLoader.jujuDataBase.TryGetValue(jujuCode, out var juju))
            {
                if (juju.boundPoint <= boundPointThreshold)
                {
                    filteredJujuCodes.Add(jujuCode);
                }
            }
        }
        Debug.Log($"총 {filteredJujuCodes.Count}개가 필터링 됫다 뭐가 나올지 궁금하군 후후");

        // 셔플
        ShuffleList(filteredJujuCodes);

        // 최대 3개만 선택
        int count = Mathf.Min(3, filteredJujuCodes.Count);

        for (int i = 0; i < count; i++)
        {
            string selectedCode = filteredJujuCodes[i];
            Juju jujuData = JujuLoader.jujuDataBase[selectedCode];

            GameObject jujuObj = Instantiate(jujuPrefab, jujuContentArea);
            spawnedJuju.Add(jujuObj);
             EachJujuInfo info = jujuObj.GetComponent<EachJujuInfo>();
            if (info != null)
            {
                info.ApplyJujuData(jujuData);
            }
            else
            {
                Debug.LogWarning("EachJujuInfo 스크립트를 찾을 수 없습니다.");
            }
        }
    }
        void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

    public void ClearAllJuju()
    {

        //애니메이션 이쁜거 넣기 ㅎ
        // childCount 대신 Transform을 순회하여 안전하게 제거
        for (int i = jujuContentArea.childCount - 1; i >= 0; i--)
        {
            Transform child = jujuContentArea.GetChild(i);
            Destroy(child.gameObject);
        }
    }
}
