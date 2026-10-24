using System;
using TankGame.Core;
using TankGame.Tank;
using TankGame.Weapons;
using TankGame.Data;
using UnityEngine;

namespace TankGame.Testing
{
    public sealed class BackendTestBootstrap : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager = null;
        [SerializeField] private TurnManager turnManager = null;
        [SerializeField] private MatchFlowController matchFlow = null;
        [SerializeField] private GameplayActionController gameplayActions = null;
        [SerializeField] private BackendTestPlayerController playerZero = null;
        [SerializeField] private BackendTestPlayerController playerOne = null;
        [SerializeField] private Transform playerZeroMuzzle = null;
        [SerializeField] private Transform playerOneMuzzle = null;
        [SerializeField] private float playerZeroDefaultAim = 45f;
        [SerializeField] private float playerOneDefaultAim = 135f;
        private Vector3 playerZeroStartPosition;
        private Vector3 playerOneStartPosition;
        private float playerZeroStartRotation, playerOneStartRotation;
        [SerializeField] private ShieldData[] shieldOptions;
        [SerializeField] private int nextShieldIndex = 1;
        [SerializeField] private TankDefinition[] tankOptions;
        [SerializeField] private int nextTankIndex = 1;
        [SerializeField] private TankDefinition opponentTank;
        [SerializeField] private bool startInGarage;
        [SerializeField] private ProgressionController progression;
        public ProgressionController Progression=>progression;
        public bool StartInGarage => startInGarage;
        public TankDefinition[] TankOptions => tankOptions;
        public ShieldData[] ShieldOptions => shieldOptions;
        public TankDefinition NextTank => tankOptions!=null&&tankOptions.Length>0?tankOptions[Mathf.Clamp(nextTankIndex,0,tankOptions.Length-1)]:null;
        public TankDefinition OpponentTank => opponentTank;
        public bool CanStartConfiguredMatch => gameManager!=null&&(gameManager.CurrentState==MatchState.None||CanRestart);
        public void SelectNextTank(int index)
        {
            if(tankOptions==null||index<0||index>=tankOptions.Length)throw new ArgumentOutOfRangeException(nameof(index));
            if(progression!=null&&!progression.Store.OwnsTank(tankOptions[index]))return;
            int oldTank=nextTankIndex,oldShield=nextShieldIndex;
            nextTankIndex=index;
            if(!NextTank.Supports(NextShield))
                for(int i=shieldOptions.Length-1;i>=0;i--)
                    if(NextTank.Supports(shieldOptions[i])&&(progression==null||progression.Store.OwnsShield(shieldOptions[i]))){nextShieldIndex=i;break;}
            if(!SaveSelection()){nextTankIndex=oldTank;nextShieldIndex=oldShield;}
        }
        public bool SelectNextShield(int index)
        {
            if(shieldOptions==null||index<0||index>=shieldOptions.Length)throw new ArgumentOutOfRangeException(nameof(index));
            if(NextTank!=null&&!NextTank.Supports(shieldOptions[index]))return false;
            if(progression!=null&&!progression.Store.OwnsShield(shieldOptions[index]))return false;
            int before=nextShieldIndex;nextShieldIndex=index;
            if(SaveSelection())return true;nextShieldIndex=before;return false;
        }
        private bool SaveSelection()=>progression==null||progression.Store.Select(NextTank,NextShield);
        private void RestoreSelection()
        {
            if(progression==null)return;var saved=progression.Store.Snapshot;
            int tank=Array.FindIndex(tankOptions,value=>value.tankId==saved.selectedTank&&progression.Store.OwnsTank(value));
            nextTankIndex=tank>=0?tank:1;
            int shield=Array.FindIndex(shieldOptions,value=>value.shieldId==saved.selectedShield&&NextTank.Supports(value)&&progression.Store.OwnsShield(value));
            nextShieldIndex=shield>=0?shield:1;
        }
        public ShieldData NextShield => shieldOptions != null && shieldOptions.Length > 0 ? shieldOptions[Mathf.Clamp(nextShieldIndex, 0, shieldOptions.Length - 1)] : null;
        public void CycleNextShield()
        {
            if(shieldOptions==null)return;
            for(int i=1;i<=shieldOptions.Length;i++)if(SelectNextShield((nextShieldIndex+i)%shieldOptions.Length))return;
        }
        private void ApplyLoadout()
        {
            if(NextTank!=null)
            {
                ApplyTank(playerZero,NextTank,NextShield,false);
                var enemyShield=NextShield;
                if(opponentTank!=null&&!opponentTank.Supports(enemyShield))enemyShield=shieldOptions[0];
                ApplyTank(playerOne,opponentTank!=null?opponentTank:NextTank,enemyShield,true);
            }
            foreach (var player in new[] { playerZero, playerOne })
            {
                if (NextTank==null&&shieldOptions != null && shieldOptions.Length > 0) player.GetComponent<HealthComponent>().EquipShield(NextShield);
                player.GetComponent<WeaponController>().ResetAmmunition();
            }
        }
        private void ApplyTank(BackendTestPlayerController player,TankDefinition tank,ShieldData shield,bool enemy)
        {
            var loadout=player.GetComponent<TankLoadout>();if(loadout==null)loadout=player.gameObject.AddComponent<TankLoadout>();
            loadout.Apply(tank,shield,enemy,!enemy&&progression!=null?progression.Store.Upgrades(tank.tankId):null,progression!=null?progression.Rules:null);
        }

        public bool CanRestart => gameManager != null && gameManager.CurrentState == MatchState.MatchEnded;
        public bool CanIssueActions => gameManager != null && !matchFlow.IsPaused && gameManager.CurrentState == MatchState.TurnActive;

        private void Start()
        {
            if (gameManager == null || turnManager == null || matchFlow == null || gameplayActions == null
                || playerZero == null || playerOne == null || playerZeroMuzzle == null || playerOneMuzzle == null)
                throw new InvalidOperationException("BackendTest bootstrap references must all be assigned.");

            playerZeroStartPosition = playerZero.transform.position;
            playerOneStartPosition = playerOne.transform.position;
            playerZeroStartRotation = playerZero.transform.eulerAngles.z; playerOneStartRotation = playerOne.transform.eulerAngles.z;
            InitializePlayer(playerZero, 0);
            InitializePlayer(playerOne, 1);
            RestoreSelection();
            ApplyLoadout();
            matchFlow.Initialize(gameManager, turnManager, playerZero, playerOne);
            gameplayActions.Initialize(gameManager, turnManager, matchFlow);
            RegisterPlayer(playerZero, 0, playerZeroMuzzle);
            RegisterPlayer(playerOne, 1, playerOneMuzzle);
            playerZero.GetComponent<TankAim>().SetAimAngle(playerZeroDefaultAim);
            playerOne.GetComponent<TankAim>().SetAimAngle(playerOneDefaultAim);
            if(!startInGarage&&(progression==null||progression.BeginBattle()))matchFlow.StartMatch();
        }

        public void RestartTestMatch()
        {
            if (!CanRestart)
                throw new InvalidOperationException("Restart BackendTest only after MatchEnded.");
            StartConfiguredMatch();
        }

        public void StartConfiguredMatch()
        {
            if(!CanStartConfiguredMatch)throw new InvalidOperationException("Finish the current match before starting another.");
            if(progression!=null&&!progression.BeginBattle())return;
            foreach (var terrain in UnityEngine.Object.FindObjectsByType<TankGame.Terrain.DestructibleTerrain>()) terrain.ResetSurface();
            RestorePlayer(playerZero, playerZeroStartPosition, playerZeroDefaultAim);
            RestorePlayer(playerOne, playerOneStartPosition, playerOneDefaultAim);
            playerZero.GetComponent<Rigidbody2D>().rotation = playerZeroStartRotation;
            playerOne.GetComponent<Rigidbody2D>().rotation = playerOneStartRotation;
            Physics2D.SyncTransforms();
            ApplyLoadout();
            matchFlow.StartMatch();
        }

        private static void RestorePlayer(BackendTestPlayerController player, Vector3 position, float angle)
        {
            player.GetComponent<HealthComponent>().ResetHealth();
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            body.position = position;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            player.GetComponent<TankAim>().SetAimAngle(angle);
        }

        private static void InitializePlayer(BackendTestPlayerController player, int index)
        {
            player.Initialize(index);
            TankController tank = player.GetComponent<TankController>();
            HealthComponent health = player.GetComponent<HealthComponent>();
            if (tank == null || health == null)
                throw new InvalidOperationException("Each BackendTest player requires TankController and HealthComponent.");
            tank.Initialize(index);
            health.ResetHealth();
        }

        private void RegisterPlayer(BackendTestPlayerController player, int index, Transform muzzle)
        {
            gameplayActions.RegisterPlayer(index, player, player.GetComponent<TankMotor>(),
                player.GetComponent<TankAim>(), player.GetComponent<WeaponController>(), muzzle);
            matchFlow.RegisterHealth(index, player.GetComponent<HealthComponent>());
        }
    }
}
