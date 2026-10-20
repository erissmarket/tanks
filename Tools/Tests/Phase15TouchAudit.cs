using System;
using System.IO;
using System.Text;
using UnityEngine;
using TankGame.Presentation;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var log = new StringBuilder(); int checks = 0, commits = 0, fires = 0, aims = 0;
        Action<bool,string> check = (ok,name) => { log.AppendLine((ok?"PASS ":"FAIL ")+name); if(!ok)throw new Exception(name); checks++; };
        var root = new GameObject("Phase15 synthetic touch validation");
        try
        {
            var ui = root.AddComponent<GameplayTouchControls>(); bool permission = true;
            ui.Bind(() => permission, _ => { }, () => { }, (d,p) => aims++, () => {fires++; ui.CancelInput(false);},
                d => AimPowerJoystick.Constrain(d,false,0), () => Vector2.up, () => 3, true, () => commits++);
            ui.PointerDown(1, GameplayTouchControls.JoystickCenter + new Vector2(60,-60));
            var direction = ui.Direction; float power = ui.Power;
            ui.PointerUp(1);
            check(commits==1 && fires==0, "release reliably commits final aim without firing");
            check(ui.Direction==direction && ui.Power==power, "release preserves selected direction and power");
            ui.PointerDown(2, GameplayTouchControls.JoystickCenter + new Vector2(80,-30));
            ui.PointerDown(3, GameplayTouchControls.FireRect.center);
            check(fires==1 && commits==1, "separate Fire cancels pointer without redundant post-Fire aim commit");
            ui.PointerDown(4, GameplayTouchControls.JoystickCenter + new Vector2(50,-50));
            ui.CancelInput(); check(commits==2, "pointer cancellation commits authorized final intent");
            ui.PointerDown(5, GameplayTouchControls.JoystickCenter + new Vector2(40,-50));
            permission=false; ui.PointerUp(5); check(commits==2, "turn loss does not submit a final command");
            int previous=aims; ui.PointerDown(6, GameplayTouchControls.JoystickCenter + new Vector2(50,-50));
            check(aims==previous, "inactive controller cannot create anticipated aim intent");
            permission=true; ui.PointerDown(7, GameplayTouchControls.JoystickCenter + new Vector2(50,-50));
            ui.SendMessage("OnApplicationFocus",false);check(commits==3,"focus loss commits and clears held aim once");
            previous=aims;ui.PointerDrag(7,GameplayTouchControls.JoystickCenter+new Vector2(90,-10));check(aims==previous,"no stale pointer drag after focus cancellation");
            log.AppendLine("TOTAL PASS "+checks);
        }
        finally { UnityEngine.Object.Destroy(root); File.WriteAllText("Builds/Phase15/TouchValidation.txt",log.ToString()); }
        result.Log("Phase15 touch finalization checks: "+checks);
    }
}
