#ifndef PAUSE_H
#define PAUSE_H

#ifdef NDEBUG

#include <immintrin.h>

namespace autocrat
{
    inline void pause() noexcept
    {
        _mm_pause();
    }
}

#else

#include <thread>

namespace autocrat
{
    inline void pause() noexcept
    {
        std::this_thread::yield();
    }
}

#endif

#endif
