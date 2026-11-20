# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

Game Unity ghép từ các feature ĐỘC LẬP. AI viết code và đề xuất ý tưởng; người định hướng và duyệt.
Mặc định cấm: việc chưa được duyệt thì không làm. Muốn làm thì tạo file mới trong `Management/proposals/` rồi chờ duyệt.
Quyền do `.claude/hooks/guard.py` ép theo biến môi trường `FEATURE` (ví dụ `Wallet`, `_Services/Popup`). Không đặt thì cấm mọi thao tác sửa.
Bị guard chặn thì dừng và báo người, không tìm đường vòng. Lệnh shell duy nhất được chạy, từ gốc project:
`python Management/tools/validate.py` — kiểm tra ranh giới feature; còn vi phạm thì chưa xong việc.

Feature nằm ở `UnityProject/Assets/Features/<Tên>/`, dịch vụ ở `Features/_Services/<Tên>/`. Không tạo `Shared/`.

| Vùng | Đọc | Sửa |
|---|---|---|
| Feature của mình | được | được |
| `Scripts/Contracts` của mình | được | hỏi người |
| `Scripts/Contracts`, `FeatureAssets/Public` của feature khác | được | cấm |
| `Scripts/Internal`, `Scripts/View`, `FeatureAssets/Private` của feature khác | cấm | cấm |
| `Assets/_Composition`, `Assets/Core` | được | hỏi người, từng lần |
| `UnityProject/Packages`, `UnityProject/ProjectSettings` | được | hỏi người, từng lần |
| `Management/proposals` | được | chỉ tạo file mới |
| Phần còn lại của `Management` | được | cấm |
| `.claude/**`, file này | được | cấm |
| `Library`, `Temp`, `Logs`, `obj`, `UserSettings` | cấm | cấm |

Vùng không có trong bảng: cấm đọc và sửa. Tìm kiếm (Grep/Glob) phải đặt `path` nằm trong một vùng được đọc.
Sửa `Contracts`, `*.asmdef`, `InternalsVisibleTo`, `_Composition`, `Core` đều phải hỏi: nêu sửa gì và vì sao không làm được trong feature của mình. Cấm `*.asmref`.
Luật ranh giới và mã lỗi (STR/ASM/SRC/AST): xem đầu `Management/tools/validate.py`; ngoại lệ đã duyệt nằm trong `Management/tools/rules.json`.
`Management/index.html` chỉ là vỏ hiển thị; nội dung đã duyệt nằm trong `Management/data/`.
Phiên của người dùng `FEATURE=@human`. Thử lại guard: `python .claude/hooks/test_guard.py`.
