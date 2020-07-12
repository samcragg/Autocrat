#ifndef PAL_TESTS_H
#define PAL_TESTS_H

// Ctrl-C handling in Windows is weird for a few reasons, one being that
// there's no way to stop the child process from terminating the parent process
// (by default). Therefore, we have to jump through some hoops to respawn this
// current process with certain flags to stop it bringing down the unit tests.
// We could just skip testing it, but because of the quirks of handling ctrl-c
// it's actual an area that needs testing.
bool is_ctrl_c_test(const char* argument);
int run_ctrl_c_test();

#endif
