import bpy

class TASTY_PT_RigSettings(bpy.types.Panel):
    bl_label = "Tasty Rig"
    bl_idname = "TASTY_PT_rig_settings"
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'UI'
    bl_category = "Tasty Rig"

    @classmethod
    def poll(cls, context):
        return (context.object and
                context.object.type == 'ARMATURE' and
                context.object.data.get("is_tasty"))

    def draw(self, context):
        layout = self.layout
        obj = context.object

        box = layout.box()
        box.row().label(text="FK / IK Controls", icon='CON_KINEMATIC')

        box.label(text="Hand")
        hand_row = box.row()
        hand_row.prop(obj, '["use_ik_hand_l"]', text="Left", slider=True)
        hand_row.prop(obj, '["use_ik_hand_r"]', text="Right", slider=True)

        box.label(text="Foot")
        leg_row = box.row()
        leg_row.prop(obj, '["use_ik_leg_l"]', text="Left", slider=True)
        leg_row.prop(obj, '["use_ik_leg_r"]', text="Right", slider=True)

        box.label(text="Fingers")
        finger_row = box.row()
        finger_col_l = finger_row.column()
        finger_col_l.enabled = obj.get("use_ik_hand_l", 1.0) != 0
        finger_col_l.prop(obj, '["use_ik_fingers_l"]', text="Left", slider=True)

        finger_col_r = finger_row.column()
        finger_col_r.enabled = obj.get("use_ik_hand_r", 1.0) != 0
        finger_col_r.prop(obj, '["use_ik_fingers_r"]', text="Right", slider=True)

        box = layout.box()
        box.row().label(text="Miscellaneous", icon='PROPERTIES')
        box.row().prop(obj, '["use_pole_targets"]', text="Pole Targets")
