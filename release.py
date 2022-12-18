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
    
os.system(f'dotnet publish -r win-x64 -o {release_folder} -c Release /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:DebugType=None /p:DebugSymbols=false --self-contained false')

with zipfile.ZipFile('Release/FortnitePortingServer.zip', 'w', zipfile.ZIP_DEFLATED) as server_zip:
    server_zip.write("Plugins/Blender/FortnitePortingServer.py", "FortnitePortingServer.py")
    server_zip.write("Plugins/Blender/io_import_scene_unreal_psa_psk_280.py", "io_import_scene_unreal_psa_psk_280.py")
    server_zip.write("Plugins/Blender/FortnitePortingData.blend", "FortnitePortingData.blend")

with zipfile.ZipFile('Release/FortnitePorting.zip', 'w', zipfile.ZIP_DEFLATED) as main_zip:
    main_zip.write("Release/FortnitePorting.exe", "FortnitePorting.exe")
    main_zip.write("Release/FortnitePortingServer.zip", "FortnitePortingServer.zip")

os.remove("Release/FortnitePorting.exe")
os.remove("Release/FortnitePortingServer.zip")

os.startfile(os.path.realpath(release_folder))
    