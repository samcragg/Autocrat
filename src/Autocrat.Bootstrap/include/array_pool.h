#ifndef ARRAY_POOL_H
#define ARRAY_POOL_H

#include <array>
#include <atomic>
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
        const_iterator begin() const noexcept;

        /**
         * Returns the number of elements that the container has allocated
         * space for.
         * @returns Capacity of the currently allocated storage.
         */
        constexpr std::size_t capacity() const noexcept
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
        const_iterator end() const noexcept;

        /**
         * Resizes the container to contain count elements.
         * @param count The new size of the container.
         */
        void resize(std::size_t count);

        /**
         * Returns the number of elements in the container.
         * @returns The number of elements in the container.
         */
        std::size_t size() const noexcept;
    private:
        const void* _ee_type;
        std::uint64_t _length;
        storage_type _data;
    };

    class array_pool;

    namespace detail
    {
        struct array_pool_block
        {
            array_pool* owner;
            std::atomic_size_t usage;
            managed_byte_array array;
        };
    }

    /**
     * Manages the lifetime of a `managed_byte_array` that has been obtained
     * from `array_pool`.
     */
    class managed_byte_array_ptr
    {
    public:
        using element_type = managed_byte_array;

        managed_byte_array_ptr() noexcept = default;
        managed_byte_array_ptr(std::nullptr_t) noexcept;
        managed_byte_array_ptr(const managed_byte_array_ptr& other) noexcept;
        managed_byte_array_ptr(managed_byte_array_ptr&& other) noexcept;
        ~managed_byte_array_ptr();
        explicit operator bool() const noexcept;

        managed_byte_array_ptr& operator=(const managed_byte_array_ptr& other) noexcept;
        managed_byte_array_ptr& operator=(managed_byte_array_ptr&& other) noexcept;

        element_type& operator*() const noexcept;
        element_type* operator->() const noexcept;

        /**
         * Returns the stored pointer.
         * @returns The stored pointer.
         */
        element_type* get() const noexcept;

        /**
         * Exchanges the contents of this instance and `other`.
         * @param other The instance to exchange contents with.
         */
        void swap(managed_byte_array_ptr& other) noexcept;

        friend array_pool;
        friend bool operator==(const managed_byte_array_ptr&, const managed_byte_array_ptr&);
    private:
        managed_byte_array_ptr(detail::array_pool_block* block) noexcept;

        detail::array_pool_block* _block { nullptr };
    };

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
        std::size_t capacity() const noexcept;

        /**
         * Gets the number of arrays that have been allocated.
         * @returns The number of arrays in use.
         */
        std::size_t size() const noexcept;

        friend managed_byte_array_ptr;
    private:
        void release(element_type* value);

        std::stack<element_type*> _available;
        storage_type _pool;
    };

    /**
     * Determines whether two `managed_byte_array_ptr` instances are equal.
     * @param a The value to compare.
     * @param b The value to compare.
     * @returns `true` if the instances point to the same value; otherwise, `false`.
     */
    bool operator==(const managed_byte_array_ptr& a, const managed_byte_array_ptr& b);
    
    /**
     * Exchanges the given values.
     * @param lhs The value to swap.
     * @param rhs The value to swap.
     */
    void swap(managed_byte_array_ptr& lhs, managed_byte_array_ptr& rhs) noexcept;
}

#endif
