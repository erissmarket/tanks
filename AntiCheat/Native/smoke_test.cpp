#include "fire_guard.hpp"
#include <limits>

using namespace tank_anticheat;

int main()
{
    FireGuard guard(7, 100, 200);
    HostState host{7, 1, Mode::turn_based, true, false, true, 0, 2, 0};
    FireIntent shot{7, 1, 1, 0, 1, 0, 0.8f};
    if (guard.check(200, shot, host) != Result::wrong_owner) return 1;
    shot.direction_x = std::numeric_limits<float>::quiet_NaN();
    if (guard.check(100, shot, host) != Result::invalid) return 2;
    shot.direction_x = 1;
    if (guard.check(100, shot, host) != Result::accepted) return 3;
    if (guard.check(100, shot, host) != Result::stale) return 4;
    shot.sequence = 2;
    host.current_player = 1;
    if (guard.check(100, shot, host) != Result::wrong_turn) return 5;
    host.mode = Mode::simultaneous;
    host.reload_remaining = 1;
    if (guard.check(100, shot, host) != Result::reloading) return 6;
    host.reload_remaining = 0;
    host.ammunition = 0;
    if (guard.check(100, shot, host) != Result::empty_ammo) return 7;
    host.ammunition = 2;
    if (guard.check(100, shot, host) != Result::accepted) return 8;
    shot.player = 1;
    shot.sequence = 1;
    if (guard.check(200, shot, host) != Result::accepted) return 9;
    host.paused = true;
    shot.sequence = 3;
    if (guard.check(200, shot, host) != Result::inactive) return 10;
    return 0;
}
