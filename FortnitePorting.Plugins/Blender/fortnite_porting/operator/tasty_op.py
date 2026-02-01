import bpy

class TASTY_PT_RigSettings(bpy.types.Panel):
    bl_label = "Tasty Rig"
    bl_idname = "TASTY_PT_rig_settings"
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'UI'
    bl_category = "Fortnite Porting"

    @classmethod
    def poll(cls, context):
        return (context.object and
                context.object.type == 'ARMATURE' and
                context.object.data.get("is_tasty"))

    def draw(self, context):
        layout = self.layout
        obj = context.object

        box = layout.box()
        box.row().label(text="IK Controls", icon='CON_KINEMATIC')

        HandRow = box.row()
        HandRow.prop(obj, '["use_ik_hand_r"]', text="Hand R", slider=True)
        HandRow.prop(obj, '["use_ik_hand_l"]', text="Hand L", slider=True)

        LegRow = box.row()
        LegRow.prop(obj, '["use_ik_leg_r"]', text="Leg R", slider=True)
        LegRow.prop(obj, '["use_ik_leg_l"]', text="Leg L", slider=True)

        box.row().prop(obj, '["use_ik_fingers"]', text="Fingers", slider=True)

        box = layout.box()
        box.row().label(text="Miscellaneous", icon='PROPERTIES')
        box.row().prop(obj, '["use_pole_targets"]', text="Pole Targets", slider=True)