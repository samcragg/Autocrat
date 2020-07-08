#ifndef ARRAY_POOL_H
#define ARRAY_POOL_H

#include "smart_ptr.h"
#include <array>
#include <cstdint>
#include <deque>
#include <stack>

namespace autocrat
{

/**
 * Represents a byte array that can be used from managed code.
 */
class managed_byte_array
{
public:
    static constexpr std::size_t buffer_size = 4096;

    using value_type = std::uint8_t;
    using storage_type = std::array<value_type, buffer_size>;
    using const_iterator = storage_type::const_iterator;
    using iterator = storage_type::iterator;

    managed_byte_array();
    ~managed_byte_array() noexcept = default;

    managed_byte_array(const managed_byte_array&) = delete;
    managed_byte_array& operator=(const managed_byte_array&) = delete;

    managed_byte_array(managed_byte_array&&) noexcept = default;
    managed_byte_array& operator=(managed_byte_array&&) noexcept = default;

    /**
     * Returns an iterator to the first element of the container.
     * @returns An iterator to the first element.
     */
    iterator begin() noexcept;

    /**
     * Returns an iterator to the first element of the container.
     * @returns An iterator to the first element.
     */
    [[nodiscard]] const_iterator begin() const noexcept;

    /**
     * Returns the number of elements that the container has allocated
     * space for.
     * @returns Capacity of the currently allocated storage.
     */
    [[nodiscard]] constexpr std::size_t capacity() const noexcept
    {
        return buffer_size;
    }

    /**
     * Erases all elements from the container.
     */
    void clear() noexcept;

    /**
     * Returns pointer to the underlying array serving as element storage.
     */
    value_type* data() noexcept;

    /**
     * Returns pointer to the underlying array serving as element storage.
     */
    [[nodiscard]] const value_type* data() const noexcept;

    /**
     * Returns an iterator to the element following the last element of the
     * container.
     * @returns An iterator to the element following the last element.
     */
    iterator end() noexcept;

    /**
     * Returns an iterator to the element following the last element of the
     * container.
     * @returns An iterator to the element following the last element.
     */
    [[nodiscard]] const_iterator end() const noexcept;

    /**
     * Resizes the container to contain count elements.
     * @param count The new size of the container.
     */
    void resize(std::size_t count);

    /**
     * Returns the number of elements in the container.
     * @returns The number of elements in the container.
     */
    [[nodiscard]] std::size_t size() const noexcept;

private:
    const void* _ee_type;
    std::uint64_t _length = 0;
    storage_type _data;
};

class array_pool;

namespace detail
{

struct array_pool_block
{
    array_pool* owner = nullptr;
    std::atomic_size_t usage;
    managed_byte_array array;
};

void intrusive_ptr_add_ref(array_pool_block* pointer) noexcept;

void intrusive_ptr_release(array_pool_block* pointer) noexcept;

}

using managed_byte_array_ptr = intrusive_ptr<detail::array_pool_block>;

/**
 * Represents a pool of byte array resources.
 */
class array_pool
{
public:
    using element_type = detail::array_pool_block;
    using storage_type = std::deque<element_type>;

    /**
     * Gets a byte array from the pool.
     * @returns A pointer to a `mnaged_byte_array`.
     */
    managed_byte_array_ptr aquire();

    /**
     * Gets the number of arrays that the pool has currently allocated
     * space for.
     * @returns The number of arrays that have allocated space.
     */
    [[nodiscard]] std::size_t capacity() const noexcept;

    /**
     * Gets the number of arrays that have been allocated.
     * @returns The number of arrays in use.
     */
    [[nodiscard]] std::size_t size() const noexcept;

private:
    friend void detail::intrusive_ptr_release(element_type* pointer) noexcept;

    void release(element_type* value);

    std::stack<element_type*> _available;
    storage_type _pool;
};

}

#endif
