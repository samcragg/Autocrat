#include <cstdio>
#include "managed_exports.h"

int autocrat_main()
{
    std::printf("Calling configuration loaded...\n");
    managed_exports::OnConfigurationLoaded();
    std::printf("Done.\n");
    return 0;
}
