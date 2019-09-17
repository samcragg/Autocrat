#include "array_pool.h"
#include "managed_exports.h"
#include <cassert>

namespace autocrat
{
    managed_byte_array::managed_byte_array() :
        _length(0),
        _data()
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

    constexpr std::size_t managed_byte_array::capacity() const noexcept
    {
        return buffer_size;
    }

    void managed_byte_array::clear() noexcept
    {
        std::fill_n(_data.begin(), _length, value_type{});
        _length = 0;
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

    auto array_pool::aquire() -> value_type&
    {
        // TODO: Thread safety?
        value_type* result;
        if (_available.empty())
        {
            result = &_pool.emplace_back();
        }
        else
        {
            result = _available.top();
            _available.pop();
        }

        return *result;
    }

    void array_pool::release(value_type& value)
    {
        // TODO: Thread safety?
        _available.push(&value);
    }
}
