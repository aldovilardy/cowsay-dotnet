# ============================================================================
# Makefile — Build .pkg.tar.zst (Arch Linux) packages for cowsay-dotnet
#
# Prerequisites:
#   - makepkg (from pacman)
#   - Pre-built binaries from build/publish.ps1 in build/output/
#
# Usage:
#   make all          # Build for all architectures
#   make linux-x64    # Build for x86_64 only
#   make linux-arm    # Build for armv7h only
#   make linux-arm64  # Build for aarch64 only
#   make clean        # Remove build artifacts
# ============================================================================

SHELL := /bin/bash
SCRIPT_DIR := $(dir $(abspath $(lastword $(MAKEFILE_LIST))))
REPO_ROOT := $(abspath $(SCRIPT_DIR)/../../..)

.PHONY: all linux-x64 linux-arm linux-arm64 clean

all: linux-x64 linux-arm linux-arm64

linux-x64:
	@echo "=== Building .pkg.tar.zst for linux-x64 (x86_64) ==="
	@mkdir -p "$(SCRIPT_DIR)/linux-x64"
	@cd "$(SCRIPT_DIR)" && CARCH=x86_64 makepkg -f --nodeps --noextract 2>&1
	@mv -f "$(SCRIPT_DIR)"/*.pkg.tar.zst "$(SCRIPT_DIR)/linux-x64/" 2>/dev/null || true
	@echo "  -> $(SCRIPT_DIR)/linux-x64/"

linux-arm:
	@echo "=== Building .pkg.tar.zst for linux-arm (armv7h) ==="
	@mkdir -p "$(SCRIPT_DIR)/linux-arm"
	@cd "$(SCRIPT_DIR)" && CARCH=armv7h makepkg -f --nodeps --noextract 2>&1
	@mv -f "$(SCRIPT_DIR)"/*.pkg.tar.zst "$(SCRIPT_DIR)/linux-arm/" 2>/dev/null || true
	@echo "  -> $(SCRIPT_DIR)/linux-arm/"

linux-arm64:
	@echo "=== Building .pkg.tar.zst for linux-arm64 (aarch64) ==="
	@mkdir -p "$(SCRIPT_DIR)/linux-arm64"
	@cd "$(SCRIPT_DIR)" && CARCH=aarch64 makepkg -f --nodeps --noextract 2>&1
	@mv -f "$(SCRIPT_DIR)"/*.pkg.tar.zst "$(SCRIPT_DIR)/linux-arm64/" 2>/dev/null || true
	@echo "  -> $(SCRIPT_DIR)/linux-arm64/"

clean:
	@echo "Cleaning build artifacts..."
	@rm -rf "$(SCRIPT_DIR)/linux-x64" "$(SCRIPT_DIR)/linux-arm" "$(SCRIPT_DIR)/linux-arm64"
	@rm -rf "$(SCRIPT_DIR)/pkg" "$(SCRIPT_DIR)/src"
	@rm -f "$(SCRIPT_DIR)"/*.pkg.tar.zst
	@echo "Done."
