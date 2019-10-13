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

    managed_byte_array_ptr::managed_byte_array_ptr(std::nullptr_t) noexcept :
        _block(nullptr)
    {
    }

    managed_byte_array_ptr::managed_byte_array_ptr(detail::array_pool_block* block) noexcept :
        _block(block)
    {
        assert(block != nullptr);
        assert(block->usage == 0);
        block->usage = 1;
    }

    managed_byte_array_ptr::managed_byte_array_ptr(const managed_byte_array_ptr& other) noexcept :
        _block(other._block)
    {
        if (_block != nullptr)
        {
            _block->usage++;
        }
    }

    managed_byte_array_ptr::managed_byte_array_ptr(managed_byte_array_ptr&& other) noexcept :
        _block(other._block)
    {
        other._block = nullptr;
    }

    managed_byte_array_ptr::~managed_byte_array_ptr()
    {
        if (_block != nullptr)
        {
            std::size_t count = _block->usage.fetch_sub(1);
            if (count == 1)
            {
                _block->owner->release(_block);
            }
        }
    }

    managed_byte_array_ptr::operator bool() const noexcept
    {
        return _block != nullptr;
    }

    managed_byte_array_ptr& managed_byte_array_ptr::operator=(const managed_byte_array_ptr& other) noexcept
    {
        managed_byte_array_ptr temp(other);
        swap(temp);
        return *this;
    }

    managed_byte_array_ptr& managed_byte_array_ptr::operator=(managed_byte_array_ptr&& other) noexcept
    {
        detail::array_pool_block* temp = other._block;
        other._block = nullptr;
        _block = temp;
        return *this;
    }

    auto managed_byte_array_ptr::operator*() const noexcept -> element_type&
    {
        assert(_block != nullptr);
        return _block->array;
    }

    auto managed_byte_array_ptr::operator->() const noexcept -> element_type*
    {
        assert(_block != nullptr);
        return &_block->array;
    }

    auto managed_byte_array_ptr::get() const noexcept ->element_type*
    {
        return (_block == nullptr) ? nullptr : &_block->array;
    }

    void managed_byte_array_ptr::swap(managed_byte_array_ptr& other) noexcept
    {
        using std::swap;
        swap(_block, other._block);
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

    bool operator==(const managed_byte_array_ptr& a, const managed_byte_array_ptr& b)
    {
        return a._block == b._block;
    }

    void swap(managed_byte_array_ptr& lhs, managed_byte_array_ptr& rhs) noexcept
    {
        lhs.swap(rhs);
    }
}
