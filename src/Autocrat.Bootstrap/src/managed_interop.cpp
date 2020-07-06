#include "managed_interop.h"
#include "collections.h"
#include "services.h"
#include <cassert>
#include <cstdint>
#include <spdlog/spdlog.h>
#include <unordered_map>

namespace
{

struct gc_header
{
    std::uint32_t padding;
    std::uint32_t sync_block;
};
static_assert(sizeof(gc_header) == 8u);

// Note that we don't need this to be thread safe - it's OK if two threads
// use the same value, as they won't be scanning the same area of managed
// memory and, therefore, can't mark another threads object as scanned.
std::uint32_t scan_counter = 0u;

}

namespace autocrat::detail
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

bool has_reference_fields(const managed_type* type)
{
    const std::uint32_t has_pointers_flag = 0x0020;
    return (type->flags & has_pointers_flag) != 0;
}

void* reference_scanner::move(void* root)
{
    if (root == nullptr)
    {
        return nullptr;
    }

    std::optional<void*> moved_object = get_moved_location(root);
    if (moved_object)
    {
        return *moved_object;
    }

    auto object = static_cast<managed_object*>(root);
    const managed_type* type = object->type;
    std::size_t bytes = type->base_size;
    if (type->component_size > 0)
    {
        auto array = static_cast<const managed_array*>(object);
        bytes += static_cast<std::size_t>(type->component_size) * array->length;
    }

    void* moved = move_object(object, bytes);
    set_moved_location(object, moved);
    if (has_reference_fields(type))
    {
        scan(object, moved, type);
    }

    return moved;
}

void reference_scanner::scan(
    managed_object* object,
    void* copy,
    const managed_type* type)
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
        std::size_t count =
            (type->base_size + block->series_size) / sizeof(void*);
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

void reference_scanner::scan_references(
    void* object,
    void* copy,
    std::size_t offset,
    std::size_t count)
{
    while (count-- > 0)
    {
        void* instance = get_reference(object, offset);
        void* new_reference = move(instance);
        set_reference(copy, offset, new_reference);

        offset += sizeof(void*);
    }
}

template <class Storage>
class deserializer : private reference_scanner
{
public:
    template <class... Args>
    explicit deserializer(std::byte* data, Args&&... storage_args) :
        _data(data),
        _moved_objects(std::forward<Args>(storage_args)...)
    {
    }

    void* restore()
    {
        return move(_data);
    }

protected:
    std::optional<void*> get_moved_location(void* object) override
    {
        auto it = _moved_objects.find(object);
        if (it == _moved_objects.end())
        {
            return std::nullopt;
        }
        else
        {
            return it->second;
        }
    }

    void* get_reference(void* object, std::size_t offset) override
    {
        std::ptrdiff_t reference_offset = *reinterpret_cast<std::ptrdiff_t*>(
            static_cast<std::byte*>(object) + offset);

        // See comments in serializer::set_reference
        if (reference_offset < 0)
        {
            return nullptr;
        }
        else
        {
            return _data + reference_offset;
        }
    }

    void* move_object(void* object, std::size_t) override
    {
        return object;
    }

    void set_moved_location(void* object, void* new_location) override
    {
        _moved_objects.emplace(object, new_location);
    }

    void set_reference(void* object, std::size_t offset, void* reference)
        override
    {
        void* field_address = static_cast<std::byte*>(object) + offset;
        *static_cast<void**>(field_address) = reference;
    }

private:
    std::byte* _data;
    Storage _moved_objects;
};

class serializer : private reference_scanner
{
public:
    explicit serializer(memory_pool_buffer& buffer) :
        _buffer(&buffer),
        _objects(0)
    {
    }

    std::size_t moved_objects() const noexcept
    {
        return _objects;
    }

    void save(void* object)
    {
        move(object);
    }

protected:
    static constexpr std::uintptr_t moved_bit = 0x01;
    static constexpr std::uintptr_t moved_mask = ~moved_bit;

    std::optional<void*> get_moved_location(void* object) override
    {
        // We store the moved location inside the object (every object
        // contains a pointer to its type), using the last bit of the
        // pointer to know if it's the location or the normal type.
        auto type = reinterpret_cast<std::uintptr_t>(
            static_cast<managed_object*>(object)->type);
        if ((type & moved_bit) == 0)
        {
            return std::nullopt;
        }
        else
        {
            return reinterpret_cast<void*>(type & moved_mask);
        }
    }

    void* get_reference(void* object, std::size_t offset) override
    {
        void* address_of_field = static_cast<std::byte*>(object) + offset;
        return *static_cast<void**>(address_of_field);
    }

    void* move_object(void* object, std::size_t size) override
    {
        ++_objects;

        // Add one to the offset so that we can tell the difference between
        // null and a reference to this instance (which would have an
        // offset of zero)
        std::size_t offset = _buffer->size() + 1u;
        _buffer->append(static_cast<std::byte*>(object), size);
        return reinterpret_cast<void*>(offset);
    }

    void set_moved_location(void* object, void* new_location) override
    {
        auto pointer =
            reinterpret_cast<std::uintptr_t>(new_location) | moved_bit;
        static_cast<managed_object*>(object)->type =
            reinterpret_cast<autocrat::detail::managed_type*>(pointer);
    }

    void set_reference(void* object, std::size_t offset, void* reference)
        override
    {
        // move_object adds one to the offset to distinguish between the
        // root object and null. This has the side effect that nulls will
        // be stored as a negative index, which help the deserializer
        std::ptrdiff_t value = reinterpret_cast<std::ptrdiff_t>(reference) - 1;

        // object is the value returned by move_object, which in our case
        // is the index (+1) to where we started writing the object
        auto index = reinterpret_cast<std::size_t>(object) - 1u;
        _buffer->replace(
            index + offset,
            reinterpret_cast<std::byte*>(&value),
            sizeof(std::ptrdiff_t));
    }

private:
    static_assert(sizeof(std::ptrdiff_t) <= sizeof(void*));
    memory_pool_buffer* _buffer;
    std::size_t _objects;
};

}

namespace autocrat
{

void object_scanner::scan(void* object)
{
    // We increase the scan counter by 2 each time so that we can always
    // OR it with 1, which prevents us from ever using 0 as our version
    // number (if we initialized the counter to non-zero value and just
    // incremented it each time, it would eventually overflow to 0). Also
    // note that we don't care about thread safety; it's OK to use the same
    // number on multiple threads
    scan_counter += 2;
    _version = scan_counter | 0x1;

    move(object);
}

std::optional<void*> object_scanner::get_moved_location(void* object)
{
    // Before each object is the GC header
    gc_header& header = static_cast<gc_header*>(object)[-1];
    if (header.padding == _version)
    {
        return object;
    }
    else
    {
        return std::nullopt;
    }
}

void* object_scanner::get_reference(void* object, std::size_t offset)
{
    void* address_of_field = static_cast<std::byte*>(object) + offset;
    void** field = static_cast<void**>(address_of_field);
    on_field(field);
    return *field;
}

void* object_scanner::move_object(void* object, std::size_t size)
{
    on_object(object, size);

    // We mark the object as moved inside the GC header space that proceeds
    // each object
    gc_header& header = static_cast<gc_header*>(object)[-1];
    header.padding = _version;
    return object;
}

void object_scanner::set_moved_location(void*, void*)
{
}

void object_scanner::set_reference(void*, std::size_t, void*)
{
}

void* object_serializer::restore()
{
    std::size_t size = _buffer.size();
    gc_service* gc = global_services.get_service<gc_service>();

    void* object = gc->allocate(size);
    auto buffer = static_cast<std::byte*>(object);
    _buffer.move_to(buffer, size);

    using fixed_hash = fixed_hashmap<void*, void*>;
    if (_references <= fixed_hash::maximum_capacity)
    {
        detail::deserializer<fixed_hash> d(buffer);
        return d.restore();
    }
    else
    {
        spdlog::debug("Warning: Large object graph serialized, this could "
                      "affect performance.");
        detail::deserializer<std::unordered_map<void*, void*>> d(
            buffer, _references);
        return d.restore();
    }
}

void object_serializer::save(void* object)
{
    detail::serializer s(_buffer);
    s.save(object);
    _references = s.moved_objects();
}

}
