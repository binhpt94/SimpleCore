---
name: codelead-scaffold-feature
description: Chỉ phiên FEATURE=@CodeLead. Dựng khung cho một feature đã mở khoá code - đọc gd_pack, Contracts chỉ cho thứ feature khác đang cần, asmdef, AssemblyInfo, lớp base Internal/View, validate, biên dịch, báo cáo và nhắn phiên feature. Dùng khi một feature vừa mở khoá code hoặc cần khung mới.
---
# @CodeLead: dựng khung cho một feature
Cấu trúc khung, quyền và tiêu chí mặc định vẫn ở `.claude/rules/codelead.md`; luật Unity ở `.claude/rules/unity.md`.

## Dựng khung cho một feature
1. Đọc thiết kế của feature: `python Management/tools/gd_pack.py <Tên>` (node feature, các node bên dưới, node nhánh khác mà refs trỏ tới, thuật ngữ; đọc đủ các node vòng 4 trong file gói, không chỉ mục lục), và Contracts của các feature nó dùng.
2. Viết Contracts chỉ cho thứ feature khác đang cần (nêu tên), asmdef đúng ma trận của validator, `AssemblyInfo.cs` (View/Tests của chính nó).
3. Lớp base trong `Scripts/Internal/Base` và `Scripts/View/Base`: `internal abstract`, giữ khuôn riêng của feature (cổng vào ra, chỗ để lớp con điền luật); không chứa luật game cụ thể của feature. Luật game nằm ở lớp con do phiên feature viết. Vòng đời phiên scene không viết lại ở đây: lớp con kế thừa thẳng `Game.Core.SessionParticipantBase`. Feature không có khuôn riêng thì để trống `Base`.
4. Chạy `validate.py --feature <Tên>`, biên dịch; rồi báo cáo và nhắn phiên feature khung đã sẵn.
