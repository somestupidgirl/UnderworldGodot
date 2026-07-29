PROJECT_DIR := $(CURDIR)
GODOT := /Applications/Godot_mono.app/Contents/MacOS/Godot
EXPORT_PRESET := UnderworldMacOS
EXPORT_PATH := $(PROJECT_DIR)/Builds/macOS/Underworld.app
APP_BUNDLE := $(EXPORT_PATH)

.PHONY: all build clean

all: build

build:
	@mkdir -p $(PROJECT_DIR)/Builds/macOS
	@$(GODOT) --headless --path $(PROJECT_DIR) --export-release "$(EXPORT_PRESET)" "$(EXPORT_PATH)"
	@mkdir -p $(APP_BUNDLE)/Contents/Resources/UWDATA
	@cp -R $(PROJECT_DIR)/UWDATA/UW1 $(APP_BUNDLE)/Contents/Resources/UWDATA/
	@cp -R $(PROJECT_DIR)/UWDATA/UW2 $(APP_BUNDLE)/Contents/Resources/UWDATA/

clean:
	@rm -rf $(PROJECT_DIR)/Builds .godot
