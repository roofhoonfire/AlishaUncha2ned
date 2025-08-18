using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CardAnimationDB", menuName = "DB/Card Animation DB")]
public class CardAnimationDB : ScriptableObject
{
    
    [Serializable]
    public class HitStopSpec
    {
        [Tooltip("히트스탑을 걸 프레임(공격자 클립의 실제 프레임 기준)")]
        public int frame = 0;

        [Tooltip("히트스탑 지속 시간(초). 실시간 기준으로 동작")]
        public float duration = 0.06f;

        [Header("정지 대상")]
        public bool pauseAttacker = true;
        public bool pauseVictim = true;
        public bool pauseEffects = true;

        [Tooltip("씬 전체 정지(Time.timeScale=0)")]
        public bool pauseScene = false;
    }

    [Serializable]
    public class CardAnimEntry
    {
        [Header("키")]
        public string cardCode;
        public HookType hook;

        [Header("피격자(방어) 전용 애니(옵션)")]
        public string victimDefendTrigger;     // 예: "Trig_Defend"
        public string victimDefendStateName;   // 예: "Base Layer.Defend"

        [Header("예비동작(Prep) 애니")]
        [Tooltip("Prep으로 진입시키는 트리거(없으면 state로 직접 전환)")]
        public string prepTrigger;
        [Tooltip("Prep 상태 이름")]
        public string prepStateName;
        [Tooltip("Prep 클립(알면 정확도↑)")]
        public AnimationClip prepClip;
        [Tooltip("Prep 총 프레임 수를 직접 지정하고 싶을 때")]
        public int prepTotalFramesOverride = 0;
        [Tooltip("Prep 마지막 스프라이트에서 멈춰 있을 시간(초)")]
        public float prepPoseHoldSec = 0.25f;

        [Header("공격자(Attacker) 애니")]
        public string animatorTrigger;   // Attack 트리거
        public string stateName;         // Attack 상태 이름
        public AnimationClip clip;       // Attack 클립
        public int totalFramesOverride = 0;

        [Header("피격자(Victim) 애니")]
        public string victimAnimatorTrigger = "Trig_Hit";
        public string victimStateName = "Base Layer.Hit";
        public AnimationClip victimClip;

        [Tooltip("피격자 애니를 시작할 프레임들(※ Attack 클립 기준 프레임!)")]
        public List<int> victimStartFrames = new List<int>();


        // CardAnimationDB.CardAnimEntry 안에 아래 블록을 추가
        [Header("Cinematic Backdrop (Point1: Prep 정지 구간)")]
        [Tooltip("Prep 정지 구간에 백드롭을 사용할지")]
        public bool usePrepBackdrop = true;

        [Tooltip("Prep 정지 구간에 표시할 풀스크린 이미지")]
        public Sprite prepBackdropSprite;

        [Tooltip("백드롭 페이드 인/아웃 시간(초)")]
        public float prepBackdropFadeIn = 0.15f;
        public float prepBackdropFadeOut = 0.12f;


        [Header("히트스탑 설정(※ Attack 클립 기준 프레임!)")]
        public List<HitStopSpec> hitStops = new List<HitStopSpec>();
    }

    public List<CardAnimEntry> entries = new List<CardAnimEntry>();
}
