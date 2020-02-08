#ifndef MANAGED_INEROP_H
#define MANAGED_INEROP_H

#include <cstddef>
#include <optional>
#include "memory_pool.h"

namespace autocrat
{
    // Expose this to the unit tests, as it's the base for saving/restoring
    // objects to/from a buffer
    namespace detail
    {
        struct managed_object;
        struct managed_type;

        class reference_scanner
        {
        public:
            void* move(void* root);
        protected:
            ~reference_scanner() = default;

            virtual std::optional<void*> get_moved_location(void* object) = 0;
            virtual void* get_reference(void* object, std::size_t offset) = 0;
            virtual void* move_object(void* object, std::size_t size) = 0;
            virtual void set_moved_location(void* object, void* new_location) = 0;
            virtual void set_reference(void* object, std::size_t offset, void* reference) = 0;
        private:
            void scan(managed_object* object, void* copy, const managed_type* type);
            void scan_references(void* object, void* copy, std::size_t offset, std::size_t count);
        };
    }

    /**
     * Allows the saving and loading of an object.
     */
    class object_serializer
    {
    public:
        /**
         * Restores the previously saved object.
         * @returns A pointer to the object.
         */
        void* restore();

        /**
         * Saves the specified object to this instance.
         * @param object The address of the object to save.
         */
        void save(void* object);
    private:
        memory_pool_buffer _buffer;
        std::size_t _references = 0;
    };
}

#endif
