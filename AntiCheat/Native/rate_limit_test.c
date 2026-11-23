#include "rate_limit.h"
#include <math.h>
#include <stddef.h>

int main(void)
{
    tank_rate_limit bucket = {0};
    if (tank_rate_init(NULL, 2.0, 2.0, 0.0)) return 1;
    if (tank_rate_init(&bucket, 0.0, 2.0, 0.0)) return 2;
    if (!tank_rate_init(&bucket, 2.0, 2.0, 0.0)) return 3;
    if (!tank_rate_allow(&bucket, 0.0)) return 4;
    if (!tank_rate_allow(&bucket, 0.0)) return 5;
    if (tank_rate_allow(&bucket, 0.0)) return 6;
    if (tank_rate_allow(&bucket, 0.25)) return 7;
    if (!tank_rate_allow(&bucket, 0.5)) return 8;
    if (tank_rate_allow(&bucket, 0.4)) return 9;
    if (tank_rate_allow(&bucket, NAN)) return 10;
    if (!tank_rate_allow(&bucket, 1000000.0)) return 11;
    if (!tank_rate_allow(&bucket, 1000000.0)) return 12;
    if (tank_rate_allow(&bucket, 1000000.0)) return 13;
    bucket.tokens = INFINITY;
    if (tank_rate_allow(&bucket, 1000001.0)) return 14;
    return 0;
}
