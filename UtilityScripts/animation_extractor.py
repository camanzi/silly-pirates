import os
import sys
from PIL import Image

def split_sprites(input_path):
    # Lista delle direzioni nell'ordine fornito
    directions = ["N", "NE", "E", "SE", "S", "SW", "W", "NW"]
    
    # Pulizia del path
    input_path = input_path.strip('"').strip("'")

    if not os.path.exists(input_path):
        print(f"Errore: Il file '{input_path}' non esiste.")
        return

    try:
        with Image.open(input_path) as img:
            img = img.convert("RGBA")
            width, height = img.size
            row_height = 32
            
            # Ricaviamo il percorso e il nome base (es. "eroe_camminata")
            file_dir = os.path.dirname(os.path.abspath(input_path))
            base_name = os.path.splitext(os.path.basename(input_path))[0]
            
            # Creiamo una sottocartella per non intasare la directory principale
            output_folder = os.path.join(file_dir, f"{base_name}_split")
            
            if not os.path.exists(output_folder):
                os.makedirs(output_folder)

            print(f"--- Elaborazione: {base_name} ---")

            for i, dir_name in enumerate(directions):
                upper = i * row_height
                lower = upper + row_height
                
                if upper >= height:
                    print(f"! Nota: Fine immagine raggiunta alla riga {i} ({dir_name} saltata)")
                    break
                
                # Ritaglio della riga
                crop_box = (0, upper, width, lower)
                sprite_row = img.crop(crop_box)
                
                # Nomenclatura dinamica: NomeOriginale_Direzione.png
                output_file_name = f"{base_name}_{dir_name}.png"
                output_path = os.path.join(output_folder, output_file_name)
                
                sprite_row.save(output_path)
                print(f"Esportato: {output_file_name}")

            print(f"\nOperazione completata con successo!")
            print(f"File salvati in: {output_folder}")

    except Exception as e:
        print(f"Errore durante l'elaborazione: {e}")

if __name__ == "__main__":
    if len(sys.argv) > 1:
        split_sprites(sys.argv[1])
    else:
        print("Utilizzo: python nome_script.py <percorso_immagine>")