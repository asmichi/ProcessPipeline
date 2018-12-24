#
# Variables
#

ifeq ($(CONFIGURATION),)
CONFIGURATION = Release
endif

LIBNAME = AsmichiProcessPipeline
AGENTNAME = Asmichi.ProcessPipeline.Agent
SRCDIR = ./
OBJDIR = ../../obj/ProcessPipeline.Native/$(CONFIGURATION)/
BINDIR = ../../bin/ProcessPipeline.Native/$(CONFIGURATION)/

SRCS_COMMON = $(addprefix $(SRCDIR),common.c)
SRCS_LIB = $(SRCS_COMMON) $(addprefix $(SRCDIR),lib.c)
SRCS_AGENT = $(SRCS_COMMON) $(addprefix $(SRCDIR),agent.c)
OBJS_LIB = $(addprefix $(OBJDIR),$(SRCS_LIB:.c=.o))
OBJS_AGENT = $(addprefix $(OBJDIR),$(SRCS_AGENT:.c=.o))
CC = gcc

CFLAGS = -g -gdwarf -MMD -std=c11 -fPIC -Wextra -Wswitch -Wswitch -Werror
LDFLAGS_COMMON = -Wl,--no-undefined -Wl,-z,relro -Wl,-z,now -Wl,-z,noexecstack
LDFLAGS = $(LDFLAGS_COMMON)
LDFLAGS_SHARED = $(LDFLAGS_COMMON) -shared -Wl,-soname,lib$(LIBNAME).so -Wl,--version-script=$(SRCDIR)$(LIBNAME).version

ifeq ($(CONFIGURATION),Release)
CFLAGS += -O3
else ifeq ($(CONFIGURATION),Debug)
CFLAGS += -O0 -fno-omit-frame-pointer
else
$(error Unknown CONFIGURATION: $(CONFIGURATION))
endif

#
# Targets
#
.PHONY : all
all: $(BINDIR)lib$(LIBNAME).so $(BINDIR)$(AGENTNAME)

-include $(OBJS_LIB:.o=.d)
-include $(OBJS_AGENT:.o=.d)

$(OBJDIR)%.o : $(SRCDIR)%.c
	mkdir -p $(dir $@)
	$(CC) $(CFLAGS) -o $@ -c $<

$(BINDIR)lib$(LIBNAME).so: $(OBJS_LIB) $(SRCDIR)$(LIBNAME).version
	mkdir -p $(dir $@)
	$(CC) $(LDFLAGS_SHARED) -o $@ $(OBJS_LIB)

$(BINDIR)$(AGENTNAME) : $(OBJS_AGENT)
	mkdir -p $(dir $@)
	$(CC) $(LDFLAGS) -o $@ $(OBJS_AGENT)
