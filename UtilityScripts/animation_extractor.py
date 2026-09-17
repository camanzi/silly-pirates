import os
import sys
from PIL import Image

def split_sprites(input_path):
    # The directions, in the order they appear in the sheet
    directions = ["N", "NE", "E", "SE", "S", "SW", "W", "NW"]
    
    # Clean up the path
    input_path = input_path.strip('"').strip("'")

    if not os.path.exists(input_path):
        print(f"Error: the file '{input_path}' does not exist.")
        return

    try:
        with Image.open(input_path) as img:
            img = img.convert("RGBA")
            width, height = img.size
            row_height = 32
            
            # Derive the directory and the base name (e.g. "hero_walk")
            file_dir = os.path.dirname(os.path.abspath(input_path))
            base_name = os.path.splitext(os.path.basename(input_path))[0]
            
            # Create a subfolder so the main directory does not get cluttered
            output_folder = os.path.join(file_dir, f"{base_name}_split")
            
            if not os.path.exists(output_folder):
                os.makedirs(output_folder)

            print(f"--- Processing: {base_name} ---")

            for i, dir_name in enumerate(directions):
                upper = i * row_height
                lower = upper + row_height
                
                if upper >= height:
                    print(f"! Note: end of image reached at row {i} ({dir_name} skipped)")
                    break
                
                # Crop the row
                crop_box = (0, upper, width, lower)
                sprite_row = img.crop(crop_box)
                
                # Dynamic naming: OriginalName_Direction.png
                output_file_name = f"{base_name}_{dir_name}.png"
                output_path = os.path.join(output_folder, output_file_name)
                
                sprite_row.save(output_path)
                print(f"Exported: {output_file_name}")

            print(f"\nDone!")
            print(f"Files saved in: {output_folder}")

    except Exception as e:
        print(f"Error while processing: {e}")

if __name__ == "__main__":
    if len(sys.argv) > 1:
        split_sprites(sys.argv[1])
    else:
        print("Usage: python animation_extractor.py <image_path>")