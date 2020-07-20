mode ?= Release
OBJ_DIR = obj/$(mode)
WARNINGFLAGS = -Werror -Wall -Wextra -Wpedantic

CXXFLAGS = $(WARNINGFLAGS) -pthread -lrt -m64 -std=c++17
ifeq ($(mode), Debug)
	CXXFLAGS += -g
else
	CXXFLAGS += -O2 -DNDEBUG
endif

define build_object
	@mkdir -p $(dir $(2))
	$(CXX) $(1) -c -o $(2) $(CXXFLAGS)
endef
