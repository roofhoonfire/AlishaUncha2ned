using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor.Rendering;
using UnityEngine;

public enum DialogueType { Activate, TileChoose , FaceOff, Wait}


public class AlertDialogue : MonoBehaviour
{
    public static AlertDialogue Instance { get; private set; }

    [SerializeField] GameObject alertUI;
    [SerializeField] TMPro.TextMeshProUGUI alertText;

    private Queue<(ActionData, int, HookType, int, DialogueType)> dialogueQueue = new();
    private bool isRunning = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    public void StartDialogue(ActionData action, int actorNum, HookType h, int nthFaceOff, DialogueType d)
    {
        dialogueQueue.Enqueue((action, actorNum, h, nthFaceOff, d));
        if (!isRunning)
        {
            StartCoroutine(ProcessQueue());
        }
    }

    IEnumerator ProcessQueue()
    {
        isRunning = true;
        while (dialogueQueue.Count > 0)
        {
            var (action, actorNum, h,nthfaceoff, d) = dialogueQueue.Dequeue();
            yield return TempActionAlert(action, actorNum, h, nthfaceoff, d);
        }
        isRunning = false;
    }

    IEnumerator TempActionAlert(ActionData action, int actorNum, HookType h, int nthFaceOff, DialogueType d)
    {
        string message = null;

        if (d == DialogueType.Activate)
            message = $"플레이어 {actorNum}의 {action.cardname}의 {h} ";

        if (d == DialogueType.TileChoose)
            message = $"{action.cardname}의 발동 범위를 결정";
        if (d == DialogueType.Wait)
            message = "상대 행동중...";

        if (d == DialogueType.FaceOff)
            message = $"{nthFaceOff}번째 페이스 오프";
        float startDelay = 0.5f;
        float typingSpeed = 0.1f;

        alertUI.SetActive(true);
        alertText.text = "";

        yield return new WaitForSeconds(startDelay);

        foreach (char letter in message.ToCharArray())
        {
            alertText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        yield return new WaitForSeconds(2f);

        alertUI.SetActive(false);
    }
}
