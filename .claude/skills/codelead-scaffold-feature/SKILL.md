---
name: codelead-scaffold-feature
description: Chỉ phiên FEATURE=@CodeLead. Dựng khung cho một feature đã mở khoá code - đọc gd_pack, Contracts chỉ cho thứ feature khác đang cần, asmdef, AssemblyInfo, lớp base Internal/View, validate, biên dịch, báo cáo và nhắn phiên feature. Dùng khi một feature vừa mở khoá code hoặc cần khung mới.
---
# @CodeLead: dựng khung cho một feature
Cấu trúc khung, quyền và tiêu chí mặc định vẫn ở `.claude/rules/codelead.md`; luật Unity ở `.claude/rules/unity.md`.

## Dựng khung cho một feature
Bước 1–3 (đọc gói, viết khung, validate) giao subagent `cl-scaffolder`: tin nêu tên feature và các feature đang cần contract của nó. Bạn đọc bản tóm tắt nó trả về, quyết các điểm nó hỏi, rồi làm bước 4. Feature nhỏ hay bạn đang có sẵn ngữ cảnh thì tự làm cũng được.
0. Thư mục `UnityProject/Assets/Features/<Tên>` (hay service, Core) chưa có: tự chạy `python Management/tools/scaffold.py` (chỉ tạo thư mục và `.gitkeep` theo `live`), không chờ @Manager.
1. Đọc thiết kế của feature: `python Management/tools/gd_pack.py <Tên>` (node feature, các node bên dưới, node nhánh khác mà refs trỏ tới, thuật ngữ; đọc đủ các node vòng 4 trong file gói, không chỉ mục lục), và Contracts của các feature nó dùng.
2. Viết Contracts chỉ cho thứ feature khác đang cần (nêu tên), asmdef đúng ma trận của validator, `AssemblyInfo.cs` (View/Tests của chính nó).
3. Lớp base trong `Scripts/Internal/Base` và `Scripts/View/Base`: `internal abstract`, giữ khuôn riêng của feature (cổng vào ra, chỗ để lớp con điền luật); không chứa luật game cụ thể của feature. Luật game nằm ở lớp con do phiên feature viết. Vòng đời phiên scene không viết lại ở đây: lớp con kế thừa thẳng `Game.Core.SessionParticipantBase`. Feature không có khuôn riêng thì để trống `Base`.
4. Chạy `validate.py --feature <Tên>`, biên dịch; rồi báo cáo và báo phiên feature khung đã sẵn qua hộp thư: `python Management/tools/dispatch.py send <Tên> --from CodeLead "Khung đã sẵn · <Contracts, lớp base mới> · ghi các contract mới vào CLAUDE.md của feature"`. `validate.py` báo DOC-02 cho tới khi phiên feature viết xong mục của từng contract.
