# Phase 15 physical Android acceptance

Desktop validation is evidence for protocol correctness and simulated conditions only. Physical Android responsiveness, multitouch feel, thermal behavior and hotspot performance remain NOT TESTED in this run. The user previously reported a successful two-phone hotspot connection with delayed opposite-phone aim.

## Install without clearing application data

Use the same Phase 15 protocol-v3 APK on both phones. Phase 14 v2 peers are intentionally incompatible. Do not compare one old phone against one new phone.

Run from the repository in PowerShell with the Unity-bundled Android SDK:

```powershell
$adb = 'C:\Program Files\Unity\Hub\Editor\6000.5.10f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe'
& $adb devices -l
& $adb -s PHONE_A_SERIAL install -r 'Builds/Phase15/Android/TankArtillery_LANResponsiveness_Dev.apk'
& $adb -s PHONE_B_SERIAL install -r 'Builds/Phase15/Android/TankArtillery_LANResponsiveness_Dev.apk'
```

Replace the serial placeholders with the listed, authorized devices. Unlock and authorize USB debugging. `install -r` preserves existing app data. If installation reports a signature mismatch, stop: do not uninstall or clear data to bypass it.

## Collect two independent logs

Open two terminals; replace the serials. Do not clear logcat first.

```powershell
& $adb -s PHONE_A_SERIAL logcat -v threadtime -s Unity > Builds/Phase15/PhoneA.log
& $adb -s PHONE_B_SERIAL logcat -v threadtime -s Unity > Builds/Phase15/PhoneB.log
```

Each terminal must define `$adb` as above. Open the app through its launcher. Enter LAN through Main Menu. Enable NET METRICS on each phone; this prints a compact `LAN_METRICS` JSON line twice per second. Stop collection with Ctrl+C after the test.

## Hotspot and shared-router runs

1. Phone A enables hotspot, creates a room, and is Host/Player 0. Phone B joins that hotspot, finds the room, explicitly joins, and is Client/Player 1. Record discovery success and time. If discovery fails, record the failure and separately test manual IP; successful manual connection does not pass discovery.
2. On each player's turn, sweep aim slowly for 20 seconds, then rapidly for 20 seconds, varying power. Observe the local barrel immediately and the opposite phone's barrel. Record perceived delay, smoothness and any snapping with video when possible.
3. Release aim and verify the selected angle/power remain. Drag and immediately tap Fire repeatedly across multiple turns, at low/middle/full power and both mirrored orientations. Verify one shot, expected direction/power, one damage resolution and one turn advance. Wrong-turn input must not animate as accepted aim or fire.
4. Hold movement while aiming with another finger. Release/cancel each finger independently. Check the 3-world-unit actual-travel budget, stop on release and turn transition, and no continuing movement after focus loss or app pause. Host-side movement lease remains 0.35 seconds; do not infer on-screen stop timing from that number alone under transport delay.
5. Test background/resume, disconnect during aim and flight, return to Main Menu, fresh room entry, and Host restart after match completion. Check no stale manager, old barrel interpolation, projectile or result survives into a new session.
6. Play a natural full match, restart, and confirm health, turns, terrain craters/explosions and results on both peers. Repeat over a shared Wi-Fi router. Record device models, Android versions, Wi-Fi band, distance, hotspot/router topology and thermal/power-saving state.
7. For a historical comparison, run both phones on the same old Phase 14 build, then both on Phase 15, with comparable conditions. Preserve logs and app data. Do not infer a physical improvement from desktop results or APK build success.

## Interpret metrics correctly

- Each phone reports its own FPS; correlate Host and Client logs for both values. No clocks are synchronized or subtracted across devices.
- Rates are counter deltas divided by that peer's local elapsed time. Defaults: NGO tick 30; reliable full state 10 Hz; disposable aim and aim observation cap 20 Hz. Frame quantization means measured aim observation rate can be about 16–17 Hz at 60 FPS.
- Acknowledgment duration uses the sender's monotonic request/response clock. `observationMs` is a matching-authoritative-state echo estimate, with sample count; repeated values may match more than one intent. It is not a one-way latency measurement.
- Transport RTT can remain stale when only unreliable aim traffic flows. `visualAgeMs` is age since a local authoritative receipt, not sender-to-screen latency. Interpolation deliberately adds 35 ms; it smooths presentation and does not shorten network transit.
- Payload-byte totals omit NGO/Transport framing and acknowledgments. Sequence gaps indicate missing/reordered observations and are not a measured packet-loss percentage.
- Record actual acceptance or rejection for immediacy, opposite-phone smoothness, interpolation delay, FPS, power/Fire correctness, movement safety, discovery and restart. Do not mark Phase 15 fully accepted until real-device observations and measurements support it.

Windows note: a new executable path may trigger a firewall prompt. This task did not alter firewall policy. Desktop loopback command tests can pass while broadcast discovery is blocked; a fresh independent discovery attempt timed out with such a prompt visible.
