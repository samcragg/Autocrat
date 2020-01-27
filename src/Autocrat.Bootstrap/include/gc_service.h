#ifndef GC_SERVICE_H
#define GC_SERVICE_H

#include <cstddef>
#include "defines.h"

namespace autocrat
{
    class thread_pool;

    /**
     * Manages the managed memory and performs automatic garbage collection
     * when the memory is no longer required.
     */
    class gc_service
    {
    public:
        MOCKABLE_CONSTRUCTOR_AND_DESTRUCTOR(gc_service)

        /**
         * Constructs a new instance of the `gc_service` class.
         * @param pool Used to dispatch work to.
         */
        explicit gc_service(thread_pool* pool);

        /**
         * Allocates dynamic memory of the specified size.
         * @param size The number of bytes to allocate.
         * @returns The allocated memory.
         */
        MOCKABLE_METHOD void* allocate(std::size_t size);
    };
}

#endif
