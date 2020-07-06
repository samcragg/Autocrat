#ifndef GC_SERVICE_H
#define GC_SERVICE_H

#include "defines.h"
#include "memory_pool.h"
#include "thread_pool.h"
#include <cstddef>

namespace autocrat
{

class gc_service;

/**
 * Represents an area of managed memory.
 */
class gc_heap
{
public:
    using pool_type = autocrat::node_pool<1024u * 1024u>;

    /**
     * Constructs a new instance of the `gc_heap` class.
     */
    gc_heap();

    /**
     * Destroys the `gc_heap` instance.
     */
    ~gc_heap();

    gc_heap(const gc_heap&) = delete;
    gc_heap& operator=(const gc_heap&) = delete;

    gc_heap(gc_heap&& other) noexcept;
    gc_heap& operator=(gc_heap&& other) noexcept;

    /**
     * Exchanges the contents of this instance and `other`.
     * @param other The instance to exchange contents with.
     */
    void swap(gc_heap& other) noexcept;

private:
    friend gc_service;

    struct large_allocation
    {
        alignas(std::max_align_t) large_allocation* previous;
    };

    void* allocate_large(std::size_t size);
    std::byte* allocate_small(std::size_t size);
    void free_large();
    void free_small();

    pool_type::node_type* _head;
    pool_type::node_type* _tail;
    large_allocation* _large_objects;
};

/**
 * Manages the managed memory and performs automatic garbage collection
 * when the memory is no longer required.
 */
class gc_service FINAL_CLASS : public thread_specific_storage<gc_heap>
{
public:
    using base_type = thread_specific_storage<gc_heap>;

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

    /**
     * Resets the current threads heap to an empty instance.
     * @returns The previous memory allocations.
     */
    MOCKABLE_METHOD gc_heap reset_heap();

    /**
     * Sets the current threads head to the specified memory.
     * @param heap Contains the memory allocations.
     */
    MOCKABLE_METHOD void set_heap(gc_heap&& heap);

protected:
    void on_begin_work(gc_heap* heap) override;
    void on_end_work(gc_heap* heap) override;
};

}

#endif
