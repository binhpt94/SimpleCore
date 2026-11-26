---
name: tester-playtest-session
description: Chỉ nhóm Testers (FEATURE=@UnityAutoPlay). Một đợt chơi thử trong Unity, tám bước - nhận việc, tester.py new, đọc gd_pack, Play và dựng state cheat, chụp ảnh vào Captures rồi tester.py shot, ghi kết quả và lỗi, tester.py check, báo cáo và nhắn @CodeLead. Dùng khi bắt đầu một đợt test hay một yêu cầu XÁC NHẬN.
---
# Một đợt chơi thử
Quyền, thư mục, bảng lệnh `tester.py` và ai được giao việc vẫn ở `.claude/rules/tester.md`.

1. Nhận việc từ người hoặc @CodeLead (mục "Ai giao việc" của `tester.md`): test feature nào, mức nào (IDEA, PROTOTYPE, FULL), vì sao.
2. `tester.py new <Tên mình> "<tên đợt>"`. Ghi ngay mục Mục đích, Test gì, Môi trường.
3. Đọc thiết kế đã chốt: `python Management/tools/gd_pack.py <Tên feature>` (đọc đủ node vòng 4 và mục CHEAT của phần sẽ test). Đọc code khi cần hiểu vì sao game chạy như vậy; không sửa.
4. Chơi (tool chưa nạp: một `ToolSearch` `select:mcp__UnityMCP__manage_scene,mcp__UnityMCP__manage_editor,mcp__UnityMCP__read_console,mcp__UnityMCP__execute_menu_item,mcp__UnityMCP__execute_code,mcp__UnityMCP__manage_camera,SendMessage` cho cả đợt): mở scene, Play, dựng state bằng cheat của feature (menu hoặc `execute_code`), thao tác như người chơi, đọc console. Mỗi điều kiểm được ghi thành một bước: làm gì, mong thấy gì theo thiết kế, thực tế thấy gì.
5. Chụp ảnh chỗ đáng xem (state đạt, lỗi):
   - `manage_camera` action `screenshot`, `output_folder: "Captures"`, `screenshot_file_name: "<tên>.png"`, `include_image: true` (thấy ảnh ngay); guard chặn folder khác;
   - `python Management/tools/tester.py shot <Tên mình> <đợt> <tên>.png [<tên mới>]`: dời ảnh vào `images/` của đợt (Captures không commit, chỉ `images/` được giữ);
   - mỗi ảnh một dòng trong mục "Cấu trúc thư mục". Không chép ảnh bằng đường khác.
6. Stop Play. Ghi Kết quả và Lỗi tìm thấy. Lỗi phải tái hiện được: bước, ảnh, câu lỗi console, feature sở hữu.
7. `tester.py check` tới khi không còn lỗi cấu trúc.
8. Báo cáo cuối lượt (mục "Báo cáo" của `tester.md`). Lỗi của feature nào thì nhắn thêm @CodeLead (`SendMessage`, to: `CodeLead`) đường dẫn đợt test và danh sách lỗi; không nhắn thẳng yêu cầu sửa cho phiên feature, không sửa hộ.
