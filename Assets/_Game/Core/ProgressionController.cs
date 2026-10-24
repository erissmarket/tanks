using System.IO;
using TankGame.Data;
using TankGame.Terrain;
using UnityEngine;

namespace TankGame.Core
{
    [DefaultExecutionOrder(-80)]
    public sealed class ProgressionController : MonoBehaviour
    {
        [SerializeField] private EconomyRules rules;
        [SerializeField] private MatchFlowController match;
        [SerializeField] private TerrainThemeController themes;
        // Non-empty only for isolated Editor test instances.
        [SerializeField,HideInInspector] private string savePathOverride;
        private ProgressStore store;
        private string activeMatchId;
        private MatchResult pendingResult;
        private MatchEndReason pendingReason;
        private int pendingScore;
        private bool unsettled;
        public int LastReward { get; private set; }
        public bool RewardSaved { get; private set; }
        public EconomyRules Rules=>rules;
        public ProgressStore Store=>store??(store=new ProgressStore(SavePath(),rules));
        internal void UseQualificationSave(string path)
        {
            if(store!=null)throw new System.InvalidOperationException("Choose the isolated save before loading progress.");
            savePathOverride=path;
        }
        private string SavePath()
        {
            if(!string.IsNullOrEmpty(savePathOverride))return savePathOverride;
#if UNITY_EDITOR
            string testPath=UnityEditor.SessionState.GetString("TankGame.Progression.TestPath","");
            if(!string.IsNullOrEmpty(testPath))return testPath;
#endif
            return Path.Combine(Application.persistentDataPath,"progress-v1.json");
        }
        private void OnEnable(){if(match!=null)match.MatchEnded+=OnMatchEnded;}
        private void OnDisable()
        {
            if(match!=null)match.MatchEnded-=OnMatchEnded;
            if(themes!=null)themes.ThemeChanged-=SaveTheme;
        }
        private void Start()
        {
            var saved=Store.Snapshot;AudioListener.volume=saved.soundEnabled?1:0;
            if(themes!=null){themes.SelectTheme(Mathf.Clamp(saved.selectedTheme,0,themes.Themes.Length-1));themes.ThemeChanged+=SaveTheme;}
        }
        private void SaveTheme(int index)=>Store.SetTheme(index);
        public bool ToggleSound()
        {
            bool enabled=!Store.Snapshot.soundEnabled;
            if(!Store.SetSound(enabled))return false;AudioListener.volume=enabled?1:0;return true;
        }
        public bool BeginBattle()
        {
            if(unsettled&&!RetrySettlement())return false;
            activeMatchId=Store.BeginMatch();LastReward=0;RewardSaved=false;return !string.IsNullOrEmpty(activeMatchId);
        }
        private void OnMatchEnded(MatchResult result)
        {
            if(string.IsNullOrEmpty(activeMatchId))return;
            pendingResult=result;pendingReason=match.EndReason;pendingScore=match.GetScore(0);unsettled=true;RetrySettlement();
        }
        public bool RetrySettlement()
        {
            if(!unsettled)return true;
            if(!Store.CompleteMatch(activeMatchId,pendingResult,pendingReason,pendingScore,out int reward))return false;
            LastReward=reward;RewardSaved=true;unsettled=false;activeMatchId=null;return true;
        }
    }
}
