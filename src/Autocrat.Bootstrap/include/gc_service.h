#ifndef GC_SERVICE_H
#define GC_SERVICE_H

#include <cstddef>
#include "defines.h"
#include "thread_pool.h"

namespace autocrat
{
    /**
     * Manages the managed memory and performs automatic garbage collection
     * when the memory is no longer required.
     */
    class gc_service : public lifetime_service
    {
    public:
        MOCKABLE_CONSTRUCTOR(gc_service)

        /**
         * Constructs a new instance of the `gc_service` class.
         * @param pool Used to dispatch work to.
         */
        explicit gc_service(thread_pool* pool);

        /**
         * Destroys the `gc_service` instance.
         */
        MOCKABLE_METHOD ~gc_service() noexcept;

        /**
         * Allocates dynamic memory of the specified size.
         * @param size The number of bytes to allocate.
         * @returns The allocated memory.
         */
        MOCKABLE_METHOD void* allocate(std::size_t size);

        void on_begin_work(std::size_t thread_id) override;
        void on_end_work(std::size_t thread_id) override;
    };
}

#endif
