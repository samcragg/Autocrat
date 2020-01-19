#include <spdlog/spdlog.h>
#include "pal.h"
#include "pause.h"
#include "thread_pool.h"

namespace autocrat
{
    thread_pool::thread_pool(std::size_t cpu_id, ::size_t threads) :
        _is_running(true),
        _sleeping(0),
        _threads(threads),
        _wait_handle(0)
    {
        spdlog::info("Creating {} threads with affinity starting from {}", threads, cpu_id);

        for (std::size_t i = 0; i != threads; ++i)
        {
            _threads[i] = std::thread(&thread_pool::perform_work, this, i);
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

        for (auto& thread : _threads)
        {
            try
            {
                thread.join();
            }
            catch (...)
            {
            }
        }
    }

    void thread_pool::add_observer(lifetime_service* service)
    {
        _observers.push_back(service);
    }

    void thread_pool::enqueue(callback_function function, std::any&& arg)
    {
        if (_sleeping != 0)
        {
            pal::wake_all(&_wait_handle);
        }

        _work.emplace(std::make_tuple(function, std::move(arg)));
    }

    std::size_t thread_pool::size() const noexcept
    {
        return _threads.size();
    }

    void thread_pool::invoke_work_item(std::size_t index, work_item& item) const
    {
        for (lifetime_service* observer : _observers)
        {
            observer->on_begin_work(index);
        }

        std::get<callback_function>(item)(std::get<std::any>(item));
        
        for (lifetime_service* observer : _observers)
        {
            observer->on_end_work(index);
        }
    }

    void thread_pool::perform_work(std::size_t index)
    {
        const std::uint32_t maximum_spins = 1000;
        std::uint32_t spin_count = 0;

        while (_is_running)
        {
            work_item work = {};
            if (_work.pop(&work))
            {
                spin_count = 0;
                invoke_work_item(index, work);
            }
            else if (spin_count < maximum_spins)
            {
                ++spin_count;
                pause();
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
        if (count != _threads.size())
        {
            pal::wait_on(&_wait_handle);
        }

        --_sleeping;
    }
}