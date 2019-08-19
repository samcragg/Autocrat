#include <cstdio>

extern "C" void __cdecl OnConfigurationLoaded();

int main()
{
    std::printf("Calling managed code...\n");
    OnConfigurationLoaded();
    std::printf("Done.\n");
    return 0;
}
