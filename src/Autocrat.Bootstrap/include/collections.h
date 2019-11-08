#ifndef COLLECTIONS_H
#define COLLECTIONS_H

#include <algorithm>
#include <array>
#include <atomic>
#include <cstddef>
#include <memory>
#include <new>
#include <type_traits>
#include <utility>

#ifdef _MSC_VER
#pragma warning (push)
#pragma warning (disable: 4324) // structure was padded due to alignment specifier
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
        void emplace(Args&& ... args)
        {
            cell_t* cell;
            std::size_t position = _enqueue_position.load(std::memory_order_relaxed);

            for (;;)
            {
                cell = _buffer.data() + (position % Sz);
                std::size_t sequence = cell->sequence.load(std::memory_order_acquire);

                std::intptr_t delta = static_cast<std::intptr_t>(sequence) - static_cast<std::intptr_t>(position);
                if (delta == 0)
                {
                    if (_enqueue_position.compare_exchange_weak(position, position + 1, std::memory_order_relaxed))
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

            ::new(&cell->storage) T(std::forward<Args>(args)...);
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
            std::size_t position = _dequeue_position.load(std::memory_order_relaxed);
            for (;;)
            {
                cell = _buffer.data() + (position % Sz);
                std::size_t sequence = cell->sequence.load(std::memory_order_acquire);
                std::intptr_t delta = static_cast<std::intptr_t>(sequence) - static_cast<std::intptr_t>(position + 1);
                if (delta == 0)
                {
                    if (_dequeue_position.compare_exchange_weak(position, position + 1, std::memory_order_relaxed))
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
            typename std::aligned_storage<sizeof(T), alignof(T)>::type storage;
        };

        static constexpr std::size_t hardware_destructive_interference_size = 64;

        std::array<cell_t, Sz> _buffer;
        alignas(hardware_destructive_interference_size) std::atomic_size_t _enqueue_position;
        alignas(hardware_destructive_interference_size) std::atomic_size_t _dequeue_position;
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

        small_vector() :
            _count(0),
            _storage({})
        {
        }

        ~small_vector() noexcept
        {
            pointer element = data();
            for (std::size_t i = 0; i != _count; i++)
            {
                element->~T();
                ++element;
            }

            if (_count > maximum_local_elements)
            {
                std::allocator<T> alloc;
                std::allocator_traits<std::allocator<T>>::deallocate(alloc, _dynamic.storage, _dynamic.capacity);
            }
        }

        small_vector(const small_vector<T>&) = delete;

        small_vector(small_vector<T>&& other) noexcept(std::is_nothrow_move_constructible<T>::value) :
            _count(other._count)
        {
            other._count = 0;
            if (_count > maximum_local_elements)
            {
                _dynamic = other._dynamic;
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

        small_vector<T>& operator=(small_vector<T>&& other) noexcept(std::is_nothrow_move_assignable<T>::value)
        {
            std::size_t count = other._count;
            other._count = 0;
            _count = count;

            if (count > maximum_local_elements)
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
        const_iterator begin() const noexcept
        {
            return data();
        }

        /**
         * Returns pointer to the underlying array serving as element storage.
         * @returns A pointer to the underlying element storage.
         */
        pointer data() noexcept
        {
            if (_count <= maximum_local_elements)
            {
                return get_local_element();
            }
            else
            {
                return get_dynamic_element();
            }
        }

        /**
         * Returns pointer to the underlying array serving as element storage.
         * @returns A pointer to the underlying element storage.
         */
        const_pointer data() const noexcept
        {
            if (_count <= maximum_local_elements)
            {
                return get_local_element();
            }
            else
            {
                return get_dynamic_element();
            }
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
        const_iterator end() const noexcept
        {
            return data() + _count;
        }

        /**
         * Appends the given element value to the end of the container.
         * @param value The value of th element to append.
         */
        void push_back(const T& value)
        {
            *allocate_element() = value;
            ++_count;
        }

        /**
         * Appends the given element value to the end of the container.
         * @param value The value of th element to append.
         */
        void push_back(T&& value)
        {
            *allocate_element() = std::move(value);
            ++_count;
        }

        /**
         * Returns the number of elements in the container.
         * @returns The number of elements in the container.
         */
        std::size_t size() const noexcept
        {
            return _count;
        }
    private:
        struct dynamic_memory
        {
            T* storage;
            std::size_t capacity;
        };

        pointer allocate_element()
        {
            if (_count < maximum_local_elements)
            {
                return get_local_element() + _count;
            }
            else if (_count == maximum_local_elements)
            {
                create_dynamic_storage();
            }
            else if (_count == _dynamic.capacity)
            {
                resize_dynamic_storage();
            }

            return get_dynamic_element() + _count;
        }

        void create_dynamic_storage()
        {
            std::allocator<T> alloc;
            
            static constexpr std::size_t default_capacity = maximum_local_elements * 2;
            T* storage = std::allocator_traits<std::allocator<T>>::allocate(alloc, default_capacity);

            pointer src = get_local_element();
            std::move(src, src + maximum_local_elements, storage);
            _dynamic = { storage, default_capacity };
        }

        pointer get_dynamic_element() const noexcept
        {
            return _dynamic.storage;
        }

        pointer get_local_element() const noexcept
        {
            return std::launder(const_cast<T*>(reinterpret_cast<const T*>(&_storage)));
        }

        void resize_dynamic_storage()
        {
            std::allocator<T> alloc;

            std::size_t new_size = _count + (_count / 2); // 1.5 growth factor
            T* new_storage = std::allocator_traits<std::allocator<T>>::allocate(alloc, new_size);

            pointer src = get_dynamic_element();
            std::move(src, src + _count, static_cast<T*>(new_storage));

            std::allocator_traits<std::allocator<T>>::deallocate(alloc, _dynamic.storage, _dynamic.capacity);
            _dynamic.storage = new_storage;
            _dynamic.capacity = new_size;
        }

        std::size_t _count;
        union
        {
            dynamic_memory _dynamic;
            std::aligned_storage_t<sizeof(T) * maximum_local_elements> _storage;
        };
    };
}

#ifdef _MSC_VER
#pragma warning(pop)
#endif

#endif
