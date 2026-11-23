#pragma once

#include <cmath>
#include <cstdint>

namespace tank_anticheat
{
enum class Mode { turn_based, simultaneous };
enum class Result { accepted, invalid, wrong_owner, stale, inactive, wrong_turn, reloading, empty_ammo };

struct FireIntent
{
    std::uint64_t session, epoch, sequence;
    int player;
    float direction_x, direction_y, power;
};

// Trusted values supplied by a future authoritative caller.
struct HostState
{
    std::uint64_t session, epoch;
    Mode mode;
    bool active, paused, can_act;
    int current_player, ammunition;
    float reload_remaining;
};

// One instance per session, called serially on the authority thread.
// Accepted sequences are consumed before gameplay dispatch.
class FireGuard
{
public:
    FireGuard(std::uint64_t session, std::uint64_t first_peer, std::uint64_t second_peer)
        : session_(session), peers_{first_peer, second_peer},
          valid_(session != 0 && first_peer != second_peer) {}

    Result check(std::uint64_t authenticated_peer, const FireIntent& intent, const HostState& host)
    {
        if (!valid_ || intent.player < 0 || intent.player > 1)
            return Result::invalid;
        if (peers_[intent.player] != authenticated_peer)
            return Result::wrong_owner;
        if (intent.session != session_ || host.session != session_ || host.epoch == 0
            || intent.epoch != host.epoch || intent.sequence == 0
            || intent.sequence <= sequences_[intent.player])
            return Result::stale;
        if (!std::isfinite(intent.direction_x) || !std::isfinite(intent.direction_y)
            || !std::isfinite(intent.power) || intent.power < 0 || intent.power > 1)
            return Result::invalid;
        const double length_squared = static_cast<double>(intent.direction_x) * intent.direction_x
            + static_cast<double>(intent.direction_y) * intent.direction_y;
        if (std::abs(length_squared - 1.0) > 0.01)
            return Result::invalid;
        if (!host.active || host.paused || !host.can_act)
            return Result::inactive;
        if (host.mode != Mode::turn_based && host.mode != Mode::simultaneous)
            return Result::invalid;
        if (host.mode == Mode::turn_based && host.current_player != intent.player)
            return Result::wrong_turn;
        if (!std::isfinite(host.reload_remaining) || host.reload_remaining != 0)
            return Result::reloading;
        if (host.ammunition < -1 || host.ammunition == 0)
            return Result::empty_ammo;
        sequences_[intent.player] = intent.sequence;
        return Result::accepted;
    }

private:
    std::uint64_t session_;
    std::uint64_t peers_[2];
    std::uint64_t sequences_[2]{};
    bool valid_;
};
}
