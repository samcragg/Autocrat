#ifndef LOCKS_H
#define LOCKS_H

#include <atomic>
#include <cstdint>

namespace autocrat
{
    /**
     * Represents a light-weight lock over a resource.
     * @remarks This class provides mutual exclusion facility and can be locked
     *          recursively by the same thread.
     */
    class exclusive_lock
    {
    public:
        /**
         * Initializes a new instance of the `exclusive_lock` class.
         */
        exclusive_lock();

        /**
         * Tries to acquire the lock, without blocking.
         * @returns `true` on successful lock acquisition; otherwise, `false`.
         */
        bool try_lock();

        /**
         * Releases the lock.
         */
        void unlock();
    private:
        std::atomic_uint32_t _owner_id;
        std::uint32_t _lock_count; 
    };
}

#endif
