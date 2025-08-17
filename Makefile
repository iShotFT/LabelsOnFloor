# Makefile for LabelsOnFloor RimWorld Mod
# This Makefile is designed to work on WSL2 Ubuntu with .NET SDK installed on Windows

# Tools configuration
DOTNET = /mnt/c/dotnet/dotnet.exe
POWERSHELL = powershell.exe
BUILD_SCRIPT_WIN = $(shell wslpath -w "$(CURDIR)/build.ps1")
PROJECT_WIN = $(shell wslpath -w "$(CURDIR)/src/LabelsOnFloor/LabelsOnFloor.csproj")

# Build configuration
CONFIGURATION = Release
BUILD_FLAGS = -c $(CONFIGURATION) --no-incremental

# Directories
SRC_DIR = src/LabelsOnFloor
BIN_DIR = $(SRC_DIR)/bin
OBJ_DIR = $(SRC_DIR)/obj
DIST_DIR = dist
THIRD_PARTY_DIR = ThirdParty

# Shell to use for commands
SHELL := /bin/bash

# Colors for output (using printf for better compatibility)
RED = \\033[0;31m
GREEN = \\033[0;32m
YELLOW = \\033[1;33m
NC = \\033[0m # No Color

# Default target (may hang if BitDefender locks files during deployment)
.PHONY: all
all: clean prebuild build postbuild
	@printf "$(GREEN)✓ Full build completed successfully!$(NC)\n"

# Pre-build: Update versions and prepare dependencies
.PHONY: prebuild
prebuild:
	@printf "$(YELLOW)Running pre-build steps...$(NC)\n"
	@printf "$(YELLOW)Generating font system...$(NC)\n"
	@cd /mnt/d/dev/personal/LabelsOnFloor && python3 generate_font_system.py
	@$(POWERSHELL) -EP Unrestricted "$(BUILD_SCRIPT_WIN)" doPreBuild
	@printf "$(GREEN)✓ Pre-build completed$(NC)\n"

# Build: Compile the C# project (Note: auto post-build disabled to prevent BitDefender locks)
.PHONY: build
build:
	@printf "$(YELLOW)Building C# project...$(NC)\n"
	@printf "$(YELLOW)Note: Auto-deployment is disabled. Run 'make postbuild' separately$(NC)\n"
	@$(DOTNET) build "$(PROJECT_WIN)" $(BUILD_FLAGS)
	@printf "$(GREEN)✓ Build succeeded$(NC)\n"

# Post-build: Package and deploy
.PHONY: postbuild
postbuild:
	@printf "$(YELLOW)Running post-build steps...$(NC)\n"
	@$(POWERSHELL) -EP Unrestricted "$(BUILD_SCRIPT_WIN)" doPostBuild
	@printf "$(GREEN)✓ Post-build completed$(NC)\n"

# Clean build artifacts
.PHONY: clean
clean:
	@printf "$(YELLOW)Cleaning build artifacts...$(NC)\n"
	@rm -rf $(BIN_DIR) $(OBJ_DIR) $(DIST_DIR)
	@printf "$(GREEN)✓ Clean completed$(NC)\n"

# Deep clean: Remove everything including dependencies
.PHONY: distclean
distclean: clean
	@printf "$(YELLOW)Removing third-party dependencies...$(NC)\n"
	@rm -f $(THIRD_PARTY_DIR)/*.dll
	@printf "$(GREEN)✓ Deep clean completed$(NC)\n"

# Quick rebuild: Clean and build
.PHONY: rebuild
rebuild: clean all

# Build only the DLL (no packaging)
.PHONY: dll
dll: prebuild build
	@printf "$(GREEN)✓ DLL build completed (no packaging)$(NC)\n"

# Run tests (placeholder for future test implementation)
.PHONY: test
test:
	@printf "$(YELLOW)No tests configured yet$(NC)\n"

# Check if required tools are available
.PHONY: check-tools
check-tools:
	@printf "$(YELLOW)Checking build tools...$(NC)\n"
	@if [ ! -f "$(DOTNET)" ]; then \
		echo -e "$(RED)✗ .NET SDK not found at $(DOTNET)$(NC)\n"; \
		echo "Please install .NET SDK 8.0 on Windows (see SETUP_BUILD_TOOLS.md)"; \
		exit 1; \
	fi
	@$(DOTNET) --version > /dev/null 2>&1 && echo -e "$(GREEN)✓ .NET SDK found: $$($(DOTNET) --version)$(NC)\n"
	@command -v $(POWERSHELL) > /dev/null 2>&1 && echo -e "$(GREEN)✓ PowerShell found$(NC)\n" || echo -e "$(RED)✗ PowerShell not found$(NC)\n"

# Show help
.PHONY: help
help:
	@echo "LabelsOnFloor Build System"
	@echo "=========================="
	@echo ""
	@echo "Available targets:"
	@echo "  make              - Full build (clean + prebuild + build + postbuild)"
	@echo "  make build        - Build C# project only"
	@echo "  make clean        - Remove build artifacts"
	@echo "  make rebuild      - Clean and rebuild everything"
	@echo "  make dll          - Build DLL only (no packaging)"
	@echo "  make check-tools  - Verify required tools are installed"
	@echo "  make help         - Show this help message"
	@echo ""
	@echo "Individual steps:"
	@echo "  make prebuild     - Run pre-build steps (update versions, copy deps)"
	@echo "  make postbuild    - Run post-build steps (package and deploy)"
	@echo "  make distclean    - Deep clean (remove all generated files)"
	@echo ""
	@echo "Requirements:"
	@echo "  - .NET SDK 8.0 installed on Windows at C:\\dotnet"
	@echo "  - PowerShell accessible from WSL"
	@echo "  - Running on WSL2 Ubuntu"

# Install build dependencies (if not present)
.PHONY: deps
deps: check-tools
	@printf "$(YELLOW)Checking dependencies...$(NC)\n"
	@if [ ! -f "$(THIRD_PARTY_DIR)/Assembly-CSharp.dll" ]; then \
		echo -e "$(YELLOW)Dependencies missing, running prebuild to fetch them...$(NC)\n"; \
		$(MAKE) prebuild; \
	else \
		echo -e "$(GREEN)✓ Dependencies already present$(NC)\n"; \
	fi

# Quick build without clean (for development)
.PHONY: quick
quick: deps build postbuild
	@printf "$(GREEN)✓ Quick build completed!$(NC)\n"

# Watch for changes and rebuild (requires inotify-tools)
.PHONY: watch
watch:
	@command -v inotifywait > /dev/null 2>&1 || (echo -e "$(RED)inotify-tools not installed. Install with: sudo apt-get install inotify-tools$(NC)\n" && exit 1)
	@printf "$(YELLOW)Watching for changes in $(SRC_DIR)...$(NC)\n"
	@while true; do \
		inotifywait -r -e modify,create,delete $(SRC_DIR) --exclude '(bin|obj)' 2>/dev/null; \
		$(MAKE) quick; \
	done

# GIF Processing Command
# Usage: make gif-process FILE=input.mp4 [OUTPUT=name] [TEXT="Title"] [PROGRESS=1] [POS=TL] [WIDTH=800] [UPLOAD=1]
.PHONY: gif-process
gif-process:
	@if [ -z "$(FILE)" ]; then \
		echo -e "$(RED)Error: FILE parameter is required$(NC)"; \
		echo "Usage: make gif-process FILE=input.mp4 [OUTPUT=name] [TEXT=\"Title\"] [PROGRESS=1] [POS=TL] [WIDTH=800] [UPLOAD=1]"; \
		exit 1; \
	fi
	@if [ ! -f "$(FILE)" ]; then \
		echo -e "$(RED)Error: File '$(FILE)' not found$(NC)"; \
		exit 1; \
	fi
	@printf "$(YELLOW)Processing GIF from $(FILE)...$(NC)\n"
	@WIDTH=$${WIDTH:-800}; \
	POS=$${POS:-TL}; \
	DIR=$$(dirname "$(FILE)"); \
	BASENAME=$$(basename "$(FILE)" | sed 's/\.[^.]*$$//'); \
	TIMESTAMP=$$(date +%s); \
	OUTPUT_NAME=$${OUTPUT:-"$${BASENAME}_processed"}; \
	OUTPUT_FILE="$$DIR/$${OUTPUT_NAME}_$$TIMESTAMP.gif"; \
	TEMP_FILE="$$DIR/.temp_$${OUTPUT_NAME}_$$TIMESTAMP.gif"; \
	case "$$POS" in \
		TL) X_POS=10; Y_POS=10 ;; \
		TR) X_POS="w-text_w-10"; Y_POS=10 ;; \
		BL) X_POS=10; Y_POS="h-text_h-10" ;; \
		BR) X_POS="w-text_w-10"; Y_POS="h-text_h-10" ;; \
		*) X_POS=10; Y_POS=10 ;; \
	esac; \
	FILTER="fps=15,scale=$$WIDTH:-1:flags=lanczos"; \
	if [ -n "$(PROGRESS)" ]; then \
		FRAMECOUNT=$$(ffprobe -v error -select_streams v:0 -count_packets -show_entries stream=nb_read_packets -of csv=p=0 "$(FILE)" 2>/dev/null || echo "100"); \
		echo "Debug: Frame count = $$FRAMECOUNT"; \
		FILTER="$$FILTER,drawbox=x=0:y=ih-6:w=iw:h=6:color=black@0.3:t=fill,drawbox=x=0:y=ih-6:w='iw*min(1,n/$$FRAMECOUNT)':h=6:color=green@0.9:t=fill"; \
	fi; \
	if [ -n "$(TEXT)" ]; then \
		FILTER="$$FILTER,drawtext=text='$(TEXT)':fontcolor=white:fontsize=18:shadowcolor=black@0.8:shadowx=2:shadowy=2:x=$$X_POS:y=$$Y_POS"; \
	fi; \
	FILTER="$$FILTER,split[s0][s1];[s0]palettegen=max_colors=128:stats_mode=diff[p];[s1][p]paletteuse=dither=bayer:bayer_scale=3"; \
	printf "$(YELLOW)Converting to GIF (15 FPS, width=$$WIDTH)...$(NC)\n"; \
	ffmpeg -i "$(FILE)" -vf "$$FILTER" -loop 0 "$$TEMP_FILE" -y -loglevel error; \
	if [ $$? -ne 0 ]; then \
		echo -e "$(RED)Error: FFmpeg conversion failed$(NC)"; \
		exit 1; \
	fi; \
	if command -v gifsicle > /dev/null 2>&1; then \
		printf "$(YELLOW)Optimizing with gifsicle...$(NC)\n"; \
		gifsicle -O3 --colors 128 --lossy=30 "$$TEMP_FILE" -o "$$OUTPUT_FILE" 2>/dev/null; \
		rm -f "$$TEMP_FILE"; \
	else \
		mv "$$TEMP_FILE" "$$OUTPUT_FILE"; \
	fi; \
	SIZE=$$(du -h "$$OUTPUT_FILE" | cut -f1); \
	printf "$(GREEN)✓ GIF created: $$OUTPUT_FILE ($$SIZE)$(NC)\n"; \
	if [ -n "$(UPLOAD)" ]; then \
		if command -v imgur-uploader > /dev/null 2>&1; then \
			printf "$(YELLOW)Uploading to imgur...$(NC)\n"; \
			IMGUR_URL=$$(imgur-uploader "$$OUTPUT_FILE" 2>/dev/null | grep -oP 'https://i\.imgur\.com/[a-zA-Z0-9]+\.gif'); \
			if [ -n "$$IMGUR_URL" ]; then \
				printf "$(GREEN)✓ Uploaded to: $$IMGUR_URL$(NC)\n"; \
				echo "$$IMGUR_URL" | xclip -selection clipboard 2>/dev/null && \
				printf "$(GREEN)✓ URL copied to clipboard$(NC)\n"; \
			else \
				printf "$(YELLOW)Alternative: Upload manually at https://imgur.com$(NC)\n"; \
			fi; \
		else \
			printf "$(YELLOW)imgur-uploader not found. Install with: npm i -g imgur-uploader-cli$(NC)\n"; \
			printf "$(YELLOW)Manual upload: https://imgur.com$(NC)\n"; \
		fi; \
	fi

# Alternative GIF processing using dedicated script (with working progress bar and keyboard keys)
.PHONY: gif
gif:
	@if [ -z "$(FILE)" ]; then \
		echo -e "$(RED)═══════════════════════════════════════════════════════$(NC)"; \
		echo -e "$(RED)              GIF PROCESSOR - USAGE GUIDE               $(NC)"; \
		echo -e "$(RED)═══════════════════════════════════════════════════════$(NC)"; \
		echo ""; \
		echo -e "$(YELLOW)USAGE:$(NC) make gif FILE=input.gif [OPTIONS]"; \
		echo ""; \
		echo -e "$(YELLOW)REQUIRED:$(NC)"; \
		echo "  FILE=path/to/input.gif     Input GIF or video file"; \
		echo ""; \
		echo -e "$(YELLOW)SIZE & DURATION:$(NC)"; \
		echo "  WIDTH=800                  Output width in pixels"; \
		echo "  LENGTH=10                  Target duration in seconds"; \
		echo ""; \
		echo -e "$(YELLOW)TEXT OVERLAY:$(NC)"; \
		echo "  TEXT=\"Your Text\"           Text to overlay"; \
		echo "  TEXTPOS=TL|TR|BL|BR       Position (TopLeft/Right, BottomLeft/Right)"; \
		echo ""; \
		echo -e "$(YELLOW)PROGRESS BAR:$(NC)"; \
		echo "  PROGRESS=1                 Enable progress bar"; \
		echo "  PROGRESSCOLOR=HEX|avg      Color (hex or 'avg' for auto)"; \
		echo "  PROGRESSBACKGROUNDCOLOR=HEX Background color"; \
		echo "  PROGRESSBACKGROUNDOPACITY=75 Background opacity (0-100)"; \
		echo ""; \
		echo -e "$(YELLOW)KEYBOARD KEY:$(NC)"; \
		echo "  KEY=R                      Key letter to show"; \
		echo "  KEYPOS=TL|TR|BL|BR        Position"; \
		echo "  KEYANIM=press1-5|pulse     Animation style:"; \
		echo "    press1 = Classic depth   press2 = Mechanical"; \
		echo "    press3 = Flat/scale      press4 = Arcade glow"; \
		echo "    press5 = Material ripple pulse = Opacity pulse"; \
		echo ""; \
		echo -e "$(YELLOW)LOGO OVERLAY:$(NC)"; \
		echo "  LOGO=path/to/logo.png      Logo image (supports transparency)"; \
		echo "  LOGOPOS=TL|TR|BL|BR       Position (default: TR)"; \
		echo "  LOGOSIZE=40                Size in pixels (default: 40)"; \
		echo "  LOGOOPACITY=80             Opacity (0-100)"; \
		echo ""; \
		echo -e "$(YELLOW)OTHER:$(NC)"; \
		echo "  PADDING=25                 Edge padding in pixels"; \
		echo "  OUTPUT=name                Output filename prefix"; \
		echo "  UPLOAD=1                   Upload to imgur after processing"; \
		echo ""; \
		echo -e "$(GREEN)EXAMPLES:$(NC)"; \
		echo "  # Basic with progress bar"; \
		echo "  make gif FILE=demo.gif PROGRESS=1"; \
		echo ""; \
		echo "  # Full featured"; \
		echo "  make gif FILE=demo.gif TEXT=\"Settings\" TEXTPOS=BR \\"; \
		echo "    PROGRESS=1 PROGRESSCOLOR=avg KEY=R KEYANIM=press2 \\"; \
		echo "    LOGO=ModIcon.png LOGOSIZE=40 WIDTH=800 LENGTH=5"; \
		echo ""; \
		echo "  # Upload to imgur"; \
		echo "  make gif FILE=demo.gif TEXT=\"Feature Demo\" UPLOAD=1"; \
		echo ""; \
		exit 1; \
	fi
	@ARGS=""; \
	[ -n "$(WIDTH)" ] && ARGS="$$ARGS --width=$(WIDTH)"; \
	[ -n "$(TEXT)" ] && ARGS="$$ARGS --text=\"$(TEXT)\""; \
	[ -n "$(TEXTPOS)" ] && ARGS="$$ARGS --textpos=$(TEXTPOS)"; \
	[ -n "$(PADDING)" ] && ARGS="$$ARGS --padding=$(PADDING)"; \
	[ -n "$(KEY)" ] && ARGS="$$ARGS --key=$(KEY)"; \
	[ -n "$(KEYPOS)" ] && ARGS="$$ARGS --keypos=$(KEYPOS)"; \
	[ -n "$(KEYANIM)" ] && ARGS="$$ARGS --keyanim=$(KEYANIM)"; \
	[ -n "$(LOGO)" ] && ARGS="$$ARGS --logo=\"$(LOGO)\""; \
	[ -n "$(LOGOPOS)" ] && ARGS="$$ARGS --logopos=$(LOGOPOS)"; \
	[ -n "$(LOGOSIZE)" ] && ARGS="$$ARGS --logosize=$(LOGOSIZE)"; \
	[ -n "$(LOGOOPACITY)" ] && ARGS="$$ARGS --logoopacity=$(LOGOOPACITY)"; \
	[ -n "$(PROGRESS)" ] && ARGS="$$ARGS --progress"; \
	[ -n "$(LENGTH)" ] && ARGS="$$ARGS --length=$(LENGTH)"; \
	[ -n "$(PROGRESSCOLOR)" ] && ARGS="$$ARGS --progresscolor=$(PROGRESSCOLOR)"; \
	[ -n "$(PROGRESSBACKGROUNDCOLOR)" ] && ARGS="$$ARGS --progressbackgroundcolor=$(PROGRESSBACKGROUNDCOLOR)"; \
	[ -n "$(PROGRESSBACKGROUNDOPACITY)" ] && ARGS="$$ARGS --progressbackgroundopacity=$(PROGRESSBACKGROUNDOPACITY)"; \
	[ -n "$(OUTPUT)" ] && ARGS="$$ARGS --output=\"$(OUTPUT)\""; \
	[ -n "$(UPLOAD)" ] && ARGS="$$ARGS --upload"; \
	bash -c "./process_gif.sh \"$(FILE)\" $$ARGS"

# GIF batch processing helper
.PHONY: gif-banner
gif-banner:
	@printf "$(YELLOW)Creating standard banner GIF (800x200)...$(NC)\n"
	@$(MAKE) gif-process FILE=$(FILE) WIDTH=800 HEIGHT=200 $(ARGS)

# Help for GIF processing
.PHONY: gif-help
gif-help:
	@echo "GIF Processing Commands"
	@echo "======================"
	@echo ""
	@echo "Process a video/GIF file:"
	@echo "  make gif-process FILE=input.mp4 [OPTIONS]"
	@echo ""
	@echo "Options:"
	@echo "  OUTPUT=name        - Output filename (without extension)"
	@echo "  TEXT=\"Title\"       - Add text overlay"
	@echo "  PROGRESS=1         - Add progress bar"
	@echo "  POS=TL/TR/BL/BR    - Text position (Top/Bottom + Left/Right)"
	@echo "  WIDTH=800          - Target width in pixels (default: 800)"
	@echo "  UPLOAD=1           - Upload to imgur after processing"
	@echo ""
	@echo "Examples:"
	@echo "  make gif-process FILE=demo.mp4"
	@echo "  make gif-process FILE=demo.mp4 TEXT=\"Room Colors\" PROGRESS=1"
	@echo "  make gif-process FILE=demo.mp4 WIDTH=600 UPLOAD=1"
	@echo "  make gif-process FILE=demo.mp4 OUTPUT=banner TEXT=\"Settings\" POS=BR"
	@echo ""
	@echo "Requirements:"
	@echo "  - ffmpeg (required)"
	@echo "  - gifsicle (optional, for optimization)"
	@echo "  - imgur-uploader-cli (optional, for upload)"
	@echo ""
	@echo "Install optional tools:"
	@echo "  sudo apt install gifsicle"
	@echo "  npm install -g imgur-uploader-cli"

# Default target if no target specified
.DEFAULT_GOAL := all