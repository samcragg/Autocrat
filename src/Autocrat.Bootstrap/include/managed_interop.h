#ifndef MANAGED_INEROP_H
#define MANAGED_INEROP_H

#include <cstddef>
#include <cstdint>

namespace autocrat
{
    namespace detail
    {
        struct managed_object;
        struct managed_type;
    }

    class reference_scanner
    {
    public:
        /**
         * Gets the total size of the complete object.
         * @returns The number of bytes.
         */
        std::uint32_t bytes() const noexcept;

        /**
         * Scans the object graph for references.
         * @param root The root of the object graph to scan.
         */
        void scan(const void* root);
    private:
        void scan(const detail::managed_object* object, const detail::managed_type* type);
        void scan_references(const void* address, std::size_t count);

        std::uintptr_t _address_max = 0u;
        std::uintptr_t _address_min = 0u;
        std::uint32_t _bytes = 0u;
        std::uint32_t _references = 0u;
    };
}

#endif
