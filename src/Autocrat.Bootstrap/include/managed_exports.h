#ifndef MANAGED_EXPORTS_H
#define MANAGED_EXPORTS_H

#include "defines.h"

namespace managed_exports
{
    extern "C" void* cdecl GetByteArrayType();
    extern "C" void cdecl OnConfigurationLoaded();
}

#endif
