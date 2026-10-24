using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TankGame.Core;
using TankGame.Presentation;
using TankGame.Projectiles;
using TankGame.Tank;
using TankGame.Terrain;
using UnityEngine;
using UnityEngine.Profiling;

namespace TankGame.Testing
{
    // Opt-in test runner. Normal launches do not start tests or change the save path.
    [DefaultExecutionOrder(-500)]
    public sealed class ReleaseQualification : MonoBehaviour
    {
        private string output;
        private bool standalone,smoke,running;
        private readonly StringBuilder report=new StringBuilder();
        private readonly List<string> errors=new List<string>();
        private GameInterface flow;
        private MatchFlowController match;
        private GameplayActionController actions;
        private BackendTestBootstrap bootstrap;
        private IntegratedInput input;
        private BattleFeedback feedback;
        private TankGame.AI.SimpleArtilleryAI ai;
        private bool aiEnabled,previousRunInBackground;
        private int passed,matrixPassed,peakProjectiles,peakEffects;
        private float started;
        public bool Finished {get;private set;}
        public bool Succeeded {get;private set;}
        public int MatrixPassed=>matrixPassed;
        private void Awake()
        {
            string[] args=Environment.GetCommandLineArgs();int flag=Array.IndexOf(args,"--tank-smoke");
            if(flag<0)return;
            standalone=smoke=true;
            output=Path.GetFullPath(flag+1<args.Length?args[flag+1]:"Qualification");Directory.CreateDirectory(output);
            FindAnyObjectByType<ProgressionController>().UseQualificationSave(Path.Combine(output,"profile-"+Guid.NewGuid().ToString("N"),"progress-v1.json"));
        }
        private IEnumerator Start()
        {
            if(!standalone)yield break;
            while(FindAnyObjectByType<GameInterface>()==null||!FindAnyObjectByType<GameInterface>().IsBuilt)yield return null;
            Begin(output,true);
        }
        public void Begin(string directory,bool quick=false)
        {
            if(running)throw new InvalidOperationException("Qualification is already running.");
            output=Path.GetFullPath(directory);Directory.CreateDirectory(output);smoke=quick;running=true;started=Time.realtimeSinceStartup;
            previousRunInBackground=Application.runInBackground;Application.runInBackground=true;
            Application.logMessageReceived+=OnLog;
            StartCoroutine(Guard(Run()));
        }
        private void Update()
        {
            if(running&&match!=null&&match.IsRunning&&match.IsPaused){match.SetPaused(false);Time.timeScale=6;}
        }
        private void OnLog(string message,string stack,LogType type)
        {if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert)errors.Add(message);}
        private IEnumerator Guard(IEnumerator routine)
        {
            while(true)
            {
                bool more;object current;
                try{more=routine.MoveNext();current=more?routine.Current:null;}
                catch(Exception error){report.AppendLine("FAIL "+error);Complete(false);yield break;}
                if(!more)break;yield return current;
            }
            Complete(errors.Count==0);
        }
        private void Check(bool condition,string message){if(!condition)throw new Exception(message);}
        private void Pass(string message){passed++;report.AppendLine("PASS "+message);WriteProgress();}
        private void WriteProgress()=>File.WriteAllText(Path.Combine(output,"PhaseSevenVerification.txt"),report.ToString()+$"RUNNING matrix {matrixPassed}/168, elapsed {Time.realtimeSinceStartup-started:0.0}s\n");
        private IEnumerator Run()
        {
            flow=FindAnyObjectByType<GameInterface>();match=FindAnyObjectByType<MatchFlowController>();actions=FindAnyObjectByType<GameplayActionController>();bootstrap=FindAnyObjectByType<BackendTestBootstrap>();input=FindAnyObjectByType<IntegratedInput>();feedback=FindAnyObjectByType<BattleFeedback>();
            ai=FindAnyObjectByType<TankGame.AI.SimpleArtilleryAI>();if(ai!=null){aiEnabled=ai.enabled;ai.enabled=false;}
            Check(flow!=null&&flow.IsBuilt&&flow.CurrentScreen==InterfaceScreen.Home&&!match.IsRunning,"Release did not open at home");
            Pass("release scene opens home without starting a battle");
            var store=bootstrap.Progression.Store;
            Check(!standalone||Application.persistentDataPath!=output,"Qualification must use an isolated profile");
            for(int i=0;i<10;i++){string id=store.BeginMatch();Check(store.CompleteMatch(id,new MatchResult(0,1),MatchEndReason.Elimination,200,out int coins),"Seed test wallet failed");}
            foreach(var tank in bootstrap.TankOptions)if(!store.OwnsTank(tank))Check(store.Unlock(tank),"Unlock test tank failed");
            foreach(var shield in bootstrap.ShieldOptions)if(!store.OwnsShield(shield))Check(store.Unlock(shield),"Unlock test shield failed");
            store.SetSound(false);AudioListener.volume=0;
            Pass("isolated test wallet unlocks the full legal equipment set");
            Time.timeScale=6;
            var terrain=FindAnyObjectByType<DestructibleTerrain>();
            var terrainTexture=terrain.GetComponent<SpriteRenderer>().sprite.texture;
            long initialMemory=Profiler.GetTotalAllocatedMemoryLong();
            if(!smoke)
            {
                for(int tank=0;tank<bootstrap.TankOptions.Length;tank++)
                for(int shield=0;shield<bootstrap.ShieldOptions.Length;shield++)
                {
                    if(!bootstrap.TankOptions[tank].Supports(bootstrap.ShieldOptions[shield]))continue;
                    for(int theme=0;theme<3;theme++)for(int weapon=0;weapon<input.AvailableWeapons.Length;weapon++)
                    {
                        flow.ShowPreparation();bootstrap.SelectNextTank(tank);Check(bootstrap.SelectNextShield(shield),"Legal shield rejected");flow.SelectTheme(theme);Check(flow.SelectStartingWeapon(weapon),"Starting weapon rejected");flow.StartBattle();Time.timeScale=6;
                        Check(match.IsRunning&&match.CurrentPlayerIndex==0,"Matrix match did not start");
                        Check(terrain.CraterCount==0,"New match retained craters");
                        var player=Player(0);Check(player.GetComponent<TankLoadout>().Definition==bootstrap.TankOptions[tank],"Wrong class");
                        Check(player.GetComponent<HealthComponent>().MaxShield==bootstrap.ShieldOptions[shield].capacity,"Wrong shield");
                        yield return new WaitForSeconds(.20f);
                        float angle=weapon%3==0?25:weapon%3==1?45:70;
                        var shell=actions.Fire(0,new Vector2(Mathf.Cos(angle*Mathf.Deg2Rad),Mathf.Sin(angle*Mathf.Deg2Rad)));
                        Check(shell!=null&&shell.Shot!=null,"Matrix shot missing");
                        bool rejected=false;try{actions.Fire(0,Vector2.up);}catch(InvalidOperationException){rejected=true;}
                        Check(rejected,"Second simultaneous shot accepted");
                        float deadline=Time.realtimeSinceStartup+12;
                        while(match.IsRunning&&(match.State==MatchState.ProjectileFlying||match.State==MatchState.Resolving))
                        {
                            Check(Time.realtimeSinceStartup<deadline,"Projectile or turn resolution stuck");Measure();yield return null;
                        }
                        Check(match.ActiveShotProjectileCount==0,"Shot group leaked");
                        yield return null;
                        Check(FindObjectsByType<Projectile>().Length==0,"Live projectile leaked beyond shot completion");
                        Check(feedback.ActiveEffects<=BattleFeedback.MaximumEffects&&feedback.ActiveLabels<=BattleFeedback.MaximumLabels,"Feedback limit exceeded");
                        if(match.IsRunning){flow.Pause();flow.RequestLeave();flow.ConfirmLeave();}else flow.Home();
                        yield return null;
                        Check(!match.IsRunning&&!match.IsPaused,"Exit retained running/paused match");
                        matrixPassed++;Pass($"matrix {matrixPassed:000}: {bootstrap.TankOptions[tank].tankId}/{bootstrap.ShieldOptions[shield].shieldId}/theme-{theme}/{input.AvailableWeapons[weapon].WeaponId}");
                    }
                }
                Check(matrixPassed==168,"Unexpected matrix size");
                Check(terrain.GetComponent<SpriteRenderer>().sprite.texture==terrainTexture,"Terrain allocated replacement textures per match");
                Pass("168 combinations restore terrain and finish every projectile group");
            }
            flow.ShowPreparation();bootstrap.SelectNextTank(1);bootstrap.SelectNextShield(1);flow.SelectTheme(1);flow.SelectStartingWeapon(0);flow.StartBattle();Time.timeScale=6;
            int coinsBefore=store.Coins,winsBefore=store.Wins,turn=-1,shots=0;
            float fullDeadline=Time.realtimeSinceStartup+180;
            while(match.IsRunning)
            {
                Check(Time.realtimeSinceStartup<fullDeadline,"Complete match timed out");
                if(match.State==MatchState.TurnActive&&turn!=match.CompletedTurns)
                {
                    turn=match.CompletedTurns;int index=match.CurrentPlayerIndex;
                    var player=Player(index);var target=Player(1-index);
                    Vector2 delta=target.transform.position-player.transform.position;float x=Mathf.Abs(delta.x),y=delta.y;
                    var weapon=player.GetComponent<TankGame.Weapons.WeaponController>().CurrentWeapon;
                    double g=-Physics2D.gravity.y*weapon.ProjectilePrefab.GetComponent<Rigidbody2D>().gravityScale;
                    double speed2=weapon.LaunchSpeed*weapon.LaunchSpeed;
                    double discriminant=speed2*speed2-g*(g*x*x+2*y*speed2);
                    float angle=discriminant>=0&&g>.001?(float)(Math.Atan((speed2+Math.Sqrt(discriminant))/(g*x))*Mathf.Rad2Deg):45;
                    angle=Mathf.Clamp(angle,10,85)*Mathf.Deg2Rad;
                    actions.Fire(index,new Vector2(Mathf.Sign(delta.x)*Mathf.Cos(angle),Mathf.Sin(angle)));shots++;
                }
                Measure();yield return null;
            }
            Check(match.FinalResult!=null&&match.EndReason!=MatchEndReason.Surrender&&match.EndReason!=MatchEndReason.Cancelled,"Complete match lacked natural result");
            Check(flow.CurrentScreen==InterfaceScreen.Result&&bootstrap.Progression.RewardSaved,"Result/reward not saved");
            int after=store.Coins;bootstrap.Progression.RetrySettlement();Check(store.Coins==after,"Reward duplicated");
            Pass($"complete real-physics match: {shots} shots, {match.CompletedTurns} turns, {match.EndReason}, reward {after-coinsBefore}, wins {store.Wins-winsBefore}");
            var snapshot=store.Snapshot;
            Check(snapshot.selectedTheme==1&&snapshot.startingWeapon==input.AvailableWeapons[0].WeaponId,"Preferences lost");
            Pass("natural result persists once and setup preferences remain intact");
            flow.Home();yield return new WaitForSeconds(.9f);
            Check(FindObjectsByType<Projectile>().Length==0&&feedback.ActiveEffects==0&&feedback.ActiveLabels==0,"Cleanup leaked gameplay objects");
            Pass("return home leaves no projectile, damage label or impact effect");
            for(int i=0;i<200;i++)feedback.Burst(Vector2.zero,Color.yellow,1,10,.2f);
            Check(feedback.ActiveEffects==64,"Stress budget not respected");feedback.Clear();
            Pass("200 impact bursts stay within the 64-effect budget");
            report.AppendLine($"METRICS peak projectiles={peakProjectiles}, peak effects={peakEffects}, allocated memory start={initialMemory}, end={Profiler.GetTotalAllocatedMemoryLong()}, elapsed={Time.realtimeSinceStartup-started:0.0}s; Editor figures are not phone performance measurements.");
            Check(errors.Count==0,"Runtime logged errors: "+string.Join(" | ",errors));
            Pass("no runtime error, exception or assertion during qualification");
        }
        private BackendTestPlayerController Player(int index)
        {foreach(var player in FindObjectsByType<BackendTestPlayerController>())if(player.PlayerIndex==index)return player;throw new Exception("Player missing");}
        private void Measure(){peakProjectiles=Mathf.Max(peakProjectiles,FindObjectsByType<Projectile>().Length);peakEffects=Mathf.Max(peakEffects,feedback.ActiveEffects);}
        private void Complete(bool success)
        {
            Application.logMessageReceived-=OnLog;Application.runInBackground=previousRunInBackground;Time.timeScale=1;if(ai!=null)ai.enabled=aiEnabled;
            Finished=true;Succeeded=success;running=false;
            report.AppendLine(success?$"PASS all phase-seven checks ({passed} cases; matrix {matrixPassed}).":"FAIL qualification incomplete");
            File.WriteAllText(Path.Combine(output,"PhaseSevenVerification.txt"),report.ToString());
            Debug.Log("Phase seven qualification "+(success?"PASS":"FAIL")+"; report: "+output);
            if(standalone)Application.Quit(success?0:1);
        }
        private void OnDestroy(){Application.logMessageReceived-=OnLog;}
    }
}
