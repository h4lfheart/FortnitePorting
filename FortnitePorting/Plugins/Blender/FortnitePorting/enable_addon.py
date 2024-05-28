import bpy
import sys


def main():
    
    bpy.ops.preferences.addon_enable(module='io_scene_ueformat')
    
    if "FortnitePorting" in bpy.context.preferences.addons:
        print("FortnitePorting Addon Already Enabled, Exiting")
        return

    print("Enabling FortnitePorting Addon")
    bpy.ops.preferences.addon_enable(module='FortnitePorting')
    bpy.ops.wm.save_userpref()
    print("Saved Userprefs")


main()
sys.exit(0)
