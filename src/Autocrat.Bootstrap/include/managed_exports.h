#ifndef MANAGED_EXPORTS_H
#define MANAGED_EXPORTS_H

#include "defines.h"

namespace managed_exports
{

extern "C" void* CDECL GetByteArrayType();
extern "C" void CDECL InitializeManagedThread();
extern "C" bool CDECL LoadConfiguration(void* source);
extern "C" void CDECL OnConfigurationLoaded();
extern "C" void CDECL RegisterManagedTypes();

}

#endif
