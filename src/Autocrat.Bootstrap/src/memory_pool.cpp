#include "memory_pool.h"
#include "defines.h"
#include <algorithm>
#include <cassert>

namespace
{

autocrat::memory_pool_buffer::pool_type global_pool;

}

namespace autocrat
{

memory_pool_buffer::memory_pool_buffer() :
    _head(nullptr),
    _tail(nullptr),
    _count(0)
{
}

memory_pool_buffer::~memory_pool_buffer() noexcept
{
    node_type* node = _head;
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
        std::size_t count = std::min(remaining, node_type::capacity - used);
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

    node_type* node = _head;
    std::size_t remaining = _count;
    std::byte* dst = destination;
    while (node != nullptr)
    {
        std::size_t count = std::min(remaining, node_type::capacity);
        remaining -= count;

        dst = std::copy_n(node->buffer.data(), count, dst);
        node = release_node(node);
    }

    assert(node == nullptr);
    _head = nullptr;
    _count = 0;
}

void memory_pool_buffer::replace(
    std::size_t index,
    const value_type* data,
    std::size_t length)
{
    assert((index + length) <= _count);

    std::size_t node_index = index / node_type::capacity;
    std::size_t offset = index % node_type::capacity;

    node_type* node = _head;
    while (node_index-- > 0)
    {
        node = node->next;
    }

    const std::byte* src = data;
    std::size_t remaining = length;
    while (remaining > 0)
    {
        std::size_t count = std::min(remaining, node_type::capacity - offset);
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
    else if (_tail->data == (_tail->buffer.data() + node_type::capacity))
    {
        node_type* node = global_pool.acquire();
        _tail->next = node;
        _tail = node;
    }
}

auto memory_pool_buffer::release_node(node_type* node) -> node_type*
{
    node_type* next = node->next;
    global_pool.release(node);
    return next;
}

}
