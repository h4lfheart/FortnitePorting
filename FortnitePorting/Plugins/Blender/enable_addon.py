import bpy
import sys


def main():
    if "FortnitePorting" in bpy.context.preferences.addons:
        return

    bpy.ops.preferences.addon_enable(module='FortnitePorting')
    bpy.ops.wm.save_userpref()


main()
sys.exit(0)
