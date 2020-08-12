#ifndef APPLICATION_LIFETIME_H
#define APPLICATION_LIFETIME_H

#include "gc_service.h"
#include <atomic>

namespace autocrat
{

/**
 * Performs the main application logic.
 */
class application
{
public:
    /**
     * Initializes the services and loads the configuration data.
     */
    void initialize();

    /**
     * Runs the main program loop.
     * @remarks This blocks until a call to `stop` is made.
     */
    void run();

    /**
     * Stops the services.
     */
    void stop();

private:
    void initialize_managed_thread(gc_service* gc);
    void initialize_threads();

    gc_heap _global_heap;
    std::atomic_bool _running;
};

}

#endif
