#ifndef DEFINES_H
#define DEFINES_H

#ifdef UNIT_TESTS

#define GIVE_ACCESS_TO_MOCKS protected:
#define MOCKABLE_CONSTRUCTOR(class_name) class_name() = default;
#define MOCKABLE_CONSTRUCTOR_AND_DESTRUCTOR(class_name) class_name() = default; virtual ~class_name() noexcept = default;
#define MOCKABLE_GLOBAL(type) type&
#define MOCKABLE_METHOD virtual
#define FINAL_CLASS

#else

#define GIVE_ACCESS_TO_MOCKS
#define MOCKABLE_CONSTRUCTOR(class_name)
#define MOCKABLE_CONSTRUCTOR_AND_DESTRUCTOR(class_name)
#define MOCKABLE_GLOBAL(type) type
#define MOCKABLE_METHOD
#define FINAL_CLASS final

#endif

#if __GNUC__
#define CDECL
#else
#undef CDECL
#define CDECL __cdecl
#endif

#if NDEBUG
#define UNUSED(x) ((void)x)
#else
#define UNUSED(x)
#endif

#endif
