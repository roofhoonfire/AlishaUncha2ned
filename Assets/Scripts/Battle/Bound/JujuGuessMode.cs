using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class JujuGuessMode : MonoBehaviour
{

    public static JujuGuessMode Instance;
    public bool isActive = false;
    private Coroutine _GuessJujuCoroutine;

    [Header("Targets")]
    [SerializeField] private BoundImageConductor opBoundConductor;

    [Header("UI")]
    [SerializeField] private InputField guessInput;   // 인스펙터에 드롭
    [SerializeField] private Button confirmButton;    // 인스펙터에 드롭
    int actorNum;

    int theBoundIndex;
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

        SetUIActive(false);

    }



    public void SetActive(bool active, int BoundIndex)
    {
        if (active == isActive) return; //이미중복 코루틴 시작 방지
        isActive = active;
        if (isActive) StartGuessJujuLoop(BoundIndex);
        else StopGuessJujuLoop(false);
    }


    private void StartGuessJujuLoop(int boundIndex)
    {
        if (_GuessJujuCoroutine != null) return;

        theBoundIndex = boundIndex;
        SetUIActive(true);
        ClearInputIfAny();

        _GuessJujuCoroutine = StartCoroutine(GuessJujuLoop());
    }


    public void StopGuessJujuLoop(bool Real)
    {
        if (_GuessJujuCoroutine != null)
        {
            StopCoroutine(_GuessJujuCoroutine);
            _GuessJujuCoroutine = null;
        }


        SetUIActive(false);

        isActive = false;

        if (Real == true)
        {
            Overmind.Instance.Submit_Juju(actorNum, null);


        }
    }

    private IEnumerator GuessJujuLoop()
    {
        while (true)
        {
            yield return null;
        }
    }

    public void GuessDone()
    {
        // 1) 인풋 값 확보
        string text = (guessInput != null) ? guessInput.text?.Trim() : null;
        if (string.IsNullOrEmpty(text))
            text = "상대의 이번 턴의 수상했던 행동을 적어보자"; // 비어있으면 기본 문구(원하면 변경)

        // 2) opBoundConductor의 boundGuesses에 반영
        if (opBoundConductor != null)
        {
            var list = opBoundConductor.boundGuesses;
            if (list == null)
            {
                opBoundConductor.boundGuesses = new List<string>();
                list = opBoundConductor.boundGuesses;
            }

            // 인덱스 범위 보장(초기화에서 채워져 있다고 가정하지만, 안전장치로 보정)
            while (list.Count <= theBoundIndex)
                list.Add("죶됨먼가꼬임");

            list[theBoundIndex] = text;
        }

        // 3) UI 비활성화 + 루프 종료
        SetUIActive(false);
        StopGuessJujuLoop(true);
    }
    // ───────── 유틸 ─────────
    private void SetUIActive(bool active)
    {
        if (guessInput != null) guessInput.gameObject.SetActive(active);
        if (confirmButton != null) confirmButton.gameObject.SetActive(active);
    }

    private void ClearInputIfAny()
    {
        if (guessInput != null) guessInput.text = string.Empty;
    }
}
