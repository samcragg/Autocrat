#include <algorithm>
#include <any>
#include "pal.h"
#include "thread_pool.h"
#include "timer_service.h"

namespace
{
    void invoke_callback(std::any& data)
    {
        auto info = std::any_cast<autocrat::timer_info_ptr>(data);
        info->callback(static_cast<std::int32_t>(info->handle));
    }
}

namespace autocrat
{
    timer_service::timer_service(thread_pool* pool) :
        _thread_pool(pool)
    {
    }

    std::uint32_t timer_service::add_timer_callback(duration delay, duration interval, timer_method callback)
    {
        static std::uint32_t handle_counter = 0;
        std::chrono::microseconds now = pal::get_current_time();
        std::uint32_t handle = ++handle_counter;

        timer_info_ptr info(new timer_info());
        info->callback = callback;
        info->interval = interval;
        info->handle = handle;
        
        time_slot slot = {};
        slot.due = now + delay;
        slot.info = std::move(info);
        _slots.push_back(std::move(slot));

        return handle;
    }

    void timer_service::check_and_dispatch()
    {
        std::chrono::microseconds current = pal::get_current_time();

        // Move all the due slots to the end so we can overwrite them with the
        // new due time if they are repeating or erase them if they're not
        auto new_end = std::remove_if(_slots.begin(), _slots.end(), [=](const time_slot& slot)
            {
                return slot.due <= current;
            });

        if (new_end != _slots.end())
        {
            enqueue_callbacks(new_end);
        }
    }

    void timer_service::enqueue_callbacks(std::vector<time_slot>::iterator begin)
    {
        auto new_end = begin;
        for (auto it = begin; it != _slots.end(); ++it)
        {
            _thread_pool->enqueue(&invoke_callback, it->info);

            // Queue it up again if required
            std::chrono::microseconds interval = it->info->interval;
            if (interval.count() > 0)
            {
                it->due += interval;
                *new_end = std::move(*it);
                ++new_end;
            }
        }

        if (new_end == begin)
        {
            _slots.erase(new_end, _slots.end());
        }
        else
        {
            _slots.erase(new_end, _slots.end());
            std::sort(begin, _slots.end());
            std::inplace_merge(_slots.begin(), begin, _slots.end());
        }
    }

    bool operator<(const timer_service::time_slot& a, const timer_service::time_slot& b)
    {
        return a.due < b.due;
    }
}
