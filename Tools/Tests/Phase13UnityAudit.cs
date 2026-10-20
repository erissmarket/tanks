using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using TankGame.Core;
using TankGame.Tank;
using TankGame.Testing;
using TankGame.Presentation;
using TankGame.Multiplayer;
using TankGame.Projectiles;
using Unity.Collections;
using Unity.Netcode;

// Execute through Unity MCP in the existing MVPIntegrated Play Mode; no scene source is saved.
internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        Application.runInBackground = true;
        var log = new StringBuilder(); int checks = 0;
        Action<bool,string> check = (ok,name) => {log.AppendLine((ok ? "PASS " : "FAIL ")+name);if(!ok)throw new Exception(name);checks++;};
        Func<Action,bool> rejected = action => {try{action();return false;}catch(ArgumentException){return true;}catch(InvalidOperationException){return true;}};
        var actions=UnityEngine.Object.FindAnyObjectByType<GameplayActionController>();
        var flow=UnityEngine.Object.FindAnyObjectByType<MatchFlowController>();
        var game=UnityEngine.Object.FindAnyObjectByType<GameManager>();
        var bootstrap=UnityEngine.Object.FindAnyObjectByType<BackendTestBootstrap>();
        UnityEngine.Object.FindAnyObjectByType<TankGame.AI.SimpleArtilleryAI>().enabled=false;
        UnityEngine.Object.FindAnyObjectByType<IntegratedInput>().enabled=false;
        var tanks=UnityEngine.Object.FindObjectsByType<TankController>();var tank=Array.Find(tanks,t=>t.PlayerIndex==0);
        var body=tank.GetComponent<Rigidbody2D>();
        Action reset=()=>{flow.EndMatch();foreach(var p in UnityEngine.Object.FindObjectsByType<Projectile>()){p.gameObject.SetActive(false);UnityEngine.Object.Destroy(p.gameObject);}bootstrap.RestartTestMatch();};
        reset();
        try
        {
            Vector2 dir;float power;
            check(!AimPowerJoystick.TryMap(Vector2.zero,100,.15f,out dir,out power),"joystick dead zone preserves selection");
            check(AimPowerJoystick.TryMap(new Vector2(60,60),100,.15f,out dir,out power)&&Mathf.Abs(Vector2.Angle(dir,Vector2.right)-45)<.01f&&power>0&&power<1,"drag simultaneously maps angle and intermediate power");
            check(AimPowerJoystick.TryMap(new Vector2(500,0),100,.15f,out dir,out power)&&power==1,"drag radius clamps maximum power");
            check(rejected(()=>AimPowerJoystick.TryMap(new Vector2(float.NaN,0),100,.15f,out dir,out power)),"NaN drag rejected");
            check(rejected(()=>AimPowerJoystick.TryMap(Vector2.one,0,.15f,out dir,out power)),"invalid joystick radius rejected");
            check(rejected(()=>AimPowerJoystick.Constrain(Vector2.one,false,float.PositiveInfinity)),"nonfinite body angle rejected");
            check(Vector2.Angle(AimPowerJoystick.Constrain(Vector2.down,false,0),Vector2.right)<.01f,"lower aim limit");
            check(Vector2.Angle(AimPowerJoystick.Constrain(Vector2.left,false,0),Vector2.up)<.01f,"upper aim limit");
            var mirrored=AimPowerJoystick.Constrain(new Vector2(-1,1),true,0);check(mirrored.x<0&&mirrored.y>0,"player1 mirrored world aim");
            var uiRoot=new GameObject("Phase13 pointer unit checks");var ui=uiRoot.AddComponent<GameplayTouchControls>();
            int moves=0,stops=0,aims=0,fires=0;bool permission=true;
            ui.Bind(()=>permission,_=>moves++,()=>stops++,(d,p)=>aims++,()=>fires++,d=>AimPowerJoystick.Constrain(d,false,0),()=>Vector2.up,()=>3,true);
            ui.PointerDown(1,GameplayTouchControls.LeftRect.center);check(moves==1,"pointer press starts Move");
            ui.PointerDown(1,GameplayTouchControls.LeftRect.center);check(moves==1,"duplicate pointer-down ignored");
            ui.PointerDown(2,GameplayTouchControls.JoystickCenter+new Vector2(60,-60));check(aims==1&&moves==1,"independent movement/joystick multitouch");
            var chosen=ui.Direction;float chosenPower=ui.Power;ui.PointerUp(2);check(ui.Direction==chosen&&ui.Power==chosenPower&&fires==0,"joystick release preserves aim/power and never fires");
            ui.PointerUp(1);check(stops==1,"pointer release stops Move");
            ui.PointerDown(3,GameplayTouchControls.RightRect.center);ui.PointerDrag(3,Vector2.zero);check(stops==2,"pointer exit cancels movement");
            ui.PointerDown(4,GameplayTouchControls.LeftRect.center);ui.CancelInput();check(stops==3,"pointer cancellation stops movement");
            ui.PointerDown(5,GameplayTouchControls.LeftRect.center);ui.SendMessage("OnApplicationFocus",false);check(stops==4,"lost focus stops movement");
            ui.PointerDown(6,GameplayTouchControls.RightRect.center);check(moves==4,"unfocused input cannot restart movement");ui.SendMessage("OnApplicationFocus",true);
            ui.PointerDown(7,GameplayTouchControls.LeftRect.center);ui.SendMessage("OnApplicationPause",true);check(stops==5,"pause stops movement");ui.SendMessage("OnApplicationPause",false);
            ui.PointerDown(8,GameplayTouchControls.FireRect.center);ui.PointerDown(8,GameplayTouchControls.FireRect.center);check(fires==1,"separate Fire is exactly once per press");ui.PointerUp(8);
            permission=false;ui.PointerDown(9,GameplayTouchControls.FireRect.center);check(fires==1,"inactive controls cannot fire");
            UnityEngine.Object.Destroy(uiRoot);
            check(rejected(()=>actions.Fire(0,Vector2.up,float.NaN))&&game.CurrentState==MatchState.TurnActive,"invalid power keeps match usable");
            var weapon=tank.GetComponent<TankGame.Weapons.WeaponController>().CurrentWeapon;
            float baseDamage = weapon.BaseDamage;
            foreach(float p in new[]{0f,.5f,1f})
            {
                var shot=actions.Fire(0,Vector2.up,p);
                check(Mathf.Abs(shot.GetComponent<Rigidbody2D>().linearVelocity.magnitude-weapon.GetLaunchSpeed(p))<.001f,"authoritative launch speed power="+p);
                check(shot.OwnerPlayerIndex==0&&weapon.BaseDamage==baseDamage,"power preserves owner and weapon damage configuration="+p);
                check(rejected(()=>actions.Fire(0,Vector2.up,p)),"second Fire rejected power="+p);reset();
            }
            check(Mathf.Abs(weapon.GetLaunchSpeed(1)-weapon.LaunchSpeed)<.001f,"legacy full-power firing and AI speed unchanged");
            var weaponController=tank.GetComponent<TankGame.Weapons.WeaponController>();
            var configured=UnityEngine.Object.Instantiate(weapon);
            var configuration=new SerializedObject(configured);configuration.FindProperty("maximumLaunchPower").floatValue=1.5f;configuration.ApplyModifiedPropertiesWithoutUndo();
            weaponController.AssignWeapon(configured);
            var legacyShot=actions.Fire(0,Vector2.up);
            check(Mathf.Abs(legacyShot.GetComponent<Rigidbody2D>().linearVelocity.magnitude-configured.LaunchSpeed)<.001f,"legacy AI Fire preserves base speed with custom maximum power");reset();
            var maximumShot=actions.Fire(0,Vector2.up,1);
            check(Mathf.Abs(maximumShot.GetComponent<Rigidbody2D>().linearVelocity.magnitude-configured.LaunchSpeed*1.5f)<.001f,"explicit maximum power respects custom weapon configuration");reset();
            weaponController.AssignWeapon(weapon);UnityEngine.Object.Destroy(configured);
            var packet=new GameplayNetworkCommand(1,1,1,0,GameplayNetworkAction.AimPower,0,1,power:.4f);
            using(var writer=new FastBufferWriter(GameplayNetworkCommand.WireSize,Allocator.Temp))
            {var w=writer;packet.WriteTo(ref w);check(w.Length==39,"command v2 exactly39 bytes");using(var reader=new FastBufferReader(writer,Allocator.Temp)){check(GameplayNetworkCommand.TryRead(reader,out var parsed)&&parsed.Power==.4f&&parsed.HasValidValues(),"command power roundtrip");}}
            check(!new GameplayNetworkCommand(1,1,1,0,GameplayNetworkAction.Fire,0,1,power:float.NaN).HasValidValues(),"wire NaN power rejected");
            check(!new GameplayNetworkCommand(1,1,1,0,GameplayNetworkAction.AimPower,0,0,power:.5f).HasValidValues(),"wire nonnormalized aim rejected");
            var snapshot=new GameplayNetworkState(1,1,MatchState.TurnActive,0,-2,new NetworkTankState(Vector2.zero,0,45,100,100,3,.5f),new NetworkTankState(Vector2.zero,0,45,100,100,3,1),0,false,-1,Vector2.zero,Vector2.zero,0,Vector2.zero,0);
            using(var writer=new FastBufferWriter(GameplayNetworkState.WireSize,Allocator.Temp)){writer.WriteValueSafe(snapshot);check(writer.Length==142,"snapshot v2 exactly142 bytes");}
            actions.Move(0,1);body.linearVelocity=new Vector2(2,7);actions.Stop(0);check(body.linearVelocity.y==7,"Stop preserves vertical impulse");
            body.linearVelocity=new Vector2(2,7);actions.Stop(0);check(body.linearVelocity.x==2&&body.linearVelocity.y==7,"idle Stop does not erase external impulse");reset();
        }
        catch(Exception error){File.WriteAllText("Builds/Phase13/FocusedValidation.txt",log+"FAIL "+error);throw;}

        int phase=0;double deadline=EditorApplication.timeSinceStartup+35,until=EditorApplication.timeSinceStartup+.8;float start=0,stopped=0;GameObject wall=null;float originalY=0;
        EditorApplication.CallbackFunction tick=null;
        tick=()=>
        {
            try
            {
                if(!EditorApplication.isPlaying||EditorApplication.timeSinceStartup>deadline)throw new Exception("Movement audit timeout phase="+phase);
                double time=EditorApplication.timeSinceStartup;
                if(phase==2){actions.Move(0,1);if(actions.GetMovementRemaining(0)>0&&time<until)return;}
                else if(phase==6){actions.Move(0,1);if(time<until)return;}
                else if(time<until)return;
                switch(phase)
                {
                    case 0:start=body.position.x;actions.Move(0,1);until=time+.75;phase=1;break;
                    case 1:stopped=body.position.x;check(stopped>start&&stopped-start<1,"lost Stop expires lease before indefinite travel displacement="+(stopped-start));until=time+.5;phase=3;break;
                    case 3:check(Mathf.Abs(body.position.x-stopped)<.06f,"lease expiry remains stopped");reset();start=body.position.x;until=time+6;phase=2;break;
                    case 2:
                        check(actions.GetMovementRemaining(0)==0,"renewed movement exhausts per-turn budget");check(Mathf.Abs(body.position.x-start)<=3.12f,"budget measured actual horizontal distance within physics-step tolerance");
                        actions.Stop(0);actions.Aim(0,30);check(actions.GetMovementRemaining(0)==0,"Stop/Aim cannot refill budget");stopped=body.position.x;until=time+.5;phase=4;break;
                    case 4:actions.Move(0,1);check(actions.GetMovementRemaining(0)==0&&Mathf.Abs(body.position.x-stopped)<.06f,"repeated input cannot bypass exhausted budget");reset();check(actions.GetMovementRemaining(0)==3,"new match resets budget");actions.Move(0,1);flow.EndMatch();check(!Array.Find(UnityEngine.Object.FindObjectsByType<BackendTestPlayerController>(),p=>p.PlayerIndex==0).CanAct,"MatchEnded disables action");until=time+.2;phase=5;break;
                    case 5:check(Mathf.Abs(body.linearVelocity.x)<.15f,"MatchEnded stops horizontal drive");reset();start=body.position.x;wall=new GameObject("Phase13 collision test wall");wall.transform.position=new Vector3(start+.8f,0,0);wall.AddComponent<BoxCollider2D>().size=new Vector2(.2f,10);until=time+1.2;phase=6;break;
                    case 6:actions.Stop(0);check(body.position.x-start<.6f&&actions.GetMovementRemaining(0)>2.4f,"blocked ground collision charges actual travel rather than hold time");UnityEngine.Object.Destroy(wall);reset();stopped=body.position.x;originalY=body.position.y;body.AddForce(body.mass*new Vector2(1.5f,8),ForceMode2D.Impulse);until=time+.15;phase=7;break;
                    case 7:check(actions.GetMovementRemaining(0)==3&&body.position.x>stopped+.1f,"idle external impulse is not charged and body is not frozen");check(body.position.y>originalY+.2f,"vertical external impulse and gravity remain simulated");EditorApplication.update-=tick;reset();File.WriteAllText("Builds/Phase13/FocusedValidation.txt",log+"TOTAL PASS "+checks);SessionState.SetString("Phase13Focused","PASS "+checks);break;
                }
            }
            catch(Exception error){EditorApplication.update-=tick;File.WriteAllText("Builds/Phase13/FocusedValidation.txt",log+"FAIL "+error);SessionState.SetString("Phase13Focused","FAIL "+error.Message);Debug.LogException(error);}
        };
        EditorApplication.update+=tick;SessionState.SetString("Phase13Focused","RUNNING");result.Log("Focused pointer/power/protocol checks passed; movement lease/budget audit scheduled");
    }
}
