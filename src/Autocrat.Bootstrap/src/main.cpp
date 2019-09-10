#include <cstdio>

extern "C" void __cdecl OnConfigurationLoaded();

int autocrat_main()
{
    std::printf("Calling managed code...\n");
    OnConfigurationLoaded();
    std::printf("Done.\n");
    return 0;
}
