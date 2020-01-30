#include <atomic>
#include <cassert>
#include <cstdlib>
#include <new>
#include "MemoryMonitor.h"

namespace
{
    std::atomic_size_t allocated;

    std::size_t release(void* ptr)
    {
        std::size_t count = 0u;
        if (ptr != nullptr)
        {
            std::size_t* original = static_cast<std::size_t*>(ptr);
            --original;
            count = *original;
            allocated -= count;
            std::free(original);
        }

        return count;
    }
}

void* operator new(std::size_t bytes)
{
    void* raw = std::malloc(bytes + sizeof(std::size_t));
    if (raw == nullptr)
    {
        throw std::bad_alloc();
    }

    std::size_t* ptr = static_cast<std::size_t*>(raw);
    allocated += bytes;
    *ptr = bytes;
    return &ptr[1];
}

void operator delete(void* ptr) noexcept
{
    release(ptr);
}

void operator delete(void* ptr, std::size_t bytes) noexcept
{
    std::size_t original = release(ptr);
    assert((ptr == nullptr) || (original == bytes));
}

std::size_t allocated_bytes() noexcept
{
    return allocated.load();
}
