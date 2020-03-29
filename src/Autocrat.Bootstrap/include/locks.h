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

    /**
     * Represents a light-weight shareable lock.
     * @remarks This class provides allows shared and exclusive locking of a
     *          resource but does not allow recursive locking by the same
     *          thread in exclusive mode.
     */
    class shared_spin_lock
    {
    public:
        /**
         * Locks the instance for exclusive ownership.
         */
        void lock();

        /**
         * Locks the instance for shared ownership.
         */
        void lock_shared();

        /**
         * Unlocks the instance from exclusive ownership.
         */
        void unlock();

        /**
         * Unlocks the instance from shared ownership.
         */
        void unlock_shared();
    private:
        std::atomic_uint32_t _counter = 0;
    };
}

#endif
