#include <cstddef>
#include <cstdlib>
#include <new>
#include "defines.h"
#include "gc_service.h"

namespace
{
    using pool_type = autocrat::gc_heap::pool_type;
    autocrat::gc_heap::pool_type global_pool;

    std::size_t align_up(std::size_t value)
    {
        const std::size_t alignment = sizeof(std::max_align_t);
        std::size_t result = (value + (alignment - 1)) & ~(alignment - 1);
        assert(result >= value); // check for overflow
        return result;
    }
}

namespace autocrat
{
    gc_heap::gc_heap() :
        _large_objects(nullptr)
    {
        // Pre-allocate some memory
        _head = global_pool.acquire();
        _tail = _head;
    }

    gc_heap::~gc_heap()
    {
        // Allow the destructor to run more than once (this happens during
        // unit testing)
        if (_head != nullptr)
        {
            free_large();
            free_small();
            global_pool.release(_head);
            _head = nullptr;
        }
    }

    void* gc_heap::allocate_large(std::size_t size)
    {
        // We must return zero-filled memory, hence calloc
        void* raw = std::calloc(sizeof(large_allocation) + size, sizeof(std::byte));
        if (raw == nullptr)
        {
            throw std::bad_alloc();
        }

        auto memory = static_cast<large_allocation*>(raw);
        if (_large_objects != nullptr)
        {
            _large_objects->previous = memory;
        }

        _large_objects = memory;
        return memory + 1; // The actual memory is after the large_allocation, hence +1
    }

    std::byte* gc_heap::allocate_small(std::size_t size)
    {
        size = align_up(size);
        assert(size < pool_type::node_type::capacity);

        std::size_t used = _tail->data - _tail->buffer.data();
        std::size_t available = _tail->capacity - used;
        if (available < size)
        {
            pool_type::node_type* previous = _tail;
            _tail = global_pool.acquire();
            previous->next = _tail;
        }

        std::byte* memory = _tail->data;
        _tail->data += size;
        return memory;
    }

    void gc_heap::free_large()
    {
        large_allocation* current = _large_objects;
        while (current != nullptr)
        {
            large_allocation* previous = current->previous;
            std::free(current);
            current = previous;
        }

        _large_objects = nullptr;
    }

    void gc_heap::free_small()
    {
        // We always allocates at least one node, so we know head won't be empty
        pool_type::node_type* node = _head->next;
        while (node != nullptr)
        {
            pool_type::node_type* next = node->next;
            global_pool.release(node);
            node = next;
        }

        _head->clear_data();
        _tail = _head;
    }

    gc_service::gc_service(thread_pool* pool) :
        base_type(pool)
    {
    }

    void* gc_service::allocate(std::size_t size)
    {
        if (size > 102'400u)
        {
            return thread_storage->allocate_large(size);
        }
        else
        {
            return thread_storage->allocate_small(size);
        }
    }

    void gc_service::on_end_work(std::size_t thread_id)
    {
        thread_storage->free_large();
        thread_storage->free_small();
        base_type::on_end_work(thread_id);
    }
}
