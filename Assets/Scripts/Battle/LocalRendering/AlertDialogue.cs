using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor.Rendering;
using UnityEngine;

public enum DialogueType { Activate, TileChoose }


public class AlertDialogue : MonoBehaviour
{
    public static AlertDialogue Instance { get; private set; }

    [SerializeField] GameObject alertUI;
    [SerializeField] TMPro.TextMeshProUGUI alertText;

    private Queue<(ActionData, int, HookType, DialogueType)> dialogueQueue = new();
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

    public void StartDialogue(ActionData action, int actorNum, HookType h, DialogueType d)
    {
        dialogueQueue.Enqueue((action, actorNum, h, d));
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
            var (action, actorNum, h, d) = dialogueQueue.Dequeue();
            yield return TempActionAlert(action, actorNum, h, d);
        }
        isRunning = false;
    }

    IEnumerator TempActionAlert(ActionData action, int actorNum, HookType h, DialogueType d)
    {
        string message = null;

        if (d == DialogueType.Activate)
            message = $"플레이어 {actorNum}의 {action.cardname}의 {h} 크하하하!";

        if (d == DialogueType.TileChoose)
            message = $"플레이어 {actorNum}의 {action.cardname}의 발동 범위를 정한다";

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
