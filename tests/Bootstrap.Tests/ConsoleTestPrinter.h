#ifndef CONSOLE_TEST_PRINTER_H
#define CONSOLE_TEST_PRINTER_H

#include <gtest/gtest.h>

class ConsoleTestPrinter : public testing::EmptyTestEventListener
{
public:
    void OnTestProgramEnd(const testing::UnitTest& unit_test) override;
};

#endif
