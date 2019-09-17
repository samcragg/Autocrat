#ifndef ARRAY_POOL_H
#define ARRAY_POOL_H

#include <array>
#include <cstddef>
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
         */
        const_iterator begin() const noexcept;

        /**
         * Returns an iterator to the first element of the container.
         */
        iterator begin() noexcept;

        /**
         * Returns the number of elements that the container has allocated space for.
         */
        constexpr std::size_t capacity() const noexcept;

        /**
         * Erases all elements from the container.
         */
        void clear() noexcept;

        /**
         * Returns pointer to the underlying array serving as element storage.
         */
        value_type* data() noexcept;

        /**
         * Returns an iterator to the element following the last element of the container. 
         */
        const_iterator end() const noexcept;

        /**
         * Returns an iterator to the element following the last element of the container.
         */
        iterator end() noexcept;

        /**
         * Resizes the container to contain count elements.
         * @param count The new size of the container.
         */
        void resize(std::size_t count);

        /**
         * Returns the number of elements in the container.
         */
        std::size_t size() const noexcept;
    private:
        const void* _ee_type;
        std::uint64_t _length;
        storage_type _data;
    };

    class array_pool
    {
    public:
        using value_type = managed_byte_array;
        using storage_type = std::deque<value_type>;

        /**
         * Gets a byte array from the pool.
         */
        value_type& aquire();

        /**
         * Returns the byte array to the pool.
         */
        void release(value_type& value);
    private:
        std::stack<value_type*> _available;
        storage_type _pool;
    };
}

#endif
