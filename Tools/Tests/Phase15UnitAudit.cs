using System;
using System.IO;
using System.Text;
using UnityEngine;
using Unity.Collections;
using Unity.Netcode;
using TankGame.Multiplayer;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var log = new StringBuilder(); int checks = 0;
        Action<bool, string> check = (ok, name) => { log.AppendLine((ok ? "PASS " : "FAIL ") + name); if (!ok) throw new Exception(name); checks++; };
        Func<Action, bool> rejected = action => { try { action(); return false; } catch (ArgumentException) { return true; } };
        try
        {
            var sender = new AimSampleCoalescer();
            sender.Offer(Vector2.up, .5f); check(sender.TryTake(0, out _, out _), "first aim sample immediate at sender boundary");
            sender.Offer(Vector2.left, .7f); sender.Offer(Vector2.right, .8f);
            check(!sender.TryTake(.01, out _, out _), "unsent changes rate limited");
            check(sender.TryTake(.051, out var direction, out float power) && direction == Vector2.right && power == .8f, "obsolete unsent aims coalesced to newest");
            sender.Offer(new Vector2(1, .001f).normalized, .801f);
            check(!sender.TryTake(.11, out _, out _), "small angle and power changes suppressed by deadband");
            check(sender.TryTake(.31, out _, out _), "held final intent refreshes despite no change");
            sender.Reset(); check(!sender.TryTake(1, out _, out _), "reset cancels unsent aim and refresh");
            int sends = 0;
            for (int i = 0; i <= 1000; i++) { sender.Offer(new Vector2(Mathf.Cos(i * .01f), Mathf.Sin(i * .01f)), i / 1000f); if (sender.TryTake(i / 1000d, out _, out _)) sends++; }
            check(sends >= 19 && sends <= 21, "1000Hz offers remain bounded to20Hz");
            check(rejected(() => sender.Offer(new Vector2(float.NaN, 1), .5f)), "nonfinite direction rejected");
            check(rejected(() => sender.Offer(Vector2.one, .5f)), "non-normalized intent rejected");
            check(rejected(() => sender.Offer(Vector2.up, -1)), "negative power rejected");
            check(rejected(() => sender.Offer(Vector2.up, float.PositiveInfinity)), "infinite power rejected");
            var sample = new AimObservation { Version = GameplayNetworkCommand.ProtocolVersion, Session = 10, Turn = 2, Frame = 1,
                ZeroAngle = 10, OneAngle = 40, ZeroPower = .5f, OnePower = .7f, ZeroSequence = 3, OneSequence = 5 };
            check(sample.IsValid, "valid bounded aim observation");
            using (var writer = new FastBufferWriter(128, Allocator.Temp))
            {
                writer.WriteValueSafe(sample); check(writer.Length == AimObservation.WireSize && writer.Length == 57, "aim observation exactly57bytes");
                using (var reader = new FastBufferReader(writer, Allocator.Temp)) { reader.ReadValueSafe(out AimObservation roundtrip); check(roundtrip.IsValid && roundtrip.OneSequence == 5 && roundtrip.OnePower == .7f, "aim observation roundtrip"); }
            }
            var buffer = new AimObservationBuffer(); check(buffer.Push(sample, 10, 2, 0), "first valid observation accepted");
            check(!buffer.Push(sample, 10, 2, .01), "duplicate observation rejected");
            sample.Frame = 2; sample.ZeroAngle = 20; sample.OneAngle = 50;
            check(buffer.Push(sample, 10, 2, .05), "new observation accepted");
            check(Mathf.Abs(buffer.Evaluate(0, .06, .035f) - 15) < .001f, "short interpolation between known samples");
            check(buffer.Evaluate(0, .06, 0) == 20, "zero-delay direct authoritative comparison");
            check(buffer.Evaluate(1, .06, .035f) == 45, "mirrored player uses bounded local angle interpolation");
            check(buffer.Evaluate(0, .5, .035f) == 20, "no extrapolation past newest observation");
            check(buffer.Age(.5) > .3, "expired observations detectable on local clock");
            sample.Frame = 1; check(!buffer.Push(sample, 10, 2, .07), "reordered old frame rejected");
            sample.Frame = 3; check(!buffer.Push(sample, 11, 2, .07), "unrelated session rejected");
            check(!buffer.Push(sample, 10, 3, .07), "unrelated turn rejected");
            sample.Version = 2; check(!buffer.Push(sample, 10, 2, .07), "v2 observation incompatible with v3"); sample.Version = GameplayNetworkCommand.ProtocolVersion;
            sample.OnePower = float.NaN; check(!sample.IsValid, "NaN power observation rejected"); sample.OnePower = .7f;
            sample.ZeroAngle = 91; check(!sample.IsValid, "out-of-bounds observation angle rejected"); sample.ZeroAngle = 20;
            check(!buffer.Push(sample, 10, 2, double.NaN), "invalid local receipt time rejected");
            buffer.Reset(); check(!buffer.HasValue && double.IsPositiveInfinity(buffer.Age(1)), "restart/disconnect clear bounded observation history");
            var fire = new GameplayNetworkCommand(10, 2, 8, 1, GameplayNetworkAction.Fire, -.6f, .8f, power: .37f);
            check(fire.HasValidValues() && fire.Power == .37f, "reliable Fire independently carries final direction and power");
            check(GameplayNetworkCommand.ProtocolVersion == 3 && LanDiscoveryProtocol.Version == 3, "explicit gameplay and discovery compatibility3");
            log.AppendLine("TOTAL PASS " + checks);
        }
        catch (Exception error) { log.AppendLine("FAIL " + error); throw; }
        finally { File.WriteAllText("Builds/Phase15/UnitValidation.txt", log.ToString()); }
        result.Log("Phase15 pure aim protocol/rate/buffer checks passed: " + checks);
    }
}
