import os
import glob
import zipfile

release_folder = "./Release/"

def clear_release():
    for file in glob.glob(release_folder + "**"):
        os.remove(file)

clear_release()

if not os.path.exists("Release"):
    os.mkdir('Release')
   
with zipfile.ZipFile('FortnitePorting/Plugin/FortnitePortingBlender.zip', 'w', zipfile.ZIP_DEFLATED) as blender_zip:
    blender_zip.write("Plugins/Blender/FortnitePortingServer.py", "FortnitePortingServer.py")
    blender_zip.write("Plugins/Blender/io_import_scene_unreal_psa_psk_280.py", "io_import_scene_unreal_psa_psk_280.py")
    blender_zip.write("Plugins/Blender/FortnitePortingData.blend", "FortnitePortingData.blend")
    
os.system(f'dotnet publish -r win-x64 -o {release_folder} -c Release /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:DebugType=None /p:DebugSymbols=false --self-contained false')

os.startfile(os.path.realpath(release_folder))
    