#ifndef MEMORY_POOL_H
#define MEMORY_POOL_H

#include <algorithm>
#include <array>
#include <atomic>
#include <cassert>
#include <cstddef>

namespace autocrat
{

/**
 * Represents a small chunk of memory.
 * @tparam Size The number of bytes to store in the pool.
 */
template <std::size_t Size>
struct pool_node
{
    using this_type = pool_node<Size>;

    constexpr static std::size_t capacity = Size;

    /**
     * Initializes a new instance of the `pool_node` class.
     */
    pool_node() : buffer{}
    {
        data = buffer.data();
    }

    /**
     * Zero-fills the buffer and resets data to the beginning.
     */
    void clear_data()
    {
        std::fill(buffer.data(), data, std::byte{});
        data = buffer.data();
    }

    this_type* allocated_list;
    this_type* next;
    std::byte* data;
    std::array<std::byte, Size> buffer;
#ifndef NDEBUG
    bool is_free;
#endif
};

/**
 * Represents a pool of memory nodes.
 * @tparam NodeSize The size, in bytes, of the nodes for the pool.
 * @remarks This class is designed to be thread-safe. All nodes returned
 *          will have their buffer zero-filled.
 */
template <std::size_t NodeSize>
class node_pool
{
public:
    using node_type = pool_node<NodeSize>;

    /**
     * Initializes a new instance of the `node_pool` class.
     */
    node_pool() : _free_list(nullptr), _root(nullptr)
    {
    }

    /**
     * Destroys the `node_pool` instance.
     */
    ~node_pool() noexcept
    {
        node_type* node = _root.load();
        while (node != nullptr)
        {
            node_type* next = node->allocated_list;
            assert(next != node);
            delete node;
            node = next;
        }
    }

    /**
     * Gets a node from this instance, allocating a new one is non are
     * available.
     * @returns A node from the pool if available; otherwise, a new node.
     */
    node_type* acquire()
    {
        node_type* node = get_from_free_list();
        if (node == nullptr)
        {
            node = allocate_new();
        }

#ifndef NDEBUG
        node->is_free = false;
#endif

        node->next = nullptr;
        return node;
    }

    /**
     * Adds the specified node to this instance, allowing it to be reused.
     * @param node The node to return to the pool.
     */
    void release(node_type* node)
    {
#ifndef NDEBUG
        assert(!node->is_free);
        node->is_free = true;
#endif
        // We optimize for allocations by clearing the memory now, as this
        // code is executed after the user code, so we're not time critical
        node->clear_data();

        node_type* free = _free_list.load();
        do
        {
            node->next = free;
        } while (!_free_list.compare_exchange_weak(free, node));
    }

private:
    node_type* allocate_new()
    {
        node_type* node = new node_type();
        node_type* root = _root.load();
        do
        {
            node->allocated_list = root;
        } while (!_root.compare_exchange_weak(root, node));

        return node;
    }

    node_type* get_from_free_list()
    {
        node_type* free = _free_list.load();
        node_type* next;
        do
        {
            if (free == nullptr)
            {
                return nullptr;
            }

            next = free->next;
        } while (!_free_list.compare_exchange_weak(free, next));

        return free;
    }

    std::atomic<node_type*> _free_list;
    std::atomic<node_type*> _root;
};

/**
 * Allows the reading and writing of pieces of data.
 * @remarks This class is designed to be used by a single thread.
 */
class memory_pool_buffer
{
public:
    using pool_type = node_pool<1024u>;
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
    using node_type = pool_type::node_type;

    void ensure_space_to_write();
    node_type* release_node(node_type* node);

    node_type* _head;
    node_type* _tail;
    std::size_t _count;
};

}

#endif
