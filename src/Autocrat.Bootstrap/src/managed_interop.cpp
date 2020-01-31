#include <cassert>
#include "managed_interop.h"

namespace autocrat
{
    namespace detail
    {
        // Object
        struct managed_object
        {
            managed_type* type;
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

    const std::uintptr_t moved_bit = 0x01;
    const std::uintptr_t moved_mask = ~moved_bit;

    void* get_moved_location(const autocrat::detail::managed_object* object)
    {
        auto pointer = reinterpret_cast<std::uintptr_t>(object->type) & moved_mask;
        return reinterpret_cast<void*>(pointer);
    }

    bool has_reference_fields(const autocrat::detail::managed_type* type)
    {
        const std::uint32_t has_pointers_flag = 0x0020;
        return (type->flags & has_pointers_flag) != 0;
    }

    bool has_moved(const autocrat::detail::managed_object* object)
    {
        return reinterpret_cast<std::uintptr_t>(object->type) & moved_bit;
    }
}

namespace autocrat
{
    using namespace detail;

    reference_scanner::reference_scanner(object_mover& mover) :
        _mover(&mover)
    {
    }

    void* reference_scanner::move(void* root)
    {
        if (root == nullptr)
        {
            return nullptr;
        }

        auto object = static_cast<managed_object*>(root);
        if (has_moved(object))
        {
            return get_moved_location(object);
        }

        const managed_type* type = object->type;
        std::size_t bytes = type->base_size;
        if (type->component_size > 0)
        {
            auto array = static_cast<const managed_array*>(object);
            bytes += static_cast<std::size_t>(type->component_size) * array->length;
        }

        void* moved = move_object(object, bytes);
        if (has_reference_fields(type))
        {
            scan(object, moved, type);
        }

        return moved;
    }

    void* reference_scanner::move_object(managed_object* object, std::size_t size)
    {
        void* moved = _mover->move_object(reinterpret_cast<std::byte*>(object), size);
        auto pointer = reinterpret_cast<std::uintptr_t>(moved) | moved_bit;
        object->type = reinterpret_cast<autocrat::detail::managed_type*>(pointer);
        return moved;
    }

    void reference_scanner::scan(managed_object* object, void* copy, const managed_type* type)
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
            scan_references(object, copy, block->start_offset, count);
            --block;
        }

        // Now handle arrays
        if (type->component_size > 0)
        {
            assert(type->component_size == sizeof(void*));
            std::size_t elements = static_cast<managed_array*>(object)->length;
            scan_references(object, copy, sizeof(managed_array), elements);
        }
    }

    void reference_scanner::scan_references(const void* object, void* copy, std::size_t offset, std::size_t count)
    {
        while (count-- > 0)
        {
            void* instance = _mover->get_reference(static_cast<const std::byte*>(object), offset);
            _mover->set_reference(
                static_cast<std::byte*>(copy),
                offset,
                move(instance));

            offset += sizeof(void*);
        }
    }
}
