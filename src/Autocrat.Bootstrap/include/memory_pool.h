#ifndef MEMORY_POOL_H
#define MEMORY_POOL_H

#include <array>
#include <atomic>
#include <cstddef>

namespace autocrat
{
    /**
     * Represents a small chunk of memory.
     */
    struct pool_node
    {
        constexpr static std::size_t capacity = 1024;

        pool_node* allocated_list;
        pool_node* next;
        std::array<std::byte, capacity> buffer;
        std::byte* data;
    };

    /**
     * Represents a pool of memory nodes.
     * @remarks This class is designed to be thread-safe.
     */
    class node_pool
    {
    public:
        /**
         * Initializes a new instance of the `node_pool` class.
         */
        node_pool();

        /**
         * Destroys the `node_pool` instance.
         */
        ~node_pool() noexcept;

        /**
         * Gets a node from this instance, allocating a new one is non are
         * available.
         * @returns A node from the pool if available; otherwise, a new node.
         */
        pool_node* acquire();

        /**
         * Adds the specified node to this instance, allowing it to be reused.
         * @param node The node to return to the pool.
         */
        void release(pool_node* node);
    private:
        pool_node* allocate_new();
        pool_node* get_from_free_list();

        std::atomic<pool_node*> _free_list;
        std::atomic<pool_node*> _root;
    };

    /**
     * Allows the reading and writing of pieces of data.
     * @remarks This class is designed to be used by a single thread.
     */
    class memory_pool_buffer
    {
    public:
        using value_type = std::byte;

        /**
         * Initializes a new instance of the `memory_pool_buffer` class.
         */
        memory_pool_buffer();

        /**
         * Destroys the `memory_pool_buffer` instance.
         */
        ~memory_pool_buffer() noexcept;

        /**
         * Writes the specified data at the end of this instance.
         * @param data   The data to copy.
         * @param length The number of bytes to copy.
         */
        void append(const value_type* data, std::size_t length);

        /**
         * Moves the stored data within this instance to the specified buffer.
         * @param destination The destination buffer to receive the data.
         * @param size        The size, in bytes, of the destination buffer.
         * @remarks After calling this method, this instance will be empty.
         */
        void move_to(value_type* destination, std::size_t size);

        /**
         * Overwrites the specified data starting at the specified index.
         * @param index  The location of where to start writing.
         * @param data   The data to copy.
         * @param length The number of bytes to copy.
         */
        void replace(std::size_t index, const value_type* data, std::size_t length);

        /**
         * Gets the number of bytes written to this instance.
         * @returns The number of bytes that have been written.
         */
        std::size_t size() const noexcept;
    private:
        void ensure_space_to_write();
        pool_node* release_node(pool_node* node);

        pool_node* _head;
        pool_node* _tail;
        std::size_t _count;
    };
}

#endif
