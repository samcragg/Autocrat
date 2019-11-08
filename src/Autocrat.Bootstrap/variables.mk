mode ?= Release
OBJ_DIR = obj/$(mode)
WARNINGFLAGS = -Werror -Wall -Wextra -Wpedantic

CXXFLAGS = $(WARNINGFLAGS) -pthread -march=x86-64 -std=c++17
ifeq ($(mode), Debug)
	CXXFLAGS += -g
else
	CXXFLAGS += -O2
endif

define build_object
	@mkdir -p $(dir $(2))
	g++ $(1) -c -o $(2) $(CXXFLAGS)
endef
