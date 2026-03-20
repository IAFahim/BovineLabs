#!/bin/bash

# Workaround 1: Force X11/XWayland (prevents Wayland-specific input/rendering bugs)
export WAYLAND_DISPLAY=""

# Workaround 2: Disable Global Menu (prevents freezes when interacting with the top bar)
export UNITY_DONT_USE_APP_MENU=1

# Workaround 3: Graphics API selection
# Defaulting to Vulkan as it's modern, but you can change this to -force-opengl if freezes persist.
GRAPHICS_API="-force-vulkan"

# Locate the Unity Editor binary
UNITY_PATH="/home/i/Unity/Hub/Editor/6000.5.0a8/Editor/Unity"
PROJECT_PATH="/home/i/Github/BovineLabs"

echo "Launching Unity with stability workarounds..."
echo "Graphics API: $GRAPHICS_API"

# Launch Unity with all arguments passed to this script
exec "$UNITY_PATH" -projectpath "$PROJECT_PATH" "$GRAPHICS_API" "$@"
