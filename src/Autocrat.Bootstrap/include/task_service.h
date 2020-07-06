#ifndef TASK_SERVICE_H
#define TASK_SERVICE_H

#include "defines.h"
#include "managed_types.h"

namespace autocrat
{

class thread_pool;

/**
 * Allows the enqueue of managed work onto the thread pool.
 */
class task_service
{
public:
    MOCKABLE_CONSTRUCTOR_AND_DESTRUCTOR(task_service)

    /**
     * Constructs a new instance of the `task_service` class.
     * @param pool Used to dispatch work to.
     */
    explicit task_service(thread_pool* pool);

    /**
     * Queues the specified work on the thread pool.
     * @param callback The delegate to invoke.
     * @param state    The object to pass to the delegate.
     */
    MOCKABLE_METHOD void enqueue(managed_delegate* callback, void* state);

    /**
     * Queues the specified work on the thread pool.
     * @param action The delegate to invoke.
     */
    MOCKABLE_METHOD void start_new(managed_delegate* action);

private:
    thread_pool* _thread_pool;
};

}

#endif
