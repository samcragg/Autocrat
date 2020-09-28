#include "thread_pool.h"
#include "pal.h"
#include "pause.h"
#include <mutex>
#include <spdlog/spdlog.h>

namespace
{

static std::mutex thread_initializing;

}

namespace autocrat
{

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
            if (thread.joinable())
            {
                thread.join();
            }
        }
        catch (...)
        {
        }
    }
}

void thread_pool::add_observer(lifetime_service* service)
{
    _observers.emplace_back(service);
}

void thread_pool::enqueue(callback_function function, std::any&& arg)
{
    if (_sleeping != 0)
    {
        pal::wake_all(&_wait_handle);
    }

    _work.emplace<work_item>({function, std::move(arg)});
}

void thread_pool::start(int cpu_id, int threads, initialize_function initialize)
{
    spdlog::info(
        "Creating {} threads with affinity starting from {}", threads, cpu_id);

    // Allocate the threads first, letting the observers know too
    _threads = decltype(_threads)(threads);
    for (auto observer : _observers)
    {
        observer->pool_created(_threads.size());
    }

    // Initialize them
    for (int i = 0; i != threads; ++i)
    {
        _threads[i] =
            std::thread(&thread_pool::perform_work, this, i, initialize);
        if (cpu_id >= 0)
        {
            pal::set_affinity(&_threads[i], cpu_id + i);
        }
    }

    // Wait for initialization to complete before returning
    while (_initialized != _threads.size())
    {
        std::this_thread::yield();
    }

    spdlog::debug("Thread pool initialized");
}

void thread_pool::invoke_work_item(std::size_t index, work_item& item) const
{
    std::size_t i = 0;
    std::size_t size = _observers.size();
    auto observers = _observers.data();
    for (; i != size; ++i)
    {
        observers[i]->begin_work(index);
    }

    item.callback(item.arg);

    while (i-- > 0)
    {
        observers[i]->end_work(index);
    }
}

void thread_pool::perform_work(
    std::size_t index,
    initialize_function initialize)
{
    const std::uint32_t maximum_spins = 1000;
    std::uint32_t spin_count = 0;

    {
        std::scoped_lock lock(thread_initializing);
        spdlog::debug("Initializing thread {}", index);
        initialize(index);
        ++_initialized;
    }

    // Allow other threads to run and, more importantly, the switch to the
    // desired CPU affinity (when the thread's started we could be on any
    // core initially)
    std::this_thread::yield();

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
