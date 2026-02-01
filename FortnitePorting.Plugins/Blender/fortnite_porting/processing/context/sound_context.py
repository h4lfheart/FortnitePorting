
import os.path
import bpy
from ..enums import *
from ..utils import *

class SoundImportContext:
    
    def import_sound_data(self, data):
        for sound in data.get("Sounds"):
            path = sound.get("Path")
            self.import_sound(path, time_to_frame(sound.get("Time")))

    # TODO fix usage with sequencer for blender 5.0
    def import_sound(self, path: str, time):
        path = path[1:] if path.startswith("/") else path
        file_path, name = path.split(".")
        if existing := bpy.data.sounds.get(name):
            return existing

        ext = ESoundFormat(self.options.get("SoundFormat")).name.lower()
        sound_path = os.path.join(self.assets_root, f"{file_path}.{ext}")
        
        if sequence_editor := get_sequence_editor():
            sound = sequence_editor.strips.new_sound(name, sound_path, 0, time)
            sound["FPSound"] = True
            return sound
            
        return None