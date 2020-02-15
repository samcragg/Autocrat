#include "gc_service.h"

#include <algorithm>
#include <unordered_set>
#include <gtest/gtest.h>
#include "MemoryMonitor.h"
#include "TestMocks.h"

namespace
{
    void CheckAllocation(autocrat::gc_service& gc, std::size_t size)
    {
        // Repeat a few times to check when memory is recycled
        for (unsigned i = 0; i != 3; ++i)
        {
            gc.on_begin_work(0u);
            auto memory = static_cast<std::byte*>(gc.allocate(size));

            EXPECT_TRUE(std::all_of(memory, memory + size, [](auto b) { return b == std::byte{}; }));

            // Dirty the memory to see if it gets returned
            std::fill_n(memory, size, std::byte{ 255u });
            gc.on_end_work(0u);
        }

    }
}

class GcServiceTests : public testing::Test
{
protected:
    static constexpr std::size_t large_allocation = 200'000u;
    static constexpr std::size_t small_allocation = 200u;
    MockThreadPool _threadPool;
};

TEST_F(GcServiceTests, AllocateShouldAllocateMultipleSmallBuffers)
{
    constexpr std::size_t allocation_count = 100u;
    autocrat::gc_service gc(&_threadPool);
    gc.on_begin_work(0u);

    std::unordered_set<void*> allocations;
    for (std::size_t i = 0; i != allocation_count; ++i)
    {
        allocations.insert(gc.allocate(20'000));
    }

    EXPECT_EQ(allocation_count, allocations.size());
}

TEST_F(GcServiceTests, AllocateShouldZeroFillLargeBuffers)
{
    autocrat::gc_service gc(&_threadPool);
    CheckAllocation(gc, large_allocation);
}

TEST_F(GcServiceTests, AllocateShouldZeroFillSmallBuffers)
{
    autocrat::gc_service gc(&_threadPool);
    CheckAllocation(gc, small_allocation);
}

TEST_F(GcServiceTests, DestructorShouldReleaseAllTheMemory)
{
    std::size_t before_bytes = allocated_bytes();
    {
        autocrat::gc_service gc(&_threadPool);
        gc.on_begin_work(0);
        EXPECT_NE(nullptr, gc.allocate(small_allocation));
        EXPECT_NE(nullptr, gc.allocate(large_allocation));
    }
    std::size_t after_bytes = allocated_bytes();
    EXPECT_EQ(before_bytes, after_bytes);
}

TEST_F(GcServiceTests, OnEndWorkShouldReleaseAllTheMemory)
{
    autocrat::gc_service gc(&_threadPool);
    std::size_t before_bytes = allocated_bytes();

    gc.on_begin_work(0);
    void* small = gc.allocate(small_allocation);
    EXPECT_NE(nullptr, small);
    EXPECT_NE(nullptr, gc.allocate(large_allocation));

    gc.on_end_work(0);
    std::size_t after_bytes = allocated_bytes();
    EXPECT_EQ(before_bytes, after_bytes);
    EXPECT_EQ(small, gc.allocate(small_allocation));
}