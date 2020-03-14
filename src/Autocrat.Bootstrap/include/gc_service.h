#ifndef GC_SERVICE_H
#define GC_SERVICE_H

#include <cstddef>
#include "defines.h"
#include "memory_pool.h"
#include "thread_pool.h"

namespace autocrat
{
    namespace detail
    {
        using pool_type = autocrat::node_pool<1024u * 1024u>;

        struct large_allocation
        {
            alignas(std::max_align_t) large_allocation* previous;
        };

        struct memory_allocations
        {
            memory_allocations();
            ~memory_allocations();

            pool_type::node_type* head;
            pool_type::node_type* tail;
            large_allocation* large_objects;
        };
    }

    /**
     * Manages the managed memory and performs automatic garbage collection
     * when the memory is no longer required.
     */
    class gc_service : public thread_specific_storage<detail::memory_allocations>
    {
    public:
        using base_type = thread_specific_storage<detail::memory_allocations>;

        MOCKABLE_CONSTRUCTOR(gc_service)

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

        void on_end_work(std::size_t thread_id) override;
    };
}

#endif
