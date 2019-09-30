#ifndef COLLECTIONS_H
#define COLLECTIONS_H

#include <array>
#include <atomic>
#include <cstddef>
#include <new>
#include <type_traits>
#include <utility>
#include <immintrin.h>

namespace autocrat
{
#pragma warning (push)
#pragma warning (disable: 4324) // structure was padded due to alignment specifier

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
         * @param keep_checking Wait for data to arrive whilst this flag is true.
         * @return The element from the beginning of the queue.
         */
        T pop(const std::atomic_bool& keep_checking)
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
                    if (!keep_checking.load(std::memory_order_relaxed))
                    {
                        return T();
                    }

                    _mm_pause();
                }
                else
                {
                    position = _dequeue_position.load(std::memory_order_relaxed);
                }
            }

            T* item = std::launder(reinterpret_cast<T*>(&cell->storage));
            T copy(std::move(*item));
            item->~T();
            cell->sequence.store(position + Sz + 1, std::memory_order_release);
            return copy;
        }

    private:
        struct cell_t
        {
            std::atomic_size_t sequence;
            typename std::aligned_storage<sizeof(T), alignof(T)>::type storage;
        };

        std::array<cell_t, Sz> _buffer;
        alignas(std::hardware_destructive_interference_size) std::atomic_size_t _enqueue_position;
        alignas(std::hardware_destructive_interference_size) std::atomic_size_t _dequeue_position;
    };
#pragma warning(pop)

}

#endif
