#include "thread_pool.h"
#include "pal.h"
#include <immintrin.h>

namespace autocrat
{
    thread_pool::thread_pool(std::size_t cpu_id, ::size_t threads) :
        _is_running(true),
        _sleeping(0),
        _threads(new std::thread[threads]),
        _thread_count(threads),
        _wait_handle(0)
    {
        for (std::size_t i = 0; i != _thread_count; ++i)
        {
            _threads[i] = std::thread(&thread_pool::perform_work, this);
            pal::set_affinity(_threads[i], cpu_id + i);
        }
    }

    thread_pool::~thread_pool() noexcept
    {
        _is_running = false;
        while (_sleeping > 0)
        {
            pal::wake_all(&_wait_handle);
        }

        for (std::size_t i = 0; i != _thread_count; ++i)
        {
            try
            {
                _threads[i].join();
            }
            catch (...)
            {
            }
        }
    }

    void thread_pool::enqueue(callback_function function, std::any&& arg)
    {
        if (_sleeping != 0)
        {
            pal::wake_all(&_wait_handle);
        }

        _work.emplace(std::make_tuple(function, std::move(arg)));
    }

    void thread_pool::perform_work()
    {
        const std::uint32_t maximum_spins = 1000;
        std::uint32_t spin_count = 0;

        while (_is_running)
        {
            work_item work = {};
            if (_work.pop(&work))
            {
                spin_count = 0;
                auto& [callback, arg] = work;
                callback(arg);
            }
            else if (spin_count < maximum_spins)
            {
                ++spin_count;
                _mm_pause();
            }
            else
            {
                spin_count = 0;
                wait_for_work();
            }
        }
    }

    void thread_pool::wait_for_work()
    {
        std::uint32_t count = ++_sleeping;

        // Ensure at least one thread is immediately available
        if (count != _thread_count)
        {
            pal::wait_on(&_wait_handle);
        }

        --_sleeping;
    }
}