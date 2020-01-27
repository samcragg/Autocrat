#include <new>
#include "gc_service.h"

namespace autocrat
{
    gc_service::gc_service(thread_pool*)
    {
    }

    void* gc_service::allocate(std::size_t)
    {
        throw std::bad_alloc();
    }
}
