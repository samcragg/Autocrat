#include <algorithm>
#include <cassert>
#include "defines.h"
#include "memory_pool.h"

namespace
{
    autocrat::node_pool global_pool;
}

namespace autocrat
{
    node_pool::node_pool() :
        _free_list(nullptr),
        _root(nullptr)
    {
    }

    node_pool::~node_pool() noexcept
    {
        pool_node* node = _root.load();
        while (node != nullptr)
        {
            pool_node* next = node->allocated_list;
            delete node;
            node = next;
        }
    }

    pool_node* node_pool::acquire()
    {
        pool_node* node = get_from_free_list();
        if (node == nullptr)
        {
            node = allocate_new();
        }

        node->data = node->buffer.data();
        return node;
    }

    void node_pool::release(pool_node* node)
    {
        pool_node* free = _free_list.load();
        do
        {
            node->next = free;
        } while (!_free_list.compare_exchange_weak(free, node));
    }

    pool_node* node_pool::allocate_new()
    {
        pool_node* node = new pool_node { };
        pool_node* root = _root.load();
        do
        {
            node->allocated_list = root;
        } while (!_root.compare_exchange_weak(root, node));

        return node;
    }

    pool_node* node_pool::get_from_free_list()
    {
        pool_node* free = _free_list.load();
        pool_node* next;
        do
        {
            if (free == nullptr)
            {
                return nullptr;
            }

            next = free->next;
        } while (!_free_list.compare_exchange_weak(free, next));

        free->next = nullptr;
        return free;
    }

    memory_pool_buffer::memory_pool_buffer() :
        _head(nullptr),
        _tail(nullptr),
        _count(0)
    {
    }

    memory_pool_buffer::~memory_pool_buffer() noexcept
    {
        pool_node* node = _head;
        while (node != nullptr)
        {
            node = release_node(node);
        }
    }

    void memory_pool_buffer::append(const value_type* data, std::size_t length)
    {
        const std::byte* src = data;
        std::size_t remaining = length;
        while (remaining > 0)
        {
            ensure_space_to_write();
            std::size_t used = _tail->data - _tail->buffer.data();
            std::size_t count = std::min(remaining, pool_node::capacity - used);
            _tail->data = std::copy_n(src, count, _tail->data);

            src += count;
            _count += count;
            remaining -= count;
        }
    }

    void memory_pool_buffer::move_to(value_type* destination, std::size_t size)
    {
        assert(size >= _count);
        UNUSED(size);

        pool_node* node = _head;
        std::size_t remaining = _count;
        std::byte* dst = destination;
        while (node != nullptr)
        {
            std::size_t count = std::min(remaining, pool_node::capacity);
            remaining -= count;

            dst = std::copy_n(node->buffer.data(), count, dst);
            node = release_node(node);
        }

        assert(node == nullptr);
        _head = nullptr;
        _count = 0;
    }

    void memory_pool_buffer::replace(std::size_t index, const value_type* data, std::size_t length)
    {
        assert((index + length) <= _count);

        std::size_t node_index = index / pool_node::capacity;
        std::size_t offset = index % pool_node::capacity;

        pool_node* node = _head;
        while (node_index-- > 0)
        {
            node = node->next;
        }

        const std::byte* src = data;
        std::size_t remaining = length;
        while (remaining > 0)
        {
            std::size_t count = std::min(remaining, pool_node::capacity - offset);
            std::copy_n(src, count, node->buffer.data() + offset);
            offset = 0;

            node = node->next;
            src += count;
            remaining -= count;
        }
    }

    std::size_t memory_pool_buffer::size() const noexcept
    {
        return _count;
    }

    void memory_pool_buffer::ensure_space_to_write()
    {
        if (_head == nullptr)
        {
            _head = global_pool.acquire();
            _tail = _head;
        }
        else if (_tail->data == (_tail->buffer.data() + pool_node::capacity))
        {
            pool_node* node = global_pool.acquire();
            _tail->next = node;
            _tail = node;
        }
    }

    pool_node* memory_pool_buffer::release_node(pool_node* node)
    {
        pool_node* next = node->next;
        global_pool.release(node);
        return next;
    }
}
