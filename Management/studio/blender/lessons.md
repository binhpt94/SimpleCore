# Bài học của @Blender
@Blender đọc toàn bộ file này trước khi viết mỗi script và áp mọi mục vào script đó. Mỗi mục một dòng: luật làm được, kèm nguồn (đường dẫn feedback hoặc log). Xem .claude/rules/studio.md.

- Blender 5.0.1 headless trên máy này: bpy.ops.export_scene.fbx lỗi (thiếu use_space_transform, không nhận use_selection); xuất .glb bằng bpy.ops.export_scene.gltf(export_format="GLB"), trừ khi order bắt buộc FBX thì báo feature (lần chạy thử studio.py, 2026-11-23)
- Không gọi bpy.ops.wm.read_factory_settings trong script: studio.py đã chạy --factory-startup, và gọi lại làm hỏng các addon xuất file (lần chạy thử studio.py, 2026-11-23)
