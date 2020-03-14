#include <cstddef>
#include <cstdlib>
#include <new>
#include "defines.h"
#include "gc_service.h"

namespace
{
    using namespace autocrat::detail;

    pool_type global_pool;

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
    namespace detail
    {
        memory_allocations::memory_allocations() :
            large_objects(nullptr)
        {
            // Pre-allocate some memory
            head = global_pool.acquire();
            tail = head;
        }

        memory_allocations::~memory_allocations()
        {
            // Allow the destructor to run more than once (this happens during
            // unit testing)
            if (head != nullptr)
            {
                free_allocations(*this);
                global_pool.release(head);
                head = nullptr;
            }
        }
    }

    gc_service::gc_service(thread_pool* pool) :
        base_type(pool)
    {
    }

    void* gc_service::allocate(std::size_t size)
    {
        if (size > 102'400u)
        {
            // This will zero out the memory it returns
            return allocate_large(thread_storage->large_objects, size);
        }
        else
        {
            std::byte* memory = allocate_small(thread_storage->tail, size);
            std::fill_n(memory, size, std::byte{}); // TODO: Zero the memory in bulk when it is freed
            return memory;
        }
    }

    void gc_service::on_end_work(std::size_t thread_id)
    {
        free_allocations(*thread_storage);
        base_type::on_end_work(thread_id);
    }
}
