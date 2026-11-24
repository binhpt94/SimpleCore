# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

Game Unity ghép từ các feature ĐỘC LẬP. AI viết code và đề xuất ý tưởng; người định hướng và duyệt.
Mặc định cấm: việc chưa được duyệt thì không làm. Muốn làm thì tạo file mới trong `Management/proposals/` rồi chờ duyệt.
Quyền do `.claude/hooks/guard.py` ép theo `FEATURE` của phiên, chia 6 nhóm: Master `@human`; Leaders `@Manager` (điều phối, không sửa code, chỉ tạo file mới trong `Management/proposals`), `@CodeLead` (quản cấu trúc khung và nối dây: chỉ sửa khung và toàn bộ `Assets/_Composition`; kiểm code, giải quyết xung đột, biên dịch/test; xem `.claude/rules/codelead.md`), `@GameDesigner` (thiết kế game, không code, chỉ đọc `Management/game-design` và chỉ ghi `Management/game-design/ai`, xem `.claude/rules/gamedesigner.md`); Tools `@Codex` (ảnh, tham vấn GPT), `@Blender` (model 3D), `@ClaudeImage` (ảnh Claude tự vẽ bằng SVG, đủ cho giai đoạn IDEA): không code, chỉ nhận order của feature, xem `.claude/rules/studio.md`; `@DesignAdvisor` (hỏi GPT về thiết kế khi người hoặc @GameDesigner nhắn, chỉ ghi câu hỏi và nhận xét trong `Management/game-design/advice`, góp ý không ràng buộc, xem `.claude/rules/designadvisor.md`); Basic `@Core` (chỉ sửa `Assets/Core`); Features (`Wallet`); Services (`_Services/Popup`). Người gán trong chat bằng `FEATURE=<giá trị> <PIN>`; chưa gán thì cấm mọi thao tác sửa; AI không tự gán được. Tên chat bỏ `@` của vai trò (`Codex`, `GameDesigner`, `CodeLead`...; feature giữ tên, `Wallet`) vì SendMessage hiểu `@` là địa chỉ team: nhắn @Codex bằng `SendMessage` với `to: "Codex"`.
Bị guard chặn thì dừng và báo người, không tìm đường vòng. Xoá file: chỉ file trong vùng của mình, qua unity-mcp (`manage_asset`, action `delete`, path bắt đầu bằng `Assets/`).
unity-mcp theo tác động của lệnh (bảng ở `.claude/rules/unity.md`): đọc thì path phải đọc được; tạo, sửa asset và prefab chỉ trong vùng mình; mọi phiên code được biên dịch (`refresh_unity`) và đọc console. Mở, sửa, lưu scene hay prefab stage thì guard giữ khoá Editor cho một chat tới hết lượt. Play, test, menu, cài đặt project: @CodeLead. Code chỉ viết bằng Edit/Write, không qua unity-mcp.
Lệnh shell được chạy (mọi phiên), từ gốc project:
`python Management/tools/validate.py --feature <Tên>` — kiểm tra ranh giới feature (bỏ `--feature` là cả project); còn vi phạm thì chưa xong việc.
`python Management/tools/studio.py check|status` — order ảnh/3D (feature đặt order, chấm từng item đã giao bằng `review <F>/<id> <item> <0-3> "<phản hồi>"`: 0 làm lại ngay, 1 lỗi nhưng chưa cần làm ngay, 2 đúng phần lớn, 3 đạt; @Codex, @Blender, @ClaudeImage chạy tool, `pick`, `deliver`; `measure <F>/<id> [<item>]` đo kích thước và nền trong suốt của ảnh, mọi phiên chạy được; lượt gen có trần, hết lượt thì giao "chưa chuẩn" kèm phiếu nợ art, xem tab Studio; luật ở `.claude/rules/studio.md`).
`python Management/tools/gd_pack.py <Tên>` — gói thiết kế đã chốt của một feature (node feature, node con, node nhánh khác mà refs trỏ tới, thuật ngữ, frame bố cục): bám theo gói này khi code. Gói dài nên lệnh ghi ra `Management/game-design/.pack/` rồi in đường dẫn và mục lục có số dòng: đọc bằng Read (offset, limit) hoặc Grep; trước khi code một phần, đọc đủ node vòng 4 và mục CHEAT của phần đó.
`python Management/tools/gd_frame.py live/<node>` (hoặc `ai/idea/<node>`) — frame bố cục của một node vòng 4, đã giải ra toạ độ và kích thước tuyệt đối (px trên màn chuẩn, hoặc u trên chiến trường).
@Manager có thêm `python Management/tools/scaffold.py [--dry-run]`: tạo thư mục feature, service, Core, `_Composition` còn thiếu theo các feature trong `live` (tên trong `code.json`, chưa có thì tên đề xuất theo key; chỉ thư mục và `.gitkeep`, không tạo code). @Manager vẫn không code.

Feature nằm ở `UnityProject/Assets/Features/<Tên>/`, dịch vụ ở `Features/_Services/<Tên>/`. Không tạo `Shared/`. Phiên Features/Services chỉ sửa được code của mình khi thiết kế của nó đã tới vòng 4 trong `Management/game-design/live` (guard kiểm; dòng `guard:` đầu tin báo đang mở hay khoá); bị khoá thì dừng và báo người.

| Vùng | Đọc | Sửa |
|---|---|---|
| Feature của mình | được | được |
| Khung của feature mình: `Scripts/Contracts`, `Scripts/Internal/Base`, `Scripts/View/Base`, `*.asmdef`, `AssemblyInfo.cs` | được | cấm: chỉ @CodeLead (khi feature đã mở khoá code); code mới viết trên lớp con |
| Khung của Core: `*.asmdef`, `AssemblyInfo.cs`, interface `I*.cs`, lớp base `*Base.cs` (ngoài `Tests/`) | được | chỉ @CodeLead |
| `Scripts/Contracts`, `FeatureAssets/Public`, `CLAUDE.md` gốc của feature khác | được | cấm |
| `Scripts/Internal`, `Scripts/View`, `FeatureAssets/Private` của feature khác | cấm | cấm |
| `Assets/Core` | được | `@Core`: được (trừ khung); phiên khác: cấm, nhờ @Core |
| `Assets/_Composition` (nối dây: Boot, scene, catalog asset) | được | chỉ @CodeLead (cả vùng); phiên khác: cấm, nhờ @CodeLead |
| `UnityProject/Packages`, `UnityProject/ProjectSettings`, `Management/tools/rules.json` | được | hỏi người, từng lần |
| `Management/proposals` | được | chỉ tạo file mới |
| `Management/studio` (order, lịch sử, bài học của @Codex/@Blender/@ClaudeImage) | được (mọi phiên) | feature: chỉ tạo mới `order.md` trong `orders/<mình>/<id>/`; @GameDesigner: tương tự trong `codex/orders/_design/` và `claudeimage/orders/_design/` (ảnh art-concept và claude-concept, chỉ cho node `<feature>/cheat`, tối đa 3 ảnh mỗi feature; đặt xong tự nhắn @Codex hoặc @ClaudeImage, người chấm từng ảnh trên trang); @Codex/@Blender/@ClaudeImage: `lessons.md` và `work/` của order; còn lại studio.py và trang |
| `Resources` (hàng đã giao) | được (mọi phiên) | cấm: chỉ studio.py ghi |
| Phần còn lại của `Management` (`game-design/live` là thiết kế đã chốt, bám theo nó) | được | cấm; `game-design/ai`: chỉ `@GameDesigner`; `game-design/advice` (góp ý của GPT, chỉ để tham khảo, không phải thiết kế): chỉ `@DesignAdvisor` và `gd_advise.py` |
| `.claude/**`, file này | được | cấm |
| `Library`, `Temp`, `Logs`, `obj`, `UserSettings` | cấm | cấm |

Vùng không có trong bảng: cấm đọc và sửa. Tìm kiếm (Grep/Glob) phải đặt `path` nằm trong một vùng được đọc.
Cần đổi khung (`Contracts`, lớp base, `*.asmdef`, `InternalsVisibleTo`): gửi @CodeLead ngữ cảnh, file, đề xuất; @CodeLead sửa, phiên mình chờ. Xung đột code do @CodeLead giải quyết; lỗi @CodeLead báo về thì phiên sở hữu tự sửa. `rules.json`: hỏi người. Cấm `*.asmref`.
Luật ranh giới và mã lỗi (STR/ASM/SRC/AST): xem đầu `Management/tools/validate.py`; ngoại lệ đã duyệt nằm trong `Management/tools/rules.json`.
`Management/index.html` chỉ là vỏ hiển thị; nội dung đã duyệt nằm trong `Management/data/`.
Lượt có sửa file kết thúc bằng báo cáo (tới @CodeLead và @Manager) theo `.claude/rules/report.md`. Test, Play: @human hoặc @CodeLead; biên dịch: mọi phiên code. Phiên của người: `FEATURE=@human`. Thử lại guard: `python .claude/hooks/test_guard.py`.
