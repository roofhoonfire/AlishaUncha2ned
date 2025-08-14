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
        [Tooltip("공격자 애니메이터 정지")]
        public bool pauseAttacker = true;

        [Tooltip("피격자 애니메이터 정지")]
        public bool pauseVictim = true;

        [Tooltip("이펙트 애니메이터 정지(있으면)")]
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

        [Header("공격자(Attacker) 애니")]
        public string animatorTrigger;
        public string stateName;
        public AnimationClip clip;
        public int totalFramesOverride = 0;

        [Header("피격자(Victim) 애니")]
        [Tooltip("피격자 애니 트리거(예: Trig_Hit)")]
        public string victimAnimatorTrigger = "Trig_Hit";

        [Tooltip("피격자 애니 상태 이름(예: Base Layer.Hit)")]
        public string victimStateName = "Base Layer.Hit";

        [Tooltip("피격자 애니 클립(있으면 정확도↑)")]
        public AnimationClip victimClip;

        [Tooltip("피격자 애니를 시작할 프레임들(공격자 클립의 프레임 기준)")]
        public List<int> victimStartFrames = new List<int>();

        [Header("히트스탑 설정(공격자 프레임 기준)")]
        public List<HitStopSpec> hitStops = new List<HitStopSpec>();
    }

    public List<CardAnimEntry> entries = new List<CardAnimEntry>();
}
