#ifndef MANAGED_EXPORTS_H
#define MANAGED_EXPORTS_H

#include "defines.h"

namespace managed_exports
{
    extern "C" void* CDECL GetByteArrayType();
    extern "C" void CDECL OnConfigurationLoaded();
}

#endif
