# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

Game Unity ghép từ các feature ĐỘC LẬP. AI viết code và đề xuất ý tưởng; người định hướng và duyệt.
Mặc định cấm: việc chưa được duyệt thì không làm. Muốn làm thì tạo file mới trong `Management/proposals/` rồi chờ duyệt.
Quyền do `.claude/hooks/guard.py` ép theo `FEATURE` của phiên, chia 5 nhóm: Master `@human`; Leaders `@Manager` (điều phối, không sửa code, chỉ tạo file mới trong `Management/proposals`), `@CodeLead` (kiểm code, giải quyết xung đột, biên dịch/test; không sửa gì, xem `.claude/rules/codelead.md`), `@GameDesigner` (thiết kế game, không code, chỉ đọc `Management/game-design` và chỉ ghi `Management/game-design/ai`, xem `.claude/rules/gamedesigner.md`); Basic `@Core` (chỉ sửa `Assets/Core`), `@Composition` (chỉ sửa `Assets/_Composition`); Features (`Wallet`); Services (`_Services/Popup`). Người gán trong chat bằng `FEATURE=<giá trị> <PIN>`; chưa gán thì cấm mọi thao tác sửa; AI không tự gán được.
Bị guard chặn thì dừng và báo người, không tìm đường vòng. Xoá file: chỉ file trong vùng của mình, qua unity-mcp (`manage_asset`, action `delete`, path bắt đầu bằng `Assets/`). Lệnh shell duy nhất được chạy, từ gốc project:
`python Management/tools/validate.py --feature <Tên>` — kiểm tra ranh giới feature (bỏ `--feature` là cả project); còn vi phạm thì chưa xong việc.
@Manager có thêm `python Management/tools/scaffold.py [--dry-run]`: tạo thư mục feature, service, Core, `_Composition` còn thiếu theo các feature trong `live` (tên trong `code.json`, chưa có thì tên đề xuất theo key; chỉ thư mục và `.gitkeep`, không tạo code). @Manager vẫn không code.

Feature nằm ở `UnityProject/Assets/Features/<Tên>/`, dịch vụ ở `Features/_Services/<Tên>/`. Không tạo `Shared/`. Phiên Features/Services chỉ sửa được code của mình khi thiết kế của nó đã tới vòng 4 trong `Management/game-design/live` (guard kiểm; dòng `guard:` đầu tin báo đang mở hay khoá); bị khoá thì dừng và báo người.

| Vùng | Đọc | Sửa |
|---|---|---|
| Feature của mình | được | được |
| `Scripts/Contracts` của mình | được | được, thống nhất với @CodeLead trước |
| `Scripts/Contracts`, `FeatureAssets/Public`, `CLAUDE.md` gốc của feature khác | được | cấm |
| `Scripts/Internal`, `Scripts/View`, `FeatureAssets/Private` của feature khác | cấm | cấm |
| `Assets/_Composition`, `Assets/Core` | được | `@Composition` / `@Core`: vùng của mình được; phiên khác: cấm, nhờ phiên đó |
| `UnityProject/Packages`, `UnityProject/ProjectSettings`, `Management/tools/rules.json` | được | hỏi người, từng lần |
| `Management/proposals` | được | chỉ tạo file mới |
| Phần còn lại của `Management` (`game-design/live` là thiết kế đã chốt, bám theo nó) | được | cấm; `game-design/ai`: chỉ `@GameDesigner` |
| `.claude/**`, file này | được | cấm |
| `Library`, `Temp`, `Logs`, `obj`, `UserSettings` | cấm | cấm |

Vùng không có trong bảng: cấm đọc và sửa. Tìm kiếm (Grep/Glob) phải đặt `path` nằm trong một vùng được đọc.
Sửa `Contracts`, `*.asmdef`, `InternalsVisibleTo`: gửi @CodeLead ngữ cảnh, file, đề xuất và chờ thống nhất. Xung đột code do @CodeLead giải quyết; lỗi @CodeLead báo về thì phiên sở hữu tự sửa. `rules.json`: hỏi người. Cấm `*.asmref`.
Luật ranh giới và mã lỗi (STR/ASM/SRC/AST): xem đầu `Management/tools/validate.py`; ngoại lệ đã duyệt nằm trong `Management/tools/rules.json`.
`Management/index.html` chỉ là vỏ hiển thị; nội dung đã duyệt nằm trong `Management/data/`.
Lượt có sửa file kết thúc bằng báo cáo (tới @CodeLead và @Manager) theo `.claude/rules/report.md`. Biên dịch, test, Play: @human hoặc @CodeLead. Phiên của người: `FEATURE=@human`. Thử lại guard: `python .claude/hooks/test_guard.py`.
