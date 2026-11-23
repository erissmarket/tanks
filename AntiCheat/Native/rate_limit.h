#ifndef TANK_RATE_LIMIT_H
#define TANK_RATE_LIMIT_H

#ifdef __cplusplus
extern "C" {
#endif

/* Authority-owned state. One bucket per peer, used on one serialized thread. */
typedef struct tank_rate_limit {
    double tokens;
    double last_time;
    double capacity;
    double per_second;
} tank_rate_limit;

/* now must come from an authoritative monotonic clock. Zero means rejected. */
int tank_rate_init(tank_rate_limit *limit, double capacity, double per_second, double now);
int tank_rate_allow(tank_rate_limit *limit, double now);

#ifdef __cplusplus
}
#endif
#endif
