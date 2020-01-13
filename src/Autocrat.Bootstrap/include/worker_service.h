#ifndef WORKER_SERVICE_H
#define WORKER_SERVICE_H

#include <cstdint>
#include <unordered_map>
#include "defines.h"
#include "method_handles.h"

namespace autocrat
{
    class thread_pool;

    /**
     * Exposes functionality for obtaining worker services.
     */
    class worker_service
    {
    public:
        MOCKABLE_CONSTRUCTOR_AND_DESTRUCTOR(worker_service)

        /**
         * Constructs a new instance of the `worker_service` class.
         * @param pool Used to dispatch work to.
         */
        explicit worker_service(thread_pool* pool);

        /**
         * Gets a worker of the specified type.
         * @param type The type of the worker to return.
         * @returns A managed object of the specified type.
         */
        MOCKABLE_METHOD void* get_worker(const void* type);

        /**
         * Registers the specified constructor.
         * @param type        The type of the worker returned by the constructor.
         * @param constructor The method to construct a new managed instance.
         */
        MOCKABLE_METHOD void register_type(const void* type, construct_worker constructor);
    private:
        void* make_worker(std::uintptr_t type);

        std::unordered_map<std::uintptr_t, construct_worker> _constructors;
        std::unordered_map<std::uintptr_t, void*> _workers;
    };
}

#endif
