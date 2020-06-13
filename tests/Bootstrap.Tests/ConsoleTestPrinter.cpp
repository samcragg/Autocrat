#include <termcolor/termcolor.hpp>
#include "ConsoleTestPrinter.h"

namespace
{
    void print_test_failure_info(const testing::TestInfo& test_info)
    {
        for (int i = 0; i < test_info.result()->total_part_count(); ++i)
        {
            const testing::TestPartResult& result =
                test_info.result()->GetTestPartResult(i);

            std::cout << result.file_name() << ':' << result.line_number() << std::endl;
            std::cout << result.message() << std::endl;
        }
    }

    void print_failed_tests(const testing::UnitTest& unit_test)
    {
        for (int i = 0; i < unit_test.total_test_case_count(); ++i)
        {
            const testing::TestCase& test_case = *unit_test.GetTestCase(i);
            for (int j = 0; j < test_case.total_test_count(); ++j)
            {
                const testing::TestInfo& test_info = *test_case.GetTestInfo(j);
                if (test_info.result()->Failed())
                {
                    std::cout << "\nFailed: " << test_case.name() << '.' << test_info.name() << std::endl;
                    print_test_failure_info(test_info);
                }
            }
        }
    }

    void print_summary(std::int64_t time, int total, int successful, int failed)
    {
        if (failed > 0)
        {
            std::cout << termcolor::red << "Test Run Failed.\n" << termcolor::reset;
        }
        else
        {
            std::cout << termcolor::green << "Test Run Successful.\n" << termcolor::reset;
        }

        std::cout << "Total tests: " << total << "\n";
        std::cout << termcolor::green << "     Passed: " << successful << "\n" << termcolor::reset;

        if (failed > 0)
        {
            std::cout << termcolor::red << "     Failed: " << failed << "\n" << termcolor::reset;
        }

        std::cout << " Total time: " << (time / 1000) << '.'
            << std::setfill('0') << std::setw(3) << (time % 1000) << " Seconds" << std::endl;
    }
}

void ConsoleTestPrinter::OnTestProgramEnd(const testing::UnitTest& unit_test)
{
    print_summary(
        unit_test.elapsed_time(),
        unit_test.total_test_count(),
        unit_test.successful_test_count(),
        unit_test.failed_test_count());

    print_failed_tests(unit_test);
}
