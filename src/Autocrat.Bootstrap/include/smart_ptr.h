#ifndef SMART_PTR_H
#define SMART_PTR_H

#include <atomic>
#include <cassert>
#include <cstdint>

namespace autocrat
{

    /**
     * Stores a pointer to an object with an embedded reference count.
     * @tparam T The type of the pointed to object.
     */
    template <class T>
    class intrusive_ptr
    {
    public:
        using element_type = T;

        intrusive_ptr() noexcept :
            intrusive_ptr(nullptr)
        {
        }

        explicit intrusive_ptr(T* pointer) noexcept :
            _pointer(pointer)
        {
            if (_pointer != nullptr)
            {
                intrusive_ptr_add_ref(_pointer);
            }
        }

        intrusive_ptr(const intrusive_ptr& other) noexcept :
            _pointer(other._pointer)
        {
            if (_pointer != nullptr)
            {
                intrusive_ptr_add_ref(_pointer);
            }
        }

        intrusive_ptr(intrusive_ptr&& other) noexcept :
            _pointer(other._pointer)
        {
            other._pointer = nullptr;
        }

        ~intrusive_ptr()
        {
            if (_pointer != nullptr)
            {
                intrusive_ptr_release(_pointer);
            }
        }

        explicit operator bool() const noexcept
        {
            return _pointer != nullptr;
        }

        intrusive_ptr& operator=(const intrusive_ptr& other) noexcept
        {
            intrusive_ptr temp(other);
            swap(temp);
            return *this;
        }

        intrusive_ptr& operator=(intrusive_ptr&& other) noexcept
        {
            intrusive_ptr temp(static_cast<intrusive_ptr&&>(other));
            swap(temp);
            return *this;
        }

        element_type& operator*() const noexcept
        {
            assert(_pointer != nullptr);
            return *_pointer;
        }

        element_type* operator->() const noexcept
        {
            assert(_pointer != nullptr);
            return _pointer;
        }

        /**
         * Returns the stored pointer.
         * @returns The stored pointer.
         */
        element_type* get() const noexcept
        {
            return _pointer;
        }

        /**
         * Exchanges the contents of this instance and `other`.
         * @param other The instance to exchange contents with.
         */
        void swap(intrusive_ptr& other) noexcept
        {
            T* tmp = _pointer;
            _pointer = other._pointer;
            other._pointer = tmp;
        }
    private:
        T* _pointer;
    };

    /**
     * Determines whether two `intrusive_ptr` instances are equal.
     * @tparam T The type of the pointed to object.
     * @param a The value to compare.
     * @param b The value to compare.
     * @returns `true` if the instances point to the same value; otherwise, `false`.
     */
    template <class T>
    bool operator==(const intrusive_ptr<T>& a, const intrusive_ptr<T>& b)
    {
        return a.get() == b.get();
    }
    
    /**
     * Exchanges the given values.
     * @tparam T The type of the pointed to object.
     * @param lhs The value to swap.
     * @param rhs The value to swap.
     */
    template <class T>
    void swap(intrusive_ptr<T>& lhs, intrusive_ptr<T>& rhs) noexcept
    {
        lhs.swap(rhs);
    }

    /**
     * A reference counter base class.
     * @tparam T The derived class.
     */
    template <class T>
    class intrusive_ref_counter
    {
    public:
        intrusive_ref_counter() noexcept :
            _counter(0)
        {
        }

        intrusive_ref_counter(const intrusive_ref_counter&) noexcept :
            _counter(0)
        {
        }

        intrusive_ref_counter& operator=(const intrusive_ref_counter&) noexcept
        {
            return *this;
        }

        /**
         * Adds a reference to the specified object.
         * @tparam T The derived class.
         * @param pointer The object to add a reference to.
         */
        friend void intrusive_ptr_add_ref(const intrusive_ref_counter<T>* pointer) noexcept
        {
            pointer->_counter++;
        }

        /**
         * Removes a reference to the specified object, releasing the object
         * when there are no more references to it.
         * @tparam T The derived class.
         * @param pointer The object to remove a reference from.
         */
        friend void intrusive_ptr_release(const intrusive_ref_counter<T>* pointer) noexcept
        {
            std::uint32_t count = --pointer->_counter;
            if (count == 0)
            {
                delete static_cast<const T*>(pointer);
            }
        }
    protected:
        ~intrusive_ref_counter() = default;
    private:
        mutable std::atomic_uint32_t _counter;
    };
}

#endif
