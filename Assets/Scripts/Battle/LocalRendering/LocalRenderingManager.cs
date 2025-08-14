using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class LocalRenderingManager : MonoBehaviour
{
    public TextMeshPro myHP;
    public TextMeshPro opHP;
    public TextMeshPro mydefense;
    public TextMeshPro opdefense;

    public TextMeshProUGUI opponencostRemainTxt;
    public TextMeshProUGUI mycostRemainTxt;


    public TextMeshProUGUI myBound;
    public TextMeshProUGUI OpBound;

    public static LocalRenderingManager Instance;


    public class RenderDiff
    {
        public int actorNum;
        public Dictionary<string, (object oldValue, object newValue)> changedFields = new();
    }
    // Start is called before the first frame update
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


    public  void Rendering_JujuSync(LocalRenderingData data1, LocalRenderingData data2)
    {
        //¹Ù¿îµå ÀÎµ¦½º ³Ñ¾î°¡´Â °Å º¸¿©ÁÜ 

        //ÁÖ¼ú ¼±ÅÃ ¾Ö´Ï¸ÞÀÌ¼Ç Ãâ·Â µî 


        //data1°ú ±âÁ¸ÀÇ LocalRenderingData.localRenderingDatas ÀÇ °ª°ú ´Ù¸¥ °Íµé À» ¾Ö´Ï¸Þ·Î ÃÒÃÒÃÒ
        ApplyImmediateUI(data1);
        ApplyImmediateUI(data2);

        Overmind.Instance.Submit_RenderingDone(PhotonNetwork.LocalPlayer.ActorNumber);
    }


    public  void Rendering_AfterActionSelect(LocalRenderingData data1, LocalRenderingData data2)
    {

        List<RenderDiff> diffs = CopyandDifferences(data1, data2);
        ApplyDiffsToLocalRenderingData(diffs);


        //ÀÌ°Ç±ðÀÌ±âº¸´Ü ¶Ñ¿Õ ÇÏ°í ³ªÅ¸³ª´Â ´À³¦À¸·Î..´Ù°¡ . .
        //StartCoroutine(AnimateStatChange("defense", diffs));
        //StartCoroutine(AnimateStatChange("remainingCost", diffs));
        //ApplyDiffsToLocalRenderingData(diffs);
        //Debug.Log("¹®Á¦¾ø´Ù");

        //¶Ñ¿Õ ÇÏ´Â ´À³¦À¸·ç´Ù°¡!
        ApplyImmediateUI(data1);
        ApplyImmediateUI(data2);

        //ÀÌ°Å ÇÏ°í Àá±ñ ¸Ø­Ÿ´Ù ´Ù°¡ ¶Ñ°¡°¡°¡
        Overmind.Instance.Submit_RenderingDone(PhotonNetwork.LocalPlayer.ActorNumber);


    }
   public void ApplyImmediateUI(LocalRenderingData data)
    {
        bool isMine = data.actorNum == PhotonNetwork.LocalPlayer.ActorNumber;

        if (isMine)
        {
            myHP.text = data.hp.ToString();
            mydefense.text = data.defense.ToString();
            myBound.text = data.bounds[data.boundIndex].ToString(); // ÇÊ¿äÇÏ¸é ´Ù¸¥ ¹æ½ÄÀ¸·Î Æ÷¸Ë
        }
        else
        {
            opHP.text = data.hp.ToString();
            opdefense.text = data.defense.ToString();
            OpBound.text = data.bounds[data.boundIndex].ToString(); // ¸¶Âù°¡Áö
        }
    }

    public void Rendering_GameStart(LocalRenderingData data1, LocalRenderingData data2)
    {

        //°ÔÀÓ½ÃÀÛ ·»´õ¸µ ¿¬Ãâ ³Ö°í ½ÍÀº°Å Áý¾î ¿¸¾î¶ó ½º¹ß¾Æ


        

        ApplyImmediateUI(data1);
        ApplyImmediateUI(data2);

        //½ÌÅ©´Â ¾Æ·¡¼­ ³Ö¾îÁØ´Ù
        //
        //Overmind.Instance.Submit_RenderingDone(PhotonNetwork.LocalPlayer.ActorNumber);


    }
    public  void Rendering_FaceOff_Start(int nthFaceOff)
    {

        AlertDialogue.Instance.StartDialogue(null, 0, 0, nthFaceOff, DialogueType.FaceOff);

    }
    public void Rendering_Norm_Action(int actorNum, LocalRenderingData data1, LocalRenderingData data2, ActionData action, HookType h)
    {

        AlertDialogue.Instance.StartDialogue(action, actorNum, h, 0, DialogueType.Activate);


        //ÈÅ Å¸ÀÔ¿¡ ¸Â´Â ¾Ö´Ï¸ÞÀÌ¼Ç Àç»ýÇØÁÖ°í 

        CardAnimationRouter.Instance.Play(action.cardcode, actorNum, h);


        if (h == HookType.Counter)
            Debug.Log("Ä¨½Çµå ³Í µÚ º¾î");

        List<RenderDiff> diffs = CopyandDifferences(data1, data2);
        StartCoroutine(AnimateStatChange("defense", diffs));
        StartCoroutine(AnimateStatChange("hp", diffs));
        StartCoroutine(AnimateStatChange("remainingCost", diffs));
        
        //Ä³¸¯ÅÍ À§Ä¡µµ ¹Ù²ãÁÒ¾ß ÇÔ 
        //ÀÌ»Ú°ÔÇÏ´Â¹ýÀÌ³ª Á» Ã£¾Æ¶ó
        //ÀÓ½ÃÀÌµ¿
        //³ªÁß¿¡ ½ºÅ³ÀÌµ¿ÀÎÁö ±×³ÉÀÌµ¿ÀÎÁö ±¸ºÐÇÏ°í ¾Ö´Ï¸ÞÀÌ¼ÇÀÌ¶û ¿¬µ¿ÇØ¼­ Àß µ¿ÀÛÇÏ°Ô²û ¹Ù²Ù 3
        LocalState.Instance.PlayerObDic[data1.actorNum].transform.position = tileIndextoPosition(data1.curpos).position;
        LocalState.Instance.PlayerObDic[data2.actorNum].transform.position = tileIndextoPosition(data2.curpos).position;


        ApplyDiffsToLocalRenderingData(diffs);
        //ÀÏÄÉÇÏ¸é ¶Ç ¤¡¤ºÀ»Áöµµ ¸ð¸£°Ù±º 
        StealthPlayer(diffs); //¾Æ¸¶ ½ºÅÚ½ºµµ Áö±Ý data1, data2°¡ °¢°¢ ½Å ±¸·Î ÀÌÇØÇÏ°í ÀÕÀ» °¡´É¼ºÀÌ ÀÕÀ½ 
        ElementRenderer.Instance.RenderElementsFromDiffs(diffs);



        Overmind.Instance.Submit_RenderingDone(PhotonNetwork.LocalPlayer.ActorNumber);


    }
    public void Rendering_Dot_Action(int actorNum, LocalRenderingData data1, LocalRenderingData data2, ActionData action, HookType h)
    {

        AlertDialogue.Instance.StartDialogue(action, actorNum, h, 0, DialogueType.Activate);


        //ÈÅ Å¸ÀÔ¿¡ ¸Â´Â ¾Ö´Ï¸ÞÀÌ¼Ç Àç»ýÇØÁÖ°í 



        List<RenderDiff> diffs = CopyandDifferences(data1, data2);
        StartCoroutine(AnimateStatChange("defense", diffs));
        StartCoroutine(AnimateStatChange("hp", diffs));
        StartCoroutine(AnimateStatChange("remainingCost", diffs));


        //Ä³¸¯ÅÍ À§Ä¡µµ ¹Ù²ãÁÒ¾ß ÇÔ 
        //ÀÌ»Ú°ÔÇÏ´Â¹ýÀÌ³ª Á» Ã£¾Æ¶ó
        //ÀÓ½ÃÀÌµ¿
        //³ªÁß¿¡ ½ºÅ³ÀÌµ¿ÀÎÁö ±×³ÉÀÌµ¿ÀÎÁö ±¸ºÐÇÏ°í ¾Ö´Ï¸ÞÀÌ¼ÇÀÌ¶û ¿¬µ¿ÇØ¼­ Àß µ¿ÀÛÇÏ°Ô²û ¹Ù²Ù 3
        LocalState.Instance.PlayerObDic[data1.actorNum].transform.position = tileIndextoPosition(data1.curpos).position;
        LocalState.Instance.PlayerObDic[data2.actorNum].transform.position = tileIndextoPosition(data2.curpos).position;


        ApplyDiffsToLocalRenderingData(diffs);
        //ÀÏÄÉÇÏ¸é ¶Ç ¤¡¤ºÀ»Áöµµ ¸ð¸£°Ù±º 
        StealthPlayer(diffs); //¾Æ¸¶ ½ºÅÚ½ºµµ Áö±Ý data1, data2°¡ °¢°¢ ½Å ±¸·Î ÀÌÇØÇÏ°í ÀÕÀ» °¡´É¼ºÀÌ ÀÕÀ½ 
        ElementRenderer.Instance.RenderElementsFromDiffs(diffs);



        Overmind.Instance.Submit_RenderingDone(PhotonNetwork.LocalPlayer.ActorNumber);


    }
    private void StealthPlayer(List<RenderDiff> diffs)
    {
        foreach (var diff in diffs)
        {
            if (diff.changedFields.TryGetValue("isStealthed", out var stealthChange))
            {
                bool newValue = (bool)stealthChange.Item2;

                if (LocalState.Instance.PlayerObDic.TryGetValue(diff.actorNum, out var playerOb))
                {
                    var sr = playerOb.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        var color = sr.color;

                        if (newValue) // ½ºÅÚ½º On
                        {
                            color.a = (diff.actorNum == PhotonNetwork.LocalPlayer.ActorNumber) ? 0.4f : 0f;
                        }
                        else // ½ºÅÚ½º Off
                        {
                            color.a = 1f;
                        }

                        sr.color = color;
                    }
                }
            }
        }
    }
    private Transform tileIndextoPosition(int tileindex)
    {

        //ÀÌ°Å ·ÎÄÃ¿¡¼­µµ ±×¸®µå ÃÊ±âÈ­ ÇØ¾ßÇÔ 
        return GridManagement.Instance?.tileObjects[tileindex].transform.Find("charpoint");
        //¸®ÅÏ µÈ ³ðÀº TransformÀ¸·Î ¹Þ°í .transform.positionÀ¸·Î ½á¾ß ÀÛµ¿

    }

    public ActionData Rendering_Before_Tile_Choose_ShowDown(List<(int actorNum, ActionData action)> actionList, LocalRenderingData data1, LocalRenderingData data2)
    {
        int myActorNum = PhotonNetwork.LocalPlayer.ActorNumber;

        var myActionTuple = actionList.FirstOrDefault(pair => pair.actorNum == myActorNum);

        if (myActionTuple.action == null)
        {
            Debug.LogError($"·ÎÄÃ ÇÃ·¹ÀÌ¾îÀÇ ¾×¼ÇÀ» Ã£À» ¼ö ¾ø½À´Ï´Ù. (ActorNumber: {myActorNum})");
            return null;
        }

        ActionData myAction = myActionTuple.action;

        if (myAction.actionId == 1)
        {
            AlertDialogue.Instance.StartDialogue(myAction, 0, 0, 0, DialogueType.TileChoose);

        }
        else
        {
            AlertDialogue.Instance.StartDialogue(null, 0, 0, 0, DialogueType.Wait);
        }


        List<RenderDiff> diffs = CopyandDifferences(data1, data2);
        StartCoroutine(AnimateStatChange("remainingCost", diffs));
        ApplyDiffsToLocalRenderingData(diffs);

        return myAction;
    }

    public void Rendering_Before_Tile_Choose(LocalRenderingData data1, LocalRenderingData data2)
    {


        List<RenderDiff> diffs = CopyandDifferences(data1, data2);
        StartCoroutine(AnimateStatChange("remainingCost", diffs));
        ApplyDiffsToLocalRenderingData(diffs);

        Overmind.Instance.Submit_RenderingDone(PhotonNetwork.LocalPlayer.ActorNumber);


    }
    public void Rendering_Tile_Choose(int actorNum, ActionData action)
    {



        if (actorNum != PhotonNetwork.LocalPlayer.ActorNumber)
        {
            AlertDialogue.Instance.StartDialogue(action, 0, 0, 0, DialogueType.TileChoose);

        }
        else
        {
            AlertDialogue.Instance.StartDialogue(null, 0, 0, 0, DialogueType.Wait);

        }



    }
    private IEnumerator AnimateStatChange(string fieldName, List<RenderDiff> diffs)
    {
        foreach (var diff in diffs)
        {
            if (!diff.changedFields.TryGetValue(fieldName, out var change))
                continue;

            if (change.oldValue is not int oldVal || change.newValue is not int newVal)
                continue;

            bool isMyActor = diff.actorNum == PhotonNetwork.LocalPlayer.ActorNumber;

            // Å¸°Ù ÅØ½ºÆ® ÇÒ´ç
            TMP_Text target = null;
            switch (fieldName)
            {
                case "defense":
                    target = isMyActor ? mydefense : opdefense;
                    break;
                case "remainingCost":
                    target = isMyActor ? mycostRemainTxt : opponencostRemainTxt;
                    break;

                case "hp":
                    target = isMyActor ? myHP : opHP;
                    break;

            }

            if (target == null) continue;

            float duration = 0.4f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                int current = Mathf.RoundToInt(Mathf.Lerp(oldVal, newVal, t));
                target.text = current.ToString();
                yield return null;
            }

            target.text = newVal.ToString(); // º¸Á¤
        }
    }
    private  List<RenderDiff> CopyandDifferences(LocalRenderingData data1, LocalRenderingData data2)
    {
        List<RenderDiff> results = new();

        void CompareWithLocal(LocalRenderingData incomingData)
        {
            int actorNum = incomingData.actorNum;
            if (!LocalRenderingStatic.localRenderingDatas.TryGetValue(actorNum, out var localData))
                return;

            var diff = new RenderDiff { actorNum = actorNum };

            void Compare<T>(string fieldName, T oldVal, T newVal)
            {
                if (!EqualityComparer<T>.Default.Equals(oldVal, newVal))
                    diff.changedFields[fieldName] = (oldVal, newVal);
            }

            Compare("curpos", localData.curpos, incomingData.curpos);
            Compare("hp", localData.hp, incomingData.hp);
            Compare("defense", localData.defense, incomingData.defense);
            Compare("remainingCost", localData.remainingCost, incomingData.remainingCost);
            Compare("boundIndex", localData.boundIndex, incomingData.boundIndex);
            Compare("isStealthed", localData.isStealthed, incomingData.isStealthed); // Ãß°¡

               bool ListDiff<T>(List<T> a, List<T> b)
                => !(a?.SequenceEqual(b) ?? b == null);

            if (ListDiff(localData.hands, incomingData.hands))
                diff.changedFields["hands"] = (localData.hands, incomingData.hands);

            if (ListDiff(localData.bounds, incomingData.bounds))
                diff.changedFields["bounds"] = (localData.bounds, incomingData.bounds);
            if (ListDiff(localData.elements, incomingData.elements))
                diff.changedFields["elements"] = (localData.elements, incomingData.elements);


            //½ºÅ×ÀÌÅÍ½º´Â º¸·ù
            //  if (ListDiff(localData.statuses, incomingData.statuses))
            //    diff.changedFields["statuses"] = (localData.statuses, incomingData.statuses);

            if (diff.changedFields.Count > 0)
                results.Add(diff);
        }

        CompareWithLocal(data1);
        CompareWithLocal(data2);

        return results;
    }


    public void ApplyDiffsToLocalRenderingData(List<RenderDiff> diffs)
    {
        foreach (var diff in diffs)
        {
            if (!LocalRenderingStatic.localRenderingDatas.TryGetValue(diff.actorNum, out var targetData))
                continue;

            foreach (var kvp in diff.changedFields)
            {
                string fieldName = kvp.Key;
                object newVal = kvp.Value.newValue;

                switch (fieldName)
                {
                    case "curpos":
                        targetData.curpos = (int)newVal;
                        break;
                    case "hp":
                        targetData.hp = (int)newVal;
                        break;
                    case "defense":
                        targetData.defense = (int)newVal;
                        break;
                    case "remainingCost":
                        targetData.remainingCost = (int)newVal;
                        break;
                    case "boundIndex":
                        targetData.boundIndex = (int)newVal;
                        break;
                    case "hands":
                        targetData.hands = new List<string>((List<string>)newVal);
                        break;
                    case "bounds":
                        targetData.bounds = new List<string>((List<string>)newVal);
                        break;
                    case "isStealthed":
                        targetData.isStealthed = (bool)newVal;
                        break;
                    case "elements":
                        targetData.elements = new List<apProp>((List<apProp>)newVal);
                        break;    // case "statuses": // ÇâÈÄ ±¸Çö
                        //     break;
                }
            }
        }
    }

}
