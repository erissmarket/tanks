#include "rate_limit.h"
#include <math.h>
#include <stddef.h>

static int valid_time(double value)
{
    return isfinite(value) && value >= 0.0;
}

int tank_rate_init(tank_rate_limit *limit, double capacity, double per_second, double now)
{
    if (limit == NULL || !isfinite(capacity) || capacity < 1.0 || capacity > 10000.0
        || !isfinite(per_second) || per_second <= 0.0 || per_second > 10000.0 || !valid_time(now))
        return 0;
    limit->tokens = capacity;
    limit->last_time = now;
    limit->capacity = capacity;
    limit->per_second = per_second;
    return 1;
}

int tank_rate_allow(tank_rate_limit *limit, double now)
{
    double refill;
    double missing;
    if (limit == NULL || !valid_time(now) || !valid_time(limit->last_time)
        || now < limit->last_time || !isfinite(limit->capacity)
        || limit->capacity < 1.0 || limit->capacity > 10000.0
        || !isfinite(limit->per_second) || limit->per_second <= 0.0 || limit->per_second > 10000.0
        || !isfinite(limit->tokens) || limit->tokens < 0.0 || limit->tokens > limit->capacity)
        return 0;
    missing = limit->capacity - limit->tokens;
    /* Bound refill before multiplication, including very long idle intervals. */
    refill = now - limit->last_time;
    if (refill >= missing / limit->per_second)
        limit->tokens = limit->capacity;
    else
        limit->tokens += refill * limit->per_second;
    limit->last_time = now;
    if (limit->tokens < 1.0)
        return 0;
    limit->tokens -= 1.0;
    return 1;
}
