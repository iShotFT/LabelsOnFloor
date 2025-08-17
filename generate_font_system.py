#!/usr/bin/env python3
"""
Generate complete font system for LabelsOnFloor mod
Creates Font.png, Atlas.json, and Preview.png for each font
"""

from PIL import Image, ImageDraw, ImageFont
import os
import sys
import json
import glob
import shutil
from pathlib import Path

# Configuration
CHARS_PER_ROW = 16
TOTAL_ROWS = 16
CHAR_WIDTH = 70  # Doubled from 35 for higher resolution
CHAR_HEIGHT = 128  # Doubled from 64 for higher resolution
FONT_SIZE = 92   # Doubled from 46 to match resolution increase
OUTPUT_DIR = "mod-structure/Textures/Fonts"

# Target fonts to generate (add more fonts here as needed)
TARGET_FONTS = [
    # (font_filename, output_name)
    ("JetBrainsMono-Regular.ttf", "JetBrainsMono"),
    ("JetBrainsMono-Bold.ttf", "JetBrainsMonoBold"),
    ("JetBrainsMono-Light.ttf", "JetBrainsMonoLight"),
    ("F25_BlackletterTypewriter.ttf", "Medieval"),  # Medieval style font
    # Add more fonts here when needed, e.g.:
    # ("RobotoMono-Regular.ttf", "RobotoMono"),
    # ("FiraMono-Regular.ttf", "FiraMono"),
]

# Preview configuration - increased size for better readability
PREVIEW_HEIGHT = 48  # Doubled height for better visibility
PREVIEW_FONT_SIZE = 32  # Doubled font size for clarity
PREVIEW_PADDING = 8  # Increased padding

# Character mapping configuration
TOTAL_CHARS = 256  # 16x16 grid

def get_character_for_index(index):
    """Get the character that should be at this grid position"""
    if index == 0:
        return ' '  # Space at position 0
    
    # Basic ASCII (1-94) -> characters 33-126
    if index < 95:
        return chr(32 + index)
    
    # Latin-1 Supplement (95-190) -> characters 160-255
    if index < 191:
        return chr(160 + (index - 95))
    
    # Cyrillic (191-254) -> characters 1040-1103 (А-я)
    # This covers Russian alphabet (uppercase and lowercase)
    # 1040-1071 = А-Я (uppercase), 1072-1103 = а-я (lowercase)
    if index < 255:
        cyrillic_start = 1040  # Cyrillic 'А'
        return chr(cyrillic_start + (index - 191))
    
    # Last position (255) - special character or fallback
    return '?'

def get_index_for_character(char):
    """Get the grid index for a character"""
    char_code = ord(char)
    
    # Space
    if char_code == 32:
        return 0
    
    # Basic ASCII (33-126)
    if 33 <= char_code <= 126:
        return char_code - 32
    
    # Latin-1 Supplement (160-255)
    if 160 <= char_code <= 255:
        return 95 + (char_code - 160)
    
    # Cyrillic (1040-1103) - Russian alphabet
    if 1040 <= char_code <= 1103:
        return 191 + (char_code - 1040)
    
    # Character not in our mapping
    return -1

def load_font(font_path, size):
    """Load a font with fallback"""
    try:
        return ImageFont.truetype(font_path, size)
    except:
        print(f"Warning: Could not load font {font_path}, using default")
        return ImageFont.load_default()

def generate_font_texture(font_path, font_name, output_path):
    """Generate the main font texture atlas"""
    texture_width = CHAR_WIDTH * CHARS_PER_ROW
    texture_height = CHAR_HEIGHT * TOTAL_ROWS
    
    # Create a new image with transparent background
    img = Image.new('RGBA', (texture_width, texture_height), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    # Load font
    font = load_font(font_path, FONT_SIZE)
    
    # Track character support
    supported_chars = {}
    
    # Generate each character
    for row in range(TOTAL_ROWS):
        for col in range(CHARS_PER_ROW):
            index = row * CHARS_PER_ROW + col
            char = get_character_for_index(index)
            
            # Calculate position in texture
            x = col * CHAR_WIDTH
            y = row * CHAR_HEIGHT
            
            # Get character dimensions and metrics
            bbox = draw.textbbox((0, 0), char, font=font)
            char_w = bbox[2] - bbox[0]
            char_h = bbox[3] - bbox[1]
            
            # Center character horizontally
            char_x = x + (CHAR_WIDTH - char_w) // 2
            
            # Proper baseline positioning with padding to prevent clipping:
            # Characters should be centered vertically in their cell
            # Add 4px padding top and bottom to prevent clipping (doubled for resolution)
            padding = 4  # Doubled from 2px
            available_height = CHAR_HEIGHT - (padding * 2)
            char_y = y + padding + (available_height - char_h) // 2
            
            # Account for font metrics offset (bbox can have negative y)
            if bbox[1] < 0:
                char_y -= bbox[1]  # Compensate for ascender offset
            
            # Test if character is actually supported by rendering it and checking for pixels
            # This is the most reliable way - if nothing is drawn, the font doesn't support it
            try:
                # Create a small test image just for this character
                test_img = Image.new('RGBA', (CHAR_WIDTH, CHAR_HEIGHT), (0, 0, 0, 0))
                test_draw = ImageDraw.Draw(test_img)
                
                # Get character bounding box for centering
                bbox = test_draw.textbbox((0, 0), char, font=font)
                test_char_w = bbox[2] - bbox[0]
                test_char_h = bbox[3] - bbox[1]
                
                # Center the character in test image
                test_x = (CHAR_WIDTH - test_char_w) // 2
                test_y = (CHAR_HEIGHT - test_char_h) // 2
                if bbox[1] < 0:
                    test_y -= bbox[1]
                
                # Draw the character
                test_draw.text((test_x, test_y), char, fill=(255, 255, 255, 255), font=font)
                
                # Check if any pixels were actually drawn
                # Get the alpha channel (transparency) to see if anything was rendered
                pixels = test_img.getdata(3)  # Get alpha channel
                has_pixels = any(p > 0 for p in pixels)
                
                if has_pixels:
                    # Character is supported - draw it in the main texture
                    draw.text((char_x, char_y), char, fill=(255, 255, 255, 255), font=font)
                    supported_chars[char] = True
                else:
                    # No pixels drawn - character not supported
                    supported_chars[char] = False
            except Exception as e:
                # Error rendering character
                supported_chars[char] = False
    
    # Save the texture
    img.save(output_path, 'PNG')
    print(f"  Generated Font.png ({texture_width}x{texture_height} pixels)")
    
    return supported_chars

def detect_language_support(supported_chars):
    """Detect which language/script systems are supported by checking character ranges"""
    support = {
        "latin": False,        # Basic Latin (ASCII)
        "latinExtended": False, # Latin with accents/diacritics
        "cyrillic": False,    # Russian, etc.
        "greek": False,       # Greek
        "chinese": False,     # Chinese (would need extended implementation)
        "japanese": False,    # Japanese (would need extended implementation)
        "korean": False,      # Korean (would need extended implementation)
        "arabic": False,      # Arabic (would need extended implementation)
    }
    
    # Check Latin support (ASCII letters)
    latin_chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"
    if all(supported_chars.get(c, False) for c in latin_chars):
        support["latin"] = True
    
    # Check Latin Extended (accents) - require at least 10 accent characters
    accent_chars = "àáâãäåèéêëìíîïòóôõöùúûüýÿÀÁÂÃÄÅÈÉÊËÌÍÎÏÒÓÔÕÖÙÚÛÜÝŸñÑçÇæÆœŒ"
    accent_count = sum(1 for c in accent_chars if supported_chars.get(c, False))
    if accent_count >= 10:  # Need significant accent support, not just one or two
        support["latinExtended"] = True
    
    # Check Cyrillic support - require at least 20 Cyrillic characters
    cyrillic_chars = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя"
    cyrillic_count = sum(1 for c in cyrillic_chars if supported_chars.get(c, False))
    if cyrillic_count >= 20:  # Need significant Cyrillic support
        support["cyrillic"] = True
    
    # Check Greek support
    greek_chars = "ΑΒΓΔΕΖΗΘΙΚΛΜΝΞΟΠΡΣΤΥΦΧΨΩαβγδεζηθικλμνξοπρστυφχψω"
    if any(supported_chars.get(c, False) for c in greek_chars):
        support["greek"] = True
    
    return support

def generate_atlas_json(font_name, supported_chars, output_path):
    """Generate the Atlas.json file with character mappings and UV coordinates"""
    # Detect language support
    language_support = detect_language_support(supported_chars)
    
    atlas = {
        "fontName": font_name,
        "textureWidth": CHAR_WIDTH * CHARS_PER_ROW,
        "textureHeight": CHAR_HEIGHT * TOTAL_ROWS,
        "charWidth": CHAR_WIDTH,
        "charHeight": CHAR_HEIGHT,
        "charsPerRow": CHARS_PER_ROW,
        "totalRows": TOTAL_ROWS,
        "languageSupport": language_support,
        "metadata": {
            "hasLatinSupport": language_support["latin"],
            "hasAccentSupport": language_support["latinExtended"],
            "hasCyrillicSupport": language_support["cyrillic"],
            "hasGreekSupport": language_support["greek"],
            "totalSupportedCharacters": sum(1 for v in supported_chars.values() if v)
        },
        "characters": {}
    }
    
    # Generate character entries
    for index in range(TOTAL_CHARS):
        char = get_character_for_index(index)
        char_code = ord(char)
        
        # Skip if character is not supported
        if char not in supported_chars or not supported_chars[char]:
            continue
        
        # Calculate grid position
        grid_x = index % CHARS_PER_ROW
        grid_y = index // CHARS_PER_ROW
        
        # Calculate UV coordinates (0-1 range)
        uv_left = grid_x / CHARS_PER_ROW
        uv_right = (grid_x + 1) / CHARS_PER_ROW
        uv_top = grid_y / TOTAL_ROWS
        uv_bottom = (grid_y + 1) / TOTAL_ROWS
        
        atlas["characters"][str(char_code)] = {
            "char": char,
            "index": index,
            "gridX": grid_x,
            "gridY": grid_y,
            "uvLeft": uv_left,
            "uvRight": uv_right,
            "uvTop": uv_top,
            "uvBottom": uv_bottom,
            "supported": True
        }
    
    # Save the atlas
    with open(output_path, 'w', encoding='utf-8') as f:
        json.dump(atlas, f, indent=2, ensure_ascii=False)
    
    print(f"  Generated Atlas.json with {len(atlas['characters'])} character mappings")

def generate_preview_image(font_path, font_name, output_path):
    """Generate a preview image showing the font name in that font"""
    # Use fixed font size for consistency across all fonts
    preview_font = load_font(font_path, PREVIEW_FONT_SIZE)
    
    # Convert font name to ALL CAPS to match in-game label display
    display_text = font_name.upper()
    
    # Measure text
    temp_img = Image.new('RGBA', (1, 1), (0, 0, 0, 0))
    temp_draw = ImageDraw.Draw(temp_img)
    bbox = temp_draw.textbbox((0, 0), display_text, font=preview_font)
    text_width = bbox[2] - bbox[0]
    text_height = bbox[3] - bbox[1]
    
    # Create preview image with fixed height and variable width
    preview_width = text_width + PREVIEW_PADDING * 2
    preview_height = PREVIEW_HEIGHT  # Fixed height for all previews
    
    img = Image.new('RGBA', (preview_width, preview_height), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    # Center text vertically (important for consistency)
    text_x = PREVIEW_PADDING
    # Calculate vertical center based on the actual text bounds
    text_y = (preview_height - text_height) // 2 - bbox[1]  # Subtract bbox[1] to account for font baseline
    
    # Draw the font name in white (using the uppercase display_text)
    draw.text((text_x, text_y), display_text, fill=(255, 255, 255, 255), font=preview_font)
    
    # Save the preview
    img.save(output_path, 'PNG')
    print(f"  Generated Preview.png ({preview_width}x{preview_height} pixels)")

def process_font(font_path, font_name=None):
    """Process a single font file"""
    if not os.path.exists(font_path):
        print(f"Error: Font file {font_path} not found")
        return False
    
    # Extract font name from filename if not provided
    if font_name is None:
        font_filename = os.path.basename(font_path)
        font_name = os.path.splitext(font_filename)[0]
        # Clean up common suffixes
        for suffix in ['-Regular', '-Bold', '-Medium', '-Light', 'NL-Regular']:
            if font_name.endswith(suffix):
                font_name = font_name[:-len(suffix)]
    
    print(f"\nProcessing font: {font_name}")
    print(f"  Source: {font_path}")
    
    # Create output directory
    font_output_dir = os.path.join(OUTPUT_DIR, font_name)
    os.makedirs(font_output_dir, exist_ok=True)
    
    # Generate Font.png
    font_texture_path = os.path.join(font_output_dir, "Font.png")
    supported_chars = generate_font_texture(font_path, font_name, font_texture_path)
    
    # Generate Atlas.json
    atlas_path = os.path.join(font_output_dir, "Atlas.json")
    generate_atlas_json(font_name, supported_chars, atlas_path)
    
    # Generate Preview.png
    preview_path = os.path.join(font_output_dir, "Preview.png")
    generate_preview_image(font_path, font_name, preview_path)
    
    print(f"  Font {font_name} processed successfully!")
    return True

def process_target_fonts(fonts_dir):
    """Process only the target fonts specified in TARGET_FONTS list"""
    successful = 0
    failed = []
    
    # Clean up old font directories first
    print("Cleaning up old font directories...")
    fonts_output_dir = os.path.join(OUTPUT_DIR)
    if os.path.exists(fonts_output_dir):
        for item in os.listdir(fonts_output_dir):
            item_path = os.path.join(fonts_output_dir, item)
            if os.path.isdir(item_path):
                # Only remove directories that will be regenerated
                font_names = [name for _, name in TARGET_FONTS]
                if any(item.startswith(name) for name in font_names):
                    print(f"  Removing old generation: {item}")
                    shutil.rmtree(item_path)
    
    print(f"\nProcessing {len(TARGET_FONTS)} target fonts...\n")
    
    for font_filename, output_name in TARGET_FONTS:
        # Try to find the font file in various locations
        font_paths = [
            os.path.join(fonts_dir, font_filename),
            os.path.join(fonts_dir, 'ttf', font_filename),
            os.path.join(fonts_dir, 'otf', font_filename),
            os.path.join('.', font_filename),
            font_filename,  # Try direct path
        ]
        
        font_found = False
        for font_path in font_paths:
            if os.path.exists(font_path):
                if process_font(font_path, output_name):
                    successful += 1
                else:
                    failed.append((font_filename, output_name))
                font_found = True
                break
        
        if not font_found:
            print(f"Warning: Font file '{font_filename}' not found")
            failed.append((font_filename, output_name))
    
    print(f"\nProcessed {successful}/{len(TARGET_FONTS)} fonts successfully")
    if failed:
        print("Failed to process:")
        for font_file, name in failed:
            print(f"  - {name} ({font_file})")

def migrate_existing_fonts():
    """Migrate existing font textures to new structure"""
    existing_fonts = {
        "Consolas": "mod-structure/Textures/Consolas.png",
        "ConsolasExtended": "mod-structure/Textures/ConsolasExtended.png",
        "JetBrainsMono": "mod-structure/Textures/JetBrainsMono.png"
    }
    
    print("\nMigrating existing font textures...")
    
    for font_name, texture_path in existing_fonts.items():
        if not os.path.exists(texture_path):
            continue
        
        # Create font directory
        font_output_dir = os.path.join(OUTPUT_DIR, font_name)
        os.makedirs(font_output_dir, exist_ok=True)
        
        # Copy texture as Font.png
        dest_path = os.path.join(font_output_dir, "Font.png")
        shutil.copy2(texture_path, dest_path)
        print(f"  Migrated {font_name}")
        
        # Generate Atlas.json for existing texture
        # We'll assume all characters are supported for existing textures
        supported_chars = {get_character_for_index(i): True for i in range(256)}
        atlas_path = os.path.join(font_output_dir, "Atlas.json")
        generate_atlas_json(font_name, supported_chars, atlas_path)
        
        # For preview, we'll need to generate it with a default font
        # since we don't have the original font file
        preview_path = os.path.join(font_output_dir, "Preview.png")
        # Try to find a matching font file
        font_file = None
        if font_name == "JetBrainsMono":
            font_file = "JetBrainsMono-Regular.ttf"
        
        if font_file and os.path.exists(font_file):
            generate_preview_image(font_file, font_name, preview_path)
        else:
            # Create a simple text preview
            img = Image.new('RGBA', (150, PREVIEW_HEIGHT), (0, 0, 0, 0))
            draw = ImageDraw.Draw(img)
            draw.text((8, 8), font_name, fill=(255, 255, 255, 255))
            img.save(preview_path, 'PNG')
            print(f"    Generated simple preview for {font_name}")

def main():
    import argparse
    
    global FONT_SIZE  # Declare global at the start of function
    
    parser = argparse.ArgumentParser(description="Generate complete font system for LabelsOnFloor")
    parser.add_argument("--fonts-dir", default="fonts", help="Directory containing font files")
    parser.add_argument("--font", help="Process a specific font file")
    parser.add_argument("--font-name", help="Override font name")
    parser.add_argument("--font-size", type=int, default=FONT_SIZE, help="Font size for texture generation")
    parser.add_argument("--migrate", action="store_true", help="Migrate existing font textures")
    parser.add_argument("--clean", action="store_true", help="Clean output directory before generating")
    parser.add_argument("--all", action="store_true", help="Process ALL fonts in directory (override target list)")
    
    args = parser.parse_args()
    
    # Update font size if specified
    if args.font_size:
        FONT_SIZE = args.font_size
    
    print("LabelsOnFloor Font System Generator")
    print("====================================")
    
    # Clean output directory if requested
    if args.clean and os.path.exists(OUTPUT_DIR):
        print(f"Cleaning {OUTPUT_DIR}...")
        shutil.rmtree(OUTPUT_DIR)
    
    # Create output directory
    os.makedirs(OUTPUT_DIR, exist_ok=True)
    
    # Migrate existing fonts if requested
    if args.migrate:
        migrate_existing_fonts()
    
    # Process specific font or directory
    if args.font:
        process_font(args.font, args.font_name)
    elif args.all:
        # Process ALL fonts in directory (old behavior)
        print("Processing ALL fonts in directory...")
        process_all_fonts_in_directory(args.fonts_dir)
    else:
        # Process only target fonts (default behavior)
        process_target_fonts(args.fonts_dir)
    
    print("\nDone! Font system generated successfully.")
    print(f"Output directory: {OUTPUT_DIR}")
    print("\nTo add more fonts:")
    print("1. Add font files to the 'fonts' directory")
    print("2. Update TARGET_FONTS list in this script")
    print("3. Run: python3 generate_font_system.py")

def process_all_fonts_in_directory(fonts_dir):
    """Process ALL font files in a directory (old behavior for --all flag)"""
    # Look for TTF and OTF files
    font_patterns = ['*.ttf', '*.otf', '*.TTF', '*.OTF']
    font_files = []
    
    for pattern in font_patterns:
        font_files.extend(glob.glob(os.path.join(fonts_dir, pattern)))
        font_files.extend(glob.glob(os.path.join(fonts_dir, '*', pattern)))  # Check subdirectories
    
    if not font_files:
        print(f"No font files found in {fonts_dir}")
        return
    
    print(f"Found {len(font_files)} font files to process")
    
    # Process each font
    successful = 0
    for font_path in font_files:
        if process_font(font_path):
            successful += 1
    
    print(f"\nProcessed {successful}/{len(font_files)} fonts successfully")

if __name__ == "__main__":
    main()