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

    /**
     * Interface for the moving of managed object memory.
     */
    struct object_mover
    {
        /**
         * Gets the specified reference field in the object.
         * @param object The address of the object.
         * @param offset The offset of the field.
         * @returns The value for the reference.
         */
        virtual void* get_reference(const std::byte* object, std::size_t offset) = 0;

        /**
         * Copies the object into another memory region.
         * @param object The address of the object.
         * @param size   The size, in bytes, of the object.
         * @returns The address of the new object.
         */
        virtual void* move_object(const std::byte* object, std::size_t size) = 0;

        /**
         * Sets the specified reference field in the object.
         * @param object    The address of the object.
         * @param offset    The offset of the field.
         * @param reference The new value for the reference.
         */
        virtual void set_reference(std::byte* object, std::size_t offset, void* reference) = 0;
    };

    /**
     * Scans the managed object graph for all references.
     */
    class reference_scanner
    {
    public:
        explicit reference_scanner(object_mover& mover);

        /**
         * Moves the object graph and its references.
         * @param root The root of the object graph to scan.
         * @returns The address of the object after it has been moved.
         * @remarks The passed in object will be in an invalid state after this
         *          method has returned (i.e. its data will be inaccessible).
         */
        void* move(void* root);
    private:
        void* move_object(detail::managed_object* object, std::size_t size);
        void scan(detail::managed_object* object, void* copy, const detail::managed_type* type);
        void scan_references(const void* object, void* copy, std::size_t offset, std::size_t count);

        object_mover* _mover;
    };
}

#endif
