#include <cassert>
#include <thread>
#include "locks.h"

namespace
{
    constexpr static std::uint32_t unlocked = 0xFFFF'FFFFu;

    std::uint32_t get_current_thread_id()
    {
        std::hash<std::thread::id> hasher;
        return static_cast<std::uint32_t>(
            hasher(std::this_thread::get_id()));
    }
}

namespace autocrat
{
    exclusive_lock::exclusive_lock() :
        _owner_id(unlocked),
        _lock_count(0)
    {
    }

    bool exclusive_lock::try_lock()
    {
        std::uint32_t current_thread = get_current_thread_id();
        std::uint32_t owner = unlocked;
        _owner_id.compare_exchange_strong(owner, current_thread, std::memory_order_acq_rel);
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
        assert(_owner_id.load() == get_current_thread_id());

        --_lock_count;
        if (_lock_count == 0)
        {
            _owner_id.store(unlocked, std::memory_order_release);
        }
    }
}
