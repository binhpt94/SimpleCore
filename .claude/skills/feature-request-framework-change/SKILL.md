---
name: feature-request-framework-change
description: Chỉ phiên Features, Services và @Core. Nhờ @CodeLead đổi khung (Scripts/Contracts, lớp base, *.asmdef, AssemblyInfo.cs, InternalsVisibleTo; interface I*.cs và lớp *Base.cs của Core) hoặc xoá, đổi tên asset FeatureAssets/Public - tin gửi đủ ngữ cảnh, file, đề xuất, rồi chờ. Dùng khi việc của mình cần đổi khung hay contract mà guard không cho tự sửa.
---
# Nhờ @CodeLead đổi khung
Khung chỉ @CodeLead sửa (bảng ở `CLAUDE.md` gốc và `.claude/rules/codelead.md`); phiên mình không tự sửa khung, guard chặn.

## Tin gửi @CodeLead
Gửi @CodeLead bằng SendMessage, đủ 3 mục:
- Ngữ cảnh: đang làm gì, vướng ở đâu, ai đang dùng thứ sẽ đổi.
- File: đường dẫn cụ thể.
- Đề xuất: đổi gì, vì sao không làm được trong vùng của mình.
Bắt buộc gửi khi cần đổi khung hoặc xoá, đổi tên asset `FeatureAssets/Public` (đổi contract). Phiên mình không tự sửa khung: guard chặn.

## Trong lúc chờ
- Không chép lớp base ra sửa, không viết tạm một contract riêng để lách.
- Làm tiếp phần không đụng thứ đang chờ; báo cáo cuối lượt ghi "Cần bên khác: @CodeLead, <việc>".
- @CodeLead có thể đưa cách khác: các bên thống nhất rồi mới làm. Khung đổi xong @CodeLead nhắn lại; lúc đó mới code trên khung mới.
