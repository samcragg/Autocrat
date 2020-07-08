#include "array_pool.h"
#include "managed_exports.h"
#include <cassert>

namespace autocrat::detail
{

void intrusive_ptr_add_ref(array_pool_block* pointer) noexcept
{
    pointer->usage++;
}

void intrusive_ptr_release(array_pool_block* pointer) noexcept
{
    std::size_t count = pointer->usage.fetch_sub(1);
    if (count == 1)
    {
        pointer->owner->release(pointer);
    }
}

}

namespace autocrat
{

managed_byte_array::managed_byte_array() : _data()
{
    static const void* byte_array_type = managed_exports::GetByteArrayType();
    _ee_type = byte_array_type;
}

auto managed_byte_array::begin() const noexcept -> const_iterator
{
    return _data.begin();
}

auto managed_byte_array::begin() noexcept -> iterator
{
    return _data.begin();
}

void managed_byte_array::clear() noexcept
{
    std::fill_n(_data.begin(), _length, value_type{});
    _length = 0;
}

auto managed_byte_array::data() const noexcept -> const value_type*
{
    return _data.data();
}

auto managed_byte_array::data() noexcept -> value_type*
{
    return _data.data();
}

auto managed_byte_array::end() const noexcept -> const_iterator
{
    return _data.end();
}

auto managed_byte_array::end() noexcept -> iterator
{
    return _data.end();
}

void managed_byte_array::resize(std::size_t count)
{
    assert(count <= buffer_size);

    if (count < _length)
    {
        std::fill_n(_data.begin() + count, _length - count, value_type{});
    }

    _length = count;
}

std::size_t managed_byte_array::size() const noexcept
{
    return _length;
}

managed_byte_array_ptr array_pool::aquire()
{
    // TODO: Thread safety?
    element_type* block;
    if (_available.empty())
    {
        block = &_pool.emplace_back();
        block->owner = this;
    }
    else
    {
        block = _available.top();
        _available.pop();
    }

    return managed_byte_array_ptr(block);
}

std::size_t array_pool::capacity() const noexcept
{
    return _pool.size();
}

std::size_t array_pool::size() const noexcept
{
    return _pool.size() - _available.size();
}

void array_pool::release(element_type* value)
{
    assert(value != nullptr);
    assert(value->owner == this);

    // TODO: Thread safety?
    _available.push(value);
}

}
