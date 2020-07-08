#include "locks.h"
#include "pause.h"
#include <cassert>
#include <thread>

namespace
{

constexpr static std::uint32_t unlocked = 0xFFFF'FFFFu;
constexpr std::uint32_t writer_bit = 0x8000'0000u;

std::uint32_t get_current_thread_id()
{
    std::hash<std::thread::id> hasher;
    return static_cast<std::uint32_t>(hasher(std::this_thread::get_id()));
}

}

namespace autocrat
{

exclusive_lock::exclusive_lock() : _owner_id(unlocked)
{
}

bool exclusive_lock::try_lock()
{
    std::uint32_t current_thread = get_current_thread_id();
    std::uint32_t owner = unlocked;
    _owner_id.compare_exchange_strong(
        owner, current_thread, std::memory_order_acq_rel);
    if ((owner == unlocked) || (owner == current_thread))
    {
        ++_lock_count;
        return true;
    }
    else
    {
        return false;
    }
}

void exclusive_lock::unlock()
{
    assert(_lock_count > 0);
    assert(_owner_id.load() == get_current_thread_id());

    --_lock_count;
    if (_lock_count == 0)
    {
        _owner_id.store(unlocked, std::memory_order_release);
    }
}

void shared_spin_lock::lock()
{
    std::uint32_t counter = _counter.load(std::memory_order_acquire);
    std::uint32_t counter_locked = 0;
    do
    {
        // Allow the CAS to fail if the writer bit was already set - we'll
        // use the CAS instruction to atomically load the value again
        counter = counter & ~writer_bit;
        counter_locked = counter | writer_bit;

        // Let the CPU know we're in a busy wait - this may have a very
        // slight impact on the first time we get the lock if there is no
        // contention
        pause();
    } while (!_counter.compare_exchange_weak(
        counter, counter_locked, std::memory_order_release));

    // Wait for the readers to finish
    while (counter != writer_bit)
    {
        pause();
        counter = _counter.load(std::memory_order_acquire);
    }
}

void shared_spin_lock::lock_shared()
{
    // Optimistically acquire a reader lock (optimise for happy path)
    std::uint32_t counter = _counter.fetch_add(1, std::memory_order_acq_rel);
    while ((counter & writer_bit) != 0)
    {
        // A writer has the lock, undo our add
        counter = _counter.fetch_sub(1, std::memory_order_acq_rel);

        while ((counter & writer_bit) != 0)
        {
            pause();
            counter = _counter.load(std::memory_order_acquire);
        }

        counter = _counter.fetch_add(1, std::memory_order_acq_rel);
    }
}

void shared_spin_lock::unlock()
{
    // A reader might have incremented the value whilst we have the lock,
    // which is fine as it will decrement it, so we can't just blank out the
    // value but instead subtract it so the reader will still see it's
    // incremented value
    assert(_counter & writer_bit);
    _counter.fetch_sub(writer_bit, std::memory_order_release);
}

void shared_spin_lock::unlock_shared()
{
    assert(_counter > 0);
    _counter.fetch_sub(1, std::memory_order_release);
}

}
