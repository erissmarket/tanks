# LAN discovery checks

The small console runner compiles the actual Unity-independent discovery sources, without NuGet test libraries. It requires the locally installed .NET 10 SDK; Unity player compilation is validated separately.

From the repository root:

```powershell
dotnet build Tools/Tests/LanDiscoveryTests.csproj
dotnet Builds/Phase12/Tests/net10.0/LanDiscoveryTests.dll
```

Run two terminals/processes for independent UDP broadcast validation (a local firewall/sandbox can block UDP):

```powershell
dotnet Builds/Phase12/Tests/net10.0/LanDiscoveryTests.dll host
dotnet Builds/Phase12/Tests/net10.0/LanDiscoveryTests.dll browser
```

The host runs five seconds; the browser runs thirteen seconds and requires both room arrival and expiry. Dedicated test discovery ports are 19778–19780, with no NGO dependency. Loopback/socket unit checks, simulated interface-change/clock inputs, coordinator delegate tests, and independent production-broadcast processes are different kinds of evidence; none establishes Android hotspot compatibility.

The existing development-only LanIndependentPeerAudit additionally supports `-phase12-discovery` alongside its mandatory `-phase11c host|client -audit-dir <directory>` arguments, only in LanGameplayTest. It selects a discovered `Phase12 Audit` room for automated integration testing. Normal UI requires explicit room selection and JOIN. Existing command-file tests remain available; no driver activates in normal gameplay.

For phone acceptance, enable the hotspot manually on Phone A, connect Phone B to it in system settings, then CREATE ROOM on A / FIND ROOMS, select, JOIN on B. Repeat on ordinary Wi-Fi, exercise both players' existing controls, complete a match/restart, and verify disconnect/withdrawal and manual IP fallback. No hotspot API, location/nearby permission, multicast lock, cloud service, or firewall change is introduced. Target SDK36 uses existing INTERNET permission under default Android local-network policy; opt-in Android16 restrictions, OEM filtering/isolation and future target37 permission changes require separate device validation.
