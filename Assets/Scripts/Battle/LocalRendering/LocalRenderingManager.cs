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
        
        
        
        ApplyFacingFromRightOrLeft(data1.actorNum, data1);
        ApplyFacingFromRightOrLeft(data2.actorNum, data2);





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
        StartCoroutine(Rendering_Norm_Action_Co(actorNum, data1, data2, action, h));
    }
    private IEnumerator Rendering_Norm_Action_Co(int actorNum, LocalRenderingData data1, LocalRenderingData data2, ActionData action, HookType h)
    {
        LocalRenderingData actorData = null;
        if (data1 != null && data1.actorNum == actorNum) actorData = data1;
        else if (data2 != null && data2.actorNum == actorNum) actorData = data2;

        ApplyFacingFromRightOrLeft(actorNum, actorData);
        Debug.Log($"{actorData.rightOrLeft}¸¦ ¹Ù¶ó º¼ °Ì´Ï´Ù ÀÌÁ¦");


        bool isMoveAction = (action != null && action.actionId == 0);

        // ¸ÞÀÎ ¾×¼Ç Àü¿ë: ¼û±è Ã³¸®
        if (!isMoveAction)
            CameraLovesAlisha.Instance.HideEmAll(LocalState.Instance.PlayerObDic[actorNum]);

        if (isMoveAction)
        {
            // ¡Ú ÀÌµ¿ ¾×¼Ç Àü¿ë ¾Ö´Ï¸ÞÀÌ¼Ç(´ë½Ã/Á¡ÇÁ + ÁöÁ¤ ÇÁ·¹ÀÓ ¿öÇÁ)
            yield return StartCoroutine(
                MoveAnimationRouter.Instance.PlayMoveCo(actorNum, data1, data2, action)
            );
        }
        else
        {
            // ¸ÞÀÎ ¾×¼Ç(¾îÁ¦ ÀÛ¾÷ÇÑ ¶ó¿ìÅÍ ±×´ë·Î)
            HitResolution? forcedOutcome = null;
            if (h == HookType.Activate)
                forcedOutcome = EvaluateHitOutcome(actorNum, data1, data2, action);

            yield return StartCoroutine(
                CardAnimationRouter.Instance.PlayCo(
                    action.cardcode, actorNum, h,
                    victimActorNum: null,
                    forcedOutcome: forcedOutcome
                )
            );
        }

        Debug.Log("ÀÚÀÚ ³ë¸Ö ¾×¼Ç ½ÃÄö½º Àß”f´Ï?");


        // ===== ¿©±âºÎÅÍ´Â °øÅë »çÈÄ Ã³¸®(±âÁ¸ À¯Áö) =====
        var diffs = CopyandDifferences(data1, data2);
        yield return StartCoroutine(AnimateStatChange("defense", diffs));
        yield return StartCoroutine(AnimateStatChange("hp", diffs));
        yield return StartCoroutine(AnimateStatChange("remainingCost", diffs));

        Debug.Log("ÀÚÀÚ UI ¹Ù²ï°Å Àß¹åÁö?");

        // À§Ä¡ ÃÖÁ¾ ½º³À(ÀÌµ¿ ¶ó¿ìÅÍ¿¡¼­ ÀÌ¹Ì ¿öÇÁÇß´õ¶óµµ µ¿ÀÏ ÁÂÇ¥·Î ÇÑ¹ø ´õ Á¤·Ä ¡æ ¹®Á¦ ¾øÀ½)
        //¿©±â¼­ ³Ë¹é·ù ¾Ö´Ï¸ÞÀÌ¼Ç ³ÖÀ¸¸é ÁÁÀ» µí ¤¾¤¾
        //LocalState.Instance.PlayerObDic[data1.actorNum].transform.position = tileIndextoPosition(data1.curpos).position;
        //LocalState.Instance.PlayerObDic[data2.actorNum].transform.position = tileIndextoPosition(data2.curpos).position;

        ApplyDiffsToLocalRenderingData(diffs);
        StealthPlayer(diffs);
        ElementRenderer.Instance.RenderElementsFromDiffs(diffs);
        Debug.Log("ÀÚÀÚ ³ë¸Ö ¾×¼Ç ·»´õ¸µ ´Ù ³¡, ÀÌÁ¦ ·»´õ¸µ ¼·¹Ø¸¸ ÇÏ¸éµÊ");
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

            // ¼ýÀÚ ¾Ö´Ï¸ÞÀÌ¼Ç(¶ì¸®¸®¸µ)
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                int current = Mathf.RoundToInt(Mathf.Lerp(oldVal, newVal, t));
                target.text = current.ToString();
                yield return null;
            }

            // ¾Ö´Ï¸ÞÀÌ¼Ç Á¾·á ÈÄ º¸Á¤/ÈÄÃ³¸®
            if (fieldName == "remainingCost" && isMyActor && newVal == 0)
            {
                // ÃÖÁ¾ 0À» Àá±ñ ÂïÀº µÚ Áï½Ã Áö¿ò
                target.text = "0";
                target.text = string.Empty;

                // Æ®¸®°Å ¹ß»ç (¶óº§ ±â¹Ý)
                AnimTriggerManager.Instance?.FireByLabel("myCasting", "Trig_End");
            }
            else
            {
                target.text = newVal.ToString(); // ÀÏ¹Ý º¸Á¤
            }
        }
    }

    private List<RenderDiff> CopyandDifferences(LocalRenderingData data1, LocalRenderingData data2)
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
    private void ApplyFacingFromRightOrLeft(int actorNum, LocalRenderingData d)
    {
        if (d == null) return;
        if (!LocalState.Instance.PlayerObDic.TryGetValue(actorNum, out var go) || go == null) return;

        // "right"¸é ¿À¸¥ÂÊÀ» º¸°Ô(±âº»Àº ¿ÞÂÊÀ» º½)
        bool faceRight = string.Equals(d.rightOrLeft, "right", System.StringComparison.OrdinalIgnoreCase);

        // ¿ì¼± SpriteRenderer.flipX·Î Ã³¸® (¿©·¯ ÆÄÃ÷°¡ ÀÖÀ¸¸é ÀüºÎ µÚÁý±â)
        var srs = go.GetComponentsInChildren<SpriteRenderer>(true);
        if (srs != null && srs.Length > 0)
        {
            foreach (var sr in srs) sr.flipX = faceRight;
            return;
        }

        // ½ºÇÁ¶óÀÌÆ®°¡ ¾Æ´Ï¶ó¸é(È¤Àº ·»´õ·¯°¡ ¾ø´Ù¸é) ½ºÄÉÀÏ·Î Æú¹é
        var t = go.transform;
        var ls = t.localScale;
        ls.x = Mathf.Abs(ls.x) * (faceRight ? -1f : 1f); // ±âº» ¿ÞÂÊ(+), ¿À¸¥ÂÊÀº -·Î µÚÁý±â
        t.localScale = ls;
    }








    //¶Ñµå·ÁÆÐ±â ¤¾ÆÇÁ¤¿ë

    private HitResolution EvaluateHitOutcome(int attackerActorNum, LocalRenderingData data1, LocalRenderingData data2, ActionData action)
    {
        // 1) Å¸ÀÏÇü -1 ¡æ ÀÚµ¿ Miss
        if (action.tileType == -1)
            return HitResolution.Missed;

        // 2) »ó´ë actorNumber (2ÀÎ ÀüÁ¦)
        int victimActorNum = Overmind.Instance.GetOtherPlayerNumber(attackerActorNum);

        // 3) »ó´ë ·»´õ¸µµ¥ÀÌÅÍ Ã£±â
        var victimData =
            (data1 != null && data1.actorNum == victimActorNum) ? data1 :
            (data2 != null && data2.actorNum == victimActorNum) ? data2 : null;

        if (victimData == null)
            return HitResolution.Missed; // ¾ÈÀü»§

        int victimIndex = victimData.curpos;

        // 4) È¿°ú ¹üÀ§ Å¸ÀÏ
        var effectTiles = GetEffectTilesSafe(action);

        // 5) ¹üÀ§ ¹ÛÀÌ¸é Miss
        if (effectTiles == null || effectTiles.Count == 0 || !effectTiles.Contains(victimIndex))
            return HitResolution.Missed;

        // 6) ¹æ¾î/µ¥¹ÌÁö ºñ±³
        int dmg = Mathf.Max(0, action.damage);
        int def = GetDefenseFromRenderingData(victimData);

        return (dmg <= def) ? HitResolution.Defended : HitResolution.Damage;
    }

    private HashSet<int> GetEffectTilesSafe(ActionData action)
    {
        // ActionData¿¡ effectTiles°¡ Ã¤¿öÁ® ÀÖ´Ù°í ÇßÀ¸´Ï ±×°É ±×´ë·Î »ç¿ë
        if (action.effectTiles != null && action.effectTiles.Count > 0)
            return new HashSet<int>(action.effectTiles);

        // ¾øÀ¸¸é ºó ÁýÇÕ ¡æ Miss·Î Ã³¸®µÊ
        return new HashSet<int>();
    }

    private int GetDefenseFromRenderingData(LocalRenderingData rd)
    {
        // ³×°¡ ÁØ ÇÊµå¸í ±×´ë·Î
        return rd.defense;
    }

}
