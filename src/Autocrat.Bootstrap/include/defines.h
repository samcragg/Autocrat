#ifndef DEFINES_H
#define DEFINES_H

#ifdef UNIT_TESTS

#define GIVE_ACCESS_TO_MOCKS protected:
#define MOCKABLE_CONSTRUCTOR(class_name) class_name() = default;
#define MOCKABLE_GLOBAL(type) type&
#define MOCKABLE_METHOD virtual

#else

#define GIVE_ACCESS_TO_MOCKS
#define MOCKABLE_CONSTRUCTOR(class_name)
#define MOCKABLE_GLOBAL(type) type
#define MOCKABLE_METHOD

#endif

#endif
