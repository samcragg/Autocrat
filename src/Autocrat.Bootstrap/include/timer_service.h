#ifndef TIMER_SERVICE_H
#define TIMER_SERVICE_H

#include "defines.h"
#include "exports.h"
#include "smart_ptr.h"
#include <chrono>
#include <vector>

namespace autocrat
{

class thread_pool;

struct timer_info : intrusive_ref_counter<timer_info>
{
    timer_method callback = nullptr;
    std::chrono::microseconds interval = {};
    std::uint32_t handle = 0;
};

using timer_info_ptr = intrusive_ptr<timer_info>;

/**
 * Exposes functionality for queuing of work at specific time points.
 */
class timer_service
{
public:
    MOCKABLE_CONSTRUCTOR_AND_DESTRUCTOR(timer_service)

    using duration = std::chrono::microseconds;

    /**
     * Constructs a new instance of the `timer_service` class.
     * @param pool Used to dispatch work to.
     */
    explicit timer_service(thread_pool* pool);

    /**
     * Associates the specified time point with the handler.
     * @param delay    The initial delay before invoking the callback.
     * @param interval The time interval between invocations of `callback`.
     * @param callback The method to invoke when the time is due.
     * @returns The unique handle that will be passed to each invocation of
     *          the callback.
     */
    MOCKABLE_METHOD std::uint32_t add_timer_callback(
        duration delay,
        duration interval,
        timer_method callback);

    /**
     * Checks for network messages and dispatches any that have arrived.
     */
    MOCKABLE_METHOD void check_and_dispatch();

private:
    struct time_slot
    {
        duration due;
        timer_info_ptr info;
    };

    friend bool operator<(const time_slot& a, const time_slot& b);

    void enqueue_callbacks(std::vector<time_slot>::iterator begin);

    std::vector<time_slot> _slots;
    thread_pool* _thread_pool;
};

}

#endif
