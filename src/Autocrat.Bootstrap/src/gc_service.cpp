#include <cassert>
#include <cstddef>
#include <cstdlib>
#include <new>
#include "collections.h"
#include "defines.h"
#include "gc_service.h"
#include "memory_pool.h"

namespace
{
    using pool_type = autocrat::node_pool<1024u * 1024u>;

    struct large_allocation
    {
        alignas(std::max_align_t) large_allocation* previous;
    };

    struct memory_allocations
    {
        pool_type::node_type* head;
        pool_type::node_type* tail;
        large_allocation* large_objects;
    };

    pool_type global_pool;
    autocrat::dynamic_array<memory_allocations> thread_allocations;
    thread_local memory_allocations* current_allocations;

    std::size_t align_up(std::size_t value)
    {
        const std::size_t alignment = sizeof(std::max_align_t);
        std::size_t result = (value + (alignment - 1)) & ~(alignment - 1);
        assert(result >= value); // check for overflow
        return result;
    }

    void* allocate_large(large_allocation*& allocation, std::size_t size)
    {
        void* raw = std::calloc(sizeof(large_allocation) + size, sizeof(std::byte));
        if (raw == nullptr)
        {
            throw std::bad_alloc();
        }

        auto memory = static_cast<large_allocation*>(raw);
        if (allocation != nullptr)
        {
            allocation->previous = memory;
        }

        allocation = memory;
        return memory + 1;
    }

    std::byte* allocate_small(pool_type::node_type*& tail, std::size_t size)
    {
        size = align_up(size);
        std::size_t used = tail->data - tail->buffer.data();
        std::size_t available = tail->capacity - used;
        if (available < size)
        {
            pool_type::node_type* previous = tail;
            tail = global_pool.acquire();
            previous->next = tail;
        }

        std::byte* memory = tail->data;
        tail->data += size;
        return memory;
    }

    void free_large_allocations(memory_allocations& allocations)
    {
        large_allocation* current = allocations.large_objects;
        while (current != nullptr)
        {
            large_allocation* previous = current->previous;
            std::free(current);
            current = previous;
        }

        allocations.large_objects = nullptr;
    }

    void free_nodes(memory_allocations& allocations)
    {
        // The gc_service constructor always allocates at least one node, so
        // we know head won't be empty
        pool_type::node_type* node = allocations.head->next;
        while (node != nullptr)
        {
            pool_type::node_type* next = node->next;
            global_pool.release(node);
            node = next;
        }

        allocations.head->data = allocations.head->buffer.data(); 
        allocations.tail = allocations.head;
    }

    void free_allocations(memory_allocations& allocations)
    {
        free_large_allocations(allocations);
        free_nodes(allocations);
    }
}

namespace autocrat
{
    gc_service::gc_service(thread_pool* pool)
    {
        std::size_t pool_size = pool->size();
        thread_allocations = decltype(thread_allocations)(pool_size);

        // Preallocate some memory for all the threads
        for (std::size_t i = 0; i != pool_size; ++i)
        {
            memory_allocations& allocations = thread_allocations[i];
            allocations = {};
            allocations.head = global_pool.acquire();
            allocations.tail = allocations.head;
        }
    }

    gc_service::~gc_service() noexcept
    {
        for (auto& allocations : thread_allocations)
        {
            free_allocations(allocations);
            global_pool.release(allocations.head);
        }

        // Allow the destructor to run more than once (this happens during unit
        // testing)
        thread_allocations = {};
    }

    void* gc_service::allocate(std::size_t size)
    {
        if (size > 102'400u)
        {
            // This will zero out the memory it returns
            return allocate_large(current_allocations->large_objects, size);
        }
        else
        {
            std::byte* memory = allocate_small(current_allocations->tail, size);
            std::fill_n(memory, size, std::byte{});
            return memory;
        }
    }

    void gc_service::on_begin_work(std::size_t thread_id)
    {
        current_allocations = thread_allocations.data() + thread_id;
    }

    void gc_service::on_end_work(std::size_t thread_id)
    {
        assert(current_allocations == (thread_allocations.data() + thread_id));
        UNUSED(thread_id);

        free_allocations(*current_allocations);
    }
}
