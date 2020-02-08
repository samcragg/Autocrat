#include "ManagedObjects.h"

// These values were obtained from a 64-bit CoreRT project. The members are
// in the order the CLR puts them, not the C# declaration order
EEType object_type = { 0u, 256u, 24u, nullptr, 0u, 0u, 0u, nullptr };
EEType value_type = { 0u, 256u, 24u, &object_type, 0u, 0u, 0u, nullptr };
EEType boxed_int32_type = { 0u, 16648u, 24u, &value_type, 0u, 0u, 0u, nullptr };
EEType array_int32_type = { 4u, 258u, 24u, &boxed_int32_type, 0u, 0u, 0u, nullptr };
EEType SingleReferenceArrayType = { 8u, 290u, 24u, &SingleReferenceInfo.Type, 0u, 0u, 0u, nullptr };

BaseClassInfoType BaseClassInfo;
DerivedClassInfoType DerivedClassInfo;
SingleReferenceInfoType SingleReferenceInfo;
