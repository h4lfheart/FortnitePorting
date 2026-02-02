import os.path
import math
import bpy

from ..enums import *
from ..utils import *
from ..mappings import *
from ...utils import *
from ...logger import Log
from ...ueformat.importer.logic import UEFormatImport
from ...ueformat.importer.classes import UEAnim
from ...ueformat.options import UEAnimOptions
from ...server import Server

class AnimImportContext:
    
    def import_anim_data(self, data, override_skeleton=None):
        target_skeleton = override_skeleton or get_selected_armature()
        bpy.context.view_layer.objects.active = target_skeleton
        
        if target_skeleton is None:
            Server.instance.send_message("An armature must be selected to import an animation onto. Please select an armature and try again.")
            return

        if target_skeleton.data.get("is_tasty"):
            target_skeleton["use_pole_targets"] = False
            target_skeleton["use_ik_fingers"] = False
            

        # clear old data
        if anim_data := target_skeleton.animation_data:
           anim_data.action = None
           
           for track in anim_data.nla_tracks:
               anim_data.nla_tracks.remove(track)
        else:
            target_skeleton.animation_data_create()

        active_mesh = get_armature_mesh(target_skeleton)
        if active_mesh is not None and active_mesh.data.shape_keys is not None:
            active_mesh.data.shape_keys.name = "Pose Asset Controls"
            
            if shape_key_anim_data := active_mesh.data.shape_keys.animation_data:
                shape_key_anim_data.action = None
                for track in shape_key_anim_data.nla_tracks:
                    shape_key_anim_data.nla_tracks.remove(track)
            else:
                active_mesh.data.shape_keys.animation_data_create()
            
            
        if sequence_editor := get_sequence_editor():
            sequences_to_remove = where(sequence_editor.strips, lambda seq: seq.get("FPSound"))
            for sequence in sequences_to_remove:
                sequence_editor.strips.remove(sequence)

        bpy.context.scene.frame_set(0)

        # start import
        target_track = target_skeleton.animation_data.nla_tracks.new(prev=None)
        target_track.name = "Sections"

        if active_mesh is not None and active_mesh.data.shape_keys is not None:
            mesh_track = active_mesh.data.shape_keys.animation_data.nla_tracks.new(prev=None)
            mesh_track.name = "Sections"

        def import_sections(sections, skeleton, track, is_main_skeleton = False):
            total_frames = 0
            anim_fps = 30
            for section in sections:
                path = section.get("Path")

                action, anim_data = self.import_anim(path, skeleton)
                clear_children_bone_transforms(skeleton, action, "faceAttach")

                if anim_data.metadata is not None:
                    anim_fps = anim_data.metadata.frames_per_second
                
                section_length_frames = time_to_frame(section.get("Length"), anim_fps)
                total_frames += section_length_frames

                section_name = section.get("Name")
                time_offset = section.get("Time")
                loop_count = 999 if self.options.get("LoopAnimation") and section.get("Loop") else 1
                frame = time_to_frame(time_offset, anim_fps)

                if len(track.strips) > 0 and frame < track.strips[-1].frame_end:
                    frame = int(track.strips[-1].frame_end)

                strip = track.strips.new(section_name, frame, action)
                strip.repeat = loop_count

                if len(anim_data.curves) > 0 and active_mesh.data.shape_keys is not None and is_main_skeleton:
                    key_blocks = active_mesh.data.shape_keys.key_blocks
                    for key_block in key_blocks:
                        key_block.value = 0

                    def interpolate_keyframes(keyframes: list, frame: float, fps: float = 30) -> float:
                        frame_time = frame / fps
                    
                        if frame_time <= keyframes[0].frame / fps:
                            return keyframes[0].value
                        if frame_time >= keyframes[-1].frame / fps:
                            return keyframes[-1].value
                    
                        for i in range(len(keyframes) - 1):
                            k1 = keyframes[i]
                            k2 = keyframes[i + 1]
                            if k1.frame / fps <= frame_time <= k2.frame / fps:
                                t = (frame_time - k1.frame / fps) / (k2.frame / fps - k1.frame / fps)
                                return k1.value * (1 - t) + k2.value * t
                    
                        return keyframes[-1].value
                    
                    def import_curve_mapping(curve_mapping):
                        for curve_mapping in curve_mapping:
                            if target_block := best(key_blocks, lambda block: block.name.lower(), curve_mapping.get("Name").lower()):
                                for frame in range(section_length_frames):
                                    value_stack = []

                                    for element in curve_mapping.get("ExpressionStack"):
                                        element_type = EOpElementType(element.get("ElementType"))
                                        element_value = element.get("Value")
                                        match element_type:
                                            case EOpElementType.OPERATOR:
                                                operator_type = EOperator(element_value)

                                                if operator_type == EOperator.NEGATE:
                                                    item_two = 1
                                                    item_one = value_stack.pop()
                                                else:
                                                    item_two = value_stack.pop()
                                                    item_one = value_stack.pop()

                                                match operator_type:
                                                    case EOperator.NEGATE:
                                                        value_stack.append(-item_one)
                                                    case EOperator.ADD:
                                                        value_stack.append(item_one + item_two)
                                                    case EOperator.SUBTRACT:
                                                        value_stack.append(item_one - item_two)
                                                    case EOperator.MULTIPLY:
                                                        value_stack.append(item_one * item_two)
                                                    case EOperator.DIVIDE:
                                                        value_stack.append(item_one / item_two)
                                                    case EOperator.MODULO:
                                                        value_stack.append(item_one % item_two)
                                                    case EOperator.POWER:
                                                        value_stack.append(item_one ** item_two)
                                                    case EOperator.FLOOR_DIVIDE:
                                                        value_stack.append(item_one // item_two)

                                            case EOpElementType.NAME:
                                                sub_curve_name = str(element_value)
                                                if target_curve := best(anim_data.curves, lambda curve: curve.name.lower(), sub_curve_name.lower()):
                                                    target_value = interpolate_keyframes(target_curve.keys, frame, fps=anim_fps)
                                                    value_stack.append(target_value)
                                                else:
                                                    value_stack.append(0)

                                            case EOpElementType.FUNCTION_REF:
                                                function_index = element_value
                                                argumentCount = 0
                                                match function_index:
                                                    case 0:
                                                        argumentCount = 3
                                                    case function_index if 1 <= function_index <= 2:
                                                        argumentCount = 2
                                                    case function_index if 3 <= function_index <= 16:
                                                        argumentCount = 1
                                                    case function_index if 17 <= function_index <= 19:
                                                        argumentCount = 0
                                                    
                                                arguments = [0] * argumentCount
                                                for arg_index in range(argumentCount):
                                                    arguments[argumentCount - arg_index - 1] = value_stack.pop()
                                                    
                                                match function_index:
                                                    case 0: # clamp
                                                        value_stack.append(min(max(arguments[1], arguments[0]), arguments[2]))
                                                    case 1: # min
                                                        value_stack.append(min(arguments[0], arguments[1]))
                                                    case 2: # max
                                                        value_stack.append(max(arguments[0], arguments[1]))
                                                    case 3: # abs
                                                        value_stack.append(abs(arguments[0]))
                                                    case 4: # round
                                                        value_stack.append(round(arguments[0]))
                                                    case 5: # ceil
                                                        value_stack.append(math.ceil(arguments[0]))
                                                    case 6: # floor
                                                        value_stack.append(math.floor(arguments[0]))
                                                    case 7: # sin
                                                        value_stack.append(math.sin(arguments[0]))
                                                    case 8: # cos
                                                        value_stack.append(math.cos(arguments[0]))
                                                    case 9: # tan
                                                        value_stack.append(math.tan(arguments[0]))
                                                    case 10: # arcsin
                                                        value_stack.append(math.asin(arguments[0]))
                                                    case 11: # arccos
                                                        value_stack.append(math.acos(arguments[0]))
                                                    case 12: # arctan
                                                        value_stack.append(math.atan(arguments[0]))
                                                    case 13: # sqrt
                                                        value_stack.append(math.sqrt(arguments[0]))
                                                    case 14: # invsqrt
                                                        value_stack.append(1 / math.sqrt(arguments[0]))
                                                    case 15: # log
                                                        value_stack.append(math.log(arguments[0], math.e))
                                                    case 16: # exp
                                                        value_stack.append(math.exp(arguments[0]))
                                                    case 17: # exp
                                                        value_stack.append(math.e)
                                                    case 18: # exp
                                                        value_stack.append(math.pi)
                                                    case 19: # undef
                                                        value_stack.append(float('nan'))

                                            case EOpElementType.FLOAT:
                                                value_stack.append(float(element_value))

                                    target_block.value = value_stack.pop()
                                    target_block.keyframe_insert(data_path="value", frame=frame)

                                target_block.value = 0

                    is_skeleton_legacy = any(skeleton.data.bones, lambda bone: bone.name == "faceAttach")
                    is_skeleton_metahuman = any(skeleton.data.bones, lambda bone: bone.name == "FACIAL_C_FacialRoot")
                    
                    is_anim_legacy = any(anim_data.curves, lambda curve: curve.name in legacy_curve_names)
                    is_anim_metahuman = any(anim_data.curves, lambda curve: curve.name == "is_3L")
                    
                    if (is_skeleton_legacy and is_anim_legacy) or (is_anim_metahuman and is_anim_metahuman):
                        for curve in anim_data.curves:
                            if target_block := best(key_blocks, lambda block: block.name.lower(), curve.name.lower()):
                                for key in curve.keys:
                                    target_block.value = key.value
                                    target_block.keyframe_insert(data_path="value", frame=key.frame)
                                
                    if is_skeleton_metahuman and is_anim_legacy and (legacy_to_metahuman_mappings := data.get("LegacyToMetahumanMappings")):
                        import_curve_mapping(legacy_to_metahuman_mappings)

                    if is_skeleton_legacy and is_anim_metahuman and (metahuman_to_legacy_mappings := data.get("MetahumanToLegacyMappings")):
                        import_curve_mapping(metahuman_to_legacy_mappings)

                    if active_mesh.data.shape_keys.animation_data.action is not None:
                        try:
                            strip = mesh_track.strips.new(section_name, frame, active_mesh.data.shape_keys.animation_data.action)
                            strip.name = section_name
                            strip.repeat = loop_count
                        except Exception:
                            pass

                        active_mesh.data.shape_keys.animation_data.action = None
                        
            return total_frames

        total_frames = import_sections(data.get("Sections"), target_skeleton, target_track, True)
        if self.options.get("UpdateTimelineLength"):
            bpy.context.scene.frame_end = total_frames

        props = data.get("Props")
        if len(props) > 0:
            if master_skeleton := first(target_skeleton.children, lambda child: child.name == "Master_Skeleton"):
                bpy.data.objects.remove(master_skeleton)

            master_skeleton = self.import_model(data.get("Skeleton"), can_reorient=False)
            master_skeleton.name = "Master_Skeleton"
            master_skeleton.parent = target_skeleton
            master_skeleton.animation_data_create()

            master_track = master_skeleton.animation_data.nla_tracks.new(prev=None)
            master_track.name = "Sections"

            import_sections(data.get("Sections"), master_skeleton, master_track)

            for prop in props:
                mesh = self.import_model(prop.get("Mesh"))
                constraint_object(mesh, master_skeleton, prop.get("SocketName"), [0, 0, 0])
                mesh.rotation_euler = make_euler(prop.get("RotationOffset"))
                mesh.location = make_vector(prop.get("LocationOffset"), unreal_coords_correction=True) * 0.01
                mesh.scale = make_vector(prop.get("Scale"))

                if (anims := prop.get("AnimSections")) and len(anims) > 0:
                    mesh.animation_data_create()
                    mesh_track = mesh.animation_data.nla_tracks.new(prev=None)
                    mesh_track.name = "Sections"
                    import_sections(anims, mesh, mesh_track)

            master_skeleton.hide_set(True)

        if self.options.get("ImportSounds"):
            for sound in data.get("Sounds"):
                path = sound.get("Path")
                self.import_sound(path, time_to_frame(sound.get("Time")))

    def import_anim(self, path: str, override_skeleton=None) -> tuple[bpy.types.Action, UEAnim]:
        path = path[1:] if path.startswith("/") else path
        file_path, name = path.split(".")
        if (existing := bpy.data.actions.get(name)) and existing["Skeleton"] == override_skeleton.name and not existing["HasCurves"]:
            return existing, UEAnim()

        anim_path = os.path.join(self.assets_root, file_path + ".ueanim")
        options = UEAnimOptions(link=False,
                                override_skeleton=override_skeleton,
                                scale_factor=self.scale,
                                import_curves=False)
        action, anim_data = UEFormatImport(options).import_file(anim_path)
        action["Skeleton"] = override_skeleton.name
        action["HasCurves"] = len(anim_data.curves) > 0
        return action, anim_data
    