#include <cassert>
#include "managed_interop.h"

namespace autocrat
{
    namespace detail
    {
        // Object
        struct managed_object
        {
            mutable const managed_type* type;
        };

        // Array
        struct managed_array : managed_object
        {
            std::uint32_t length;
        };

        // EEType
        struct managed_type
        {
            std::uint16_t component_size;
            std::uint16_t flags;
            std::uint32_t base_size;
            managed_type* base_type;
        };
    }
}

namespace
{
    // CGCDescSeries
    struct block_layout
    {
        std::size_t series_size;
        std::size_t start_offset;
    };

    struct managed_block_data
    {
        block_layout last_entry;
        std::size_t entry_count;
    };

    const std::uintptr_t scanned_bit = 0x01;

    bool has_reference_fields(const autocrat::detail::managed_type* type)
    {
        const std::uint32_t has_pointers_flag = 0x0020;
        return (type->flags & has_pointers_flag) != 0;
    }

    bool has_scanned(const autocrat::detail::managed_object* object)
    {
        return reinterpret_cast<std::uintptr_t>(object->type) & scanned_bit;
    }

    void mark_as_scanned(const autocrat::detail::managed_object* object, bool value)
    {
        const std::uintptr_t scanned_mask = ~scanned_bit;
        auto pointer = reinterpret_cast<std::uintptr_t>(object->type) & scanned_mask;
        if (value)
        {
            pointer |= scanned_bit;
        }

        object->type = reinterpret_cast<const autocrat::detail::managed_type*>(pointer);
    }
}

namespace autocrat
{
    using namespace detail;

    std::uint32_t reference_scanner::bytes() const noexcept
    {
        return _bytes;
    }

    void reference_scanner::scan(const void* root)
    {
        auto object = static_cast<const managed_object*>(root);
        if ((object == nullptr) || has_scanned(object))
        {
            return;
        }

        const managed_type* type = object->type;
        mark_as_scanned(object, true);
        if (has_reference_fields(type))
        {
            scan(object, type);
        }

        _bytes += type->base_size;
        if (type->component_size > 0)
        {
            auto array = static_cast<const managed_array*>(object);
            _bytes += type->component_size * array->length;
        }
    }

    void reference_scanner::scan(const managed_object* object, const managed_type* type)
    {
        // The layout of the references in a type goes backwards from the top
        // of the type information:
        //
        // -------------------
        // | block_layout[0] |
        // |       ...       |
        // | block_layout[n] |
        // | size_t entries  |
        // |-----------------|
        // | managed_type    |
        // -------------------
        auto& block_info = reinterpret_cast<const managed_block_data*>(type)[-1];
        const block_layout* block = &block_info.last_entry;
        for (std::size_t i = block_info.entry_count; i > 0; --i)
        {
            std::size_t count = (type->base_size + block->series_size) / sizeof(void*);
            auto address = reinterpret_cast<const char*>(object) + block->start_offset;
            scan_references(address, count);
            --block;
        }

        // Now handle arrays
        if (type->component_size > 0)
        {
            assert(type->component_size == sizeof(std::intptr_t));
            auto array = static_cast<const managed_array*>(object);
            scan_references(array + 1, array->length); //+1 to scan the region after the array
        }
    }

    void reference_scanner::scan_references(const void* address, std::size_t count)
    {
        while (count-- > 0)
        {
            auto reference = reinterpret_cast<const void* const*>(address);
            if (*reference != nullptr)
            {
                scan(*reference);
            }

            address = static_cast<const char*>(address) + sizeof(std::size_t);
        }
    }
}
