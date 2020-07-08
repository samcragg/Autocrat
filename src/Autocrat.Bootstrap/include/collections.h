#ifndef COLLECTIONS_H
#define COLLECTIONS_H

#include <algorithm>
#include <array>
#include <atomic>
#include <cassert>
#include <cstddef>
#include <functional>
#include <memory>
#include <new>
#include <type_traits>
#include <utility>

#ifdef _MSC_VER
// structure was padded due to alignment specifier
#pragma warning(push)
#pragma warning(disable : 4324)
#endif

namespace autocrat
{

/**
 * Represents a fixed sized queue that is designed for multiple consumers
 * and multiple producers.
 * @remarks Based on the work of Dmitry Vyukov:
 *          http://www.1024cores.net/home/lock-free-algorithms/queues/bounded-mpmc-queue
 */
template <typename T, std::size_t Sz>
class bounded_queue
{
public:
    bounded_queue()
    {
        for (std::size_t i = 0; i != Sz; ++i)
        {
            _buffer[i].sequence.store(i, std::memory_order_relaxed);
        }

        _enqueue_position.store(0, std::memory_order_relaxed);
        _dequeue_position.store(0, std::memory_order_relaxed);
    }

    /**
     * Appends a new element to the end of the queue.
     * @tparam Args The argument types.
     * @param args The arguments to forward to the constructor of the element.
     */
    template <class... Args>
    void emplace(Args&&... args)
    {
        cell_t* cell;
        std::size_t position =
            _enqueue_position.load(std::memory_order_relaxed);

        for (;;)
        {
            cell = _buffer.data() + (position % Sz);
            std::size_t sequence =
                cell->sequence.load(std::memory_order_acquire);

            std::intptr_t delta = static_cast<std::intptr_t>(sequence) -
                                  static_cast<std::intptr_t>(position);
            if (delta == 0)
            {
                if (_enqueue_position.compare_exchange_weak(
                        position, position + 1, std::memory_order_relaxed))
                {
                    break;
                }
            }
            else if (delta < 0)
            {
                throw std::bad_alloc();
            }
            else
            {
                position = _enqueue_position.load(std::memory_order_relaxed);
            }
        }

        ::new (&cell->storage) T(std::forward<Args>(args)...);
        cell->sequence.store(position + 1, std::memory_order_release);
    }

    /**
     * Removes the element from the beginning of the queue.
     * @param value Stores the element removed from the queue, if any.
     * @returns `true` if an element was removed from the queue; otherwise,
     *          `false`.
     */
    bool pop(T* value)
    {
        cell_t* cell;
        std::size_t position =
            _dequeue_position.load(std::memory_order_relaxed);
        for (;;)
        {
            cell = _buffer.data() + (position % Sz);
            std::size_t sequence =
                cell->sequence.load(std::memory_order_acquire);
            std::intptr_t delta = static_cast<std::intptr_t>(sequence) -
                                  static_cast<std::intptr_t>(position + 1);
            if (delta == 0)
            {
                if (_dequeue_position.compare_exchange_weak(
                        position, position + 1, std::memory_order_relaxed))
                {
                    break;
                }
            }
            else if (delta < 0)
            {
                return false;
            }
            else
            {
                position = _dequeue_position.load(std::memory_order_relaxed);
            }
        }

        T* item = std::launder(reinterpret_cast<T*>(&cell->storage));
        *value = std::move(*item);
        item->~T();
        cell->sequence.store(position + Sz + 1, std::memory_order_release);
        return true;
    }

private:
    struct cell_t
    {
        std::atomic_size_t sequence;
        typename std::aligned_storage<sizeof(T), alignof(T)>::type storage = {};
    };

    static constexpr std::size_t hardware_destructive_interference_size = 64;

    std::array<cell_t, Sz> _buffer;
    alignas(hardware_destructive_interference_size) std::atomic_size_t
        _enqueue_position;
    alignas(hardware_destructive_interference_size) std::atomic_size_t
        _dequeue_position;
};

/**
 * Represents a dynamically allocated array of elements.
 */
template <class T>
class dynamic_array
{
public:
    using const_iterator = const T*;
    using const_pointer = const T*;
    using const_reference = const T&;
    using iterator = T*;
    using pointer = T*;
    using reference = T&;
    using value_type = T;

    /**
     * Constructs a new instance of the `dynamic_array<T>` class.
     */
    dynamic_array() = default;

    /**
     * Constructs a new instance of the `dynamic_array<T>` class.
     * @param size The capacity for the array.
     */
    explicit dynamic_array(std::size_t size) : _count(size)
    {
        if (size)
        {
            _elements = std::make_unique<T[]>(size);
        }
    }

    reference operator[](std::size_t index) noexcept
    {
        assert(index < _count);
        return *(data() + index);
    }

    const_reference operator[](std::size_t index) const noexcept
    {
        assert(index < _count);
        return *(data() + index);
    }

    /**
     * Returns an iterator to the first element of the container.
     * @returns An iterator to the first element.
     */
    iterator begin() noexcept
    {
        return data();
    }

    /**
     * Returns an iterator to the first element of the container.
     * @returns An iterator to the first element.
     */
    [[nodiscard]] const_iterator begin() const noexcept
    {
        return data();
    }

    /**
     * Returns pointer to the underlying array serving as element storage.
     * @returns A pointer to the underlying element storage.
     */
    pointer data() noexcept
    {
        return _elements.get();
    }

    /**
     * Returns pointer to the underlying array serving as element storage.
     * @returns A pointer to the underlying element storage.
     */
    [[nodiscard]] const_pointer data() const noexcept
    {
        return _elements.get();
    }

    /**
     * Returns an iterator to the element following the last element of the
     * container.
     * @returns An iterator to the element following the last element.
     */
    iterator end() noexcept
    {
        return data() + _count;
    }

    /**
     * Returns an iterator to the element following the last element of the
     * container.
     * @returns An iterator to the element following the last element.
     */
    [[nodiscard]] const_iterator end() const noexcept
    {
        return data() + _count;
    }

    /**
     * Returns the number of elements in the container.
     * @returns The number of elements in the container.
     */
    [[nodiscard]] std::size_t size() const noexcept
    {
        return _count;
    }

private:
    std::size_t _count = 0;
    std::unique_ptr<T[]> _elements;
};

/**
 * Represents a fixed size unordered lookup collection.
 */
template <class Key, class Value>
class fixed_hashmap
{
public:
    static_assert(
        std::is_trivially_destructible_v<Key>,
        "destructor is not implemented");
    static_assert(
        std::is_trivially_destructible_v<Value>,
        "destructor is not implemented");

    /**
     * The maximum number of items that an instance can contain.
     */
    static constexpr std::size_t maximum_capacity = 127u;

    using value_type = std::pair<Key, Value>;
    using const_iterator = const value_type*;

    /**
     * Inserts a new element into the container, constructed in-place with the
     * given args if there is no element with the key.
     * @tparam Args The argument types for the constructor of the element.
     * @param key  The key to associate with the value.
     * @param args Arguments to forward to the constructor of the element.
     */
    template <class... Args>
    void emplace(const Key& key, Args&&... args)
    {
        std::size_t bucket = get_bucket(key);
        auto it = get_entry(bucket);
        if (it == nullptr)
        {
            create_entry(
                std::make_pair(key, Value(std::forward<Args>(args)...)));

            // entry indexes are one based, so next_free_index points to the
            // one after the entry we just made, which is just what we need
            _buckets[bucket] = _next_free_index;
        }
        else
        {
            entry* previous = it;
            do
            {
                if (it->pair.first == key)
                {
                    return;
                }

                previous = it;
                it = it->next;
            } while (it != nullptr);

            previous->next = create_entry(
                std::make_pair(key, Value(std::forward<Args>(args)...)));
        }
    }

    /**
     * Returns an iterator to the element following the last element of the
     * container.
     * @returns An iterator to the element following the last element.
     */
    [[nodiscard]] const_iterator end() const noexcept
    {
        return nullptr;
    }

    /**
     * Finds an element with the specified key.
     * @param key The key value of the element to search for.
     * @returns An iterator to an element with the specified key, or the value
     *          of `end()` is no such key is found.
     */
    [[nodiscard]] const_iterator find(const Key& key) const
    {
        const entry* it = get_entry(get_bucket(key));
        while (it != nullptr)
        {
            if (it->pair.first == key)
            {
                return &it->pair;
            }

            it = it->next;
        }

        return end();
    }

private:
    struct entry
    {
        entry* next;
        value_type pair;
    };

    using entry_type = std::aligned_storage_t<sizeof(entry), alignof(entry)>;

    entry* create_entry(value_type&& pair)
    {
        assert(_next_free_index < _entries.size());
        entry* e = get_entry(&_entries[_next_free_index]);
        ++_next_free_index;

        e->pair = std::move(pair);
        return e;
    }

    [[nodiscard]] entry* get_entry(std::size_t bucket) const
    {
        // We use 1-based indexes so we know the difference between empty
        // buckets and used ones
        std::size_t index = _buckets[bucket];
        if (index == 0)
        {
            return nullptr;
        }
        else
        {
            return get_entry(_entries.data() + index - 1);
        }
    }

    entry* get_entry(const entry_type* raw) const
    {
        return const_cast<entry*>(
            std::launder(reinterpret_cast<const entry*>(raw)));
    }

    [[nodiscard]] std::size_t get_bucket(const Key& key) const
    {
        return std::hash<Key>{}(key) % _buckets.size();
    }

    std::array<std::uint8_t, maximum_capacity> _buckets{};
    std::array<entry_type, maximum_capacity> _entries{};
    std::uint8_t _next_free_index = 0;
};

/**
 * Represents a dynamically sized container that stores elements
 * contiguously.
 * @remarks Initially, small numbers of elements will be stored locally,
 *          however, is the size exceeds local storage then they will be
 *          switched to use dynamic storage automatically.
 */
template <class T>
class small_vector
{
public:
    static constexpr std::size_t maximum_local_elements = 4;

    using const_iterator = const T*;
    using const_pointer = const T*;
    using iterator = T*;
    using pointer = T*;
    using value_type = T;

    /**
     * Constructs a new instance of the `small_vector<T>` class.
     */
    small_vector() : _storage{}
    {
    }

    /**
     * Destructs an instance of the `small_vector<T>` class.
     */
    ~small_vector() noexcept
    {
        clear();
        if (has_dynamic_storage())
        {
            std::allocator<T> alloc;
            std::allocator_traits<std::allocator<T>>::deallocate(
                alloc, _dynamic, _capacity);
        }
    }

    small_vector(const small_vector<T>&) = delete;

    /**
     * Constructs a new instance of the `small_vector<T>` class.
     * @param other The instance to move the resources from.
     */
    small_vector(small_vector<T>&& other) noexcept(
        std::is_nothrow_move_constructible<T>::value) :
        _capacity(other._capacity),
        _count(other._count),
        _storage{}
    {
        other._count = 0;
        if (has_dynamic_storage())
        {
            _dynamic = other._dynamic;
            other._capacity = 0;
        }
        else
        {
            pointer src = other.get_local_element();
            pointer dst = get_local_element();
            for (std::size_t i = 0; i != _count; i++)
            {
                new (dst) T(std::move(*src));
                ++dst;
                ++src;
            }
        }
    }

    small_vector<T>& operator=(const small_vector<T>&) = delete;

    small_vector<T>& operator=(small_vector<T>&& other) noexcept(
        std::is_nothrow_move_assignable<T>::value)
    {
        swap_and_zero(other._capacity, _capacity);
        swap_and_zero(other._count, _count);

        if (has_dynamic_storage())
        {
            _dynamic = other._dynamic;
        }
        else
        {
            pointer src = other.get_local_element();
            pointer dst = get_local_element();
            std::move(src, src + _count, dst);
        }

        return *this;
    }

    /**
     * Returns an iterator to the first element of the container.
     * @returns An iterator to the first element.
     */
    iterator begin() noexcept
    {
        return data();
    }

    /**
     * Returns an iterator to the first element of the container.
     * @returns An iterator to the first element.
     */
    [[nodiscard]] const_iterator begin() const noexcept
    {
        return data();
    }

    /**
     * Erases all elements from the container.
     */
    void clear() noexcept
    {
        std::destroy_n(data(), _count);
        _count = 0;
    }

    /**
     * Returns pointer to the underlying array serving as element storage.
     * @returns A pointer to the underlying element storage.
     */
    pointer data() noexcept
    {
        if (has_dynamic_storage())
        {
            return get_dynamic_element();
        }
        else
        {
            return get_local_element();
        }
    }

    /**
     * Returns pointer to the underlying array serving as element storage.
     * @returns A pointer to the underlying element storage.
     */
    [[nodiscard]] const_pointer data() const noexcept
    {
        if (has_dynamic_storage())
        {
            return get_dynamic_element();
        }
        else
        {
            return get_local_element();
        }
    }

    /**
     * Appends a new element to the end of the container.
     * @param args The arguments to forward to the constructor of the element.
     * @returns A reference to the inserted element.
     */
    template <class... Args>
    T& emplace_back(Args&&... args)
    {
        T* element = allocate_element();
        std::allocator<T> alloc;
        std::allocator_traits<std::allocator<T>>::construct(
            alloc, element, std::forward<Args>(args)...);
        ++_count;
        return *element;
    }

    /**
     * Checks if the container has no elements.
     * @returns `true` if the container is empty; otherwise, `false`.
     */
    [[nodiscard]] bool empty() const noexcept
    {
        return _count == 0;
    }

    /**
     * Returns an iterator to the element following the last element of the
     * container.
     * @returns An iterator to the element following the last element.
     */
    iterator end() noexcept
    {
        return data() + _count;
    }

    /**
     * Returns an iterator to the element following the last element of the
     * container.
     * @returns An iterator to the element following the last element.
     */
    [[nodiscard]] const_iterator end() const noexcept
    {
        return data() + _count;
    }

    /**
     * Returns the number of elements in the container.
     * @returns The number of elements in the container.
     */
    [[nodiscard]] std::size_t size() const noexcept
    {
        return _count;
    }

private:
    static void swap_and_zero(std::uint32_t& src, std::uint32_t& dst)
    {
        std::uint32_t tmp = src;
        src = 0;
        dst = tmp;
    }

    pointer allocate_element()
    {
        if (!has_dynamic_storage())
        {
            if (_count < maximum_local_elements)
            {
                return get_local_element() + _count;
            }
            else
            {
                assert(_count == maximum_local_elements);
                create_dynamic_storage();
            }
        }
        else if (_count == _capacity)
        {
            resize_dynamic_storage();
        }

        return get_dynamic_element() + _count;
    }

    void create_dynamic_storage()
    {
        std::allocator<T> alloc;

        static constexpr std::size_t default_capacity =
            maximum_local_elements * 2;
        T* storage = std::allocator_traits<std::allocator<T>>::allocate(
            alloc, default_capacity);

        pointer src = get_local_element();
        std::move(src, src + maximum_local_elements, storage);
        _dynamic = storage;
        _capacity = default_capacity;
    }

    [[nodiscard]] bool has_dynamic_storage() const noexcept
    {
        return _capacity != 0;
    }

    [[nodiscard]] pointer get_dynamic_element() const noexcept
    {
        return _dynamic;
    }

    [[nodiscard]] pointer get_local_element() const noexcept
    {
        return std::launder(
            const_cast<T*>(reinterpret_cast<const T*>(&_storage)));
    }

    void resize_dynamic_storage()
    {
        // 1.5 growth factor
        std::size_t new_size =
            static_cast<std::size_t>(_capacity) + (_capacity / 2u);
        if (new_size > std::numeric_limits<std::uint32_t>::max())
        {
            throw std::bad_alloc();
        }

        std::allocator<T> alloc;
        T* new_storage =
            std::allocator_traits<std::allocator<T>>::allocate(alloc, new_size);

        pointer src = get_dynamic_element();
        std::move(src, src + _count, static_cast<T*>(new_storage));

        std::allocator_traits<std::allocator<T>>::deallocate(
            alloc, _dynamic, _capacity);
        _dynamic = new_storage;
        _capacity = static_cast<std::uint32_t>(new_size);
    }

    std::uint32_t _capacity = 0;
    std::uint32_t _count = 0;
    union {
        T* _dynamic;
        std::aligned_storage_t<sizeof(T), alignof(T)>
            _storage[maximum_local_elements];
    };
};

}

#ifdef _MSC_VER
#pragma warning(pop)
#endif

#endif
