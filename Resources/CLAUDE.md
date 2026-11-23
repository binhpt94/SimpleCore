# Resources
Hàng @Codex và @Blender đã giao cho các feature: `Resources/<Feature>/<codex|blender>/<order-id>/v<n>/<item>.<đuôi>`.
- Chỉ `python Management/tools/studio.py deliver` ghi vào đây. Mỗi lần giao là một bản `v<n>` mới, bản cũ giữ nguyên làm lịch sử.
- Mọi phiên đã gán đều đọc được. Không ai sửa tay, kể cả phiên tool.
- Feature lấy hàng vào Unity bằng `studio.py take <Feature>/<order-id>`. Order và lịch sử nằm ở `Management/studio/`; luật ở `.claude/rules/studio.md`.
