# Native anti-cheat sketches

Independent C11 and C++17 examples, outside Unity Assets.

- C implements a per-peer token bucket with bounded burst/refill and checks for clock rollback, invalid values and corrupt state.
- C++ implements fire intent checks for authenticated ownership, session/epoch, replay, turn-based versus simultaneous permission, finite direction/power, authoritative reload and ammunition.

Use one initialized bucket per peer and one FireGuard per session on a serialized authority thread. The C limiter and C++ guard can be used independently. After accepting a fire intent, gameplay must atomically spend ammunition and start reload before processing another intent. These small examples are preliminary command checks and require authoritative integration to protect a running game.

## Build and check

~~~sh
cmake -S . -B build
cmake --build build --config Release
ctest --test-dir build -C Release --output-on-failure
~~~

The C test checks 14 input, burst, refill and clock cases. The C++ test checks 10 ownership, replay, invalid-input and lifecycle cases. Both compile with warnings treated as errors.
