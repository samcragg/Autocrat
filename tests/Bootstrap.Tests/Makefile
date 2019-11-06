include ../../src/Autocrat.Bootstrap/variables.mk

SRC_DIR = ../../src/Autocrat.Bootstrap/src
SRCS = \
 $(SRC_DIR)/array_pool.cpp

TESTS = \
 tests/ArrayPoolTests.cpp \
 tests/ManagedArrayTests.cpp \
 main.cpp

TEST_PROGRAM = bin/$(mode)/Bootstrap.Tests
TEST_OUTPUT = test_results.xml
test: $(TEST_PROGRAM)
	$(TEST_PROGRAM) --gtest_output="xml:$(TEST_OUTPUT)"

GTEST_OBJ = $(OBJ_DIR)/libs/gmock-gtest-all.o
$(GTEST_OBJ): ../../libs/gmock-gtest-all.cc
	$(call build_object,$<,$@)

SRC_OBJS = $(SRCS:$(SRC_DIR)/%.cpp=$(OBJ_DIR)/src/%.o)
$(OBJ_DIR)/src/%.o: $(SRC_DIR)/%.cpp
	$(call build_object,$<,$@)

TESTS_OBJS = $(TESTS:%.cpp=$(OBJ_DIR)/%.o)
$(OBJ_DIR)/%.o: %.cpp
	$(call build_object,$<,$@)

OBJECTS = $(GTEST_OBJ) $(SRC_OBJS) $(TESTS_OBJS)
$(TEST_PROGRAM): $(OBJECTS)
	@mkdir -p $(@D)
	g++ $(OBJECTS) $(CXXFLAGS) -o $(TEST_PROGRAM)

.PHONY: clean
clean:
	@rm -rf $(OBJ_DIR)
	@rm -f $(TEST_PROGRAM)
	@rm -f $(TEST_OUTPUT)
