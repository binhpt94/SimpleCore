---
name: codelead-resolve-issue
description: Chỉ phiên FEATURE=@CodeLead. Xử lý một vấn đề hay báo cáo xong việc của phiên Basic, Features, Services - xét phạm vi ảnh hưởng, nhắn các bên và thống nhất, đổi khung hay để bên sở hữu sửa, biên dịch, test, Play, báo lỗi về đúng phiên, lỗi kéo dài, xoá thư mục khung. Dùng khi nhận tin vấn đề hoặc báo cáo mới.
---
# @CodeLead: xử lý một vấn đề
Tiêu chí mặc định (từ chối khi không thoả), khuôn BÁO CHẶN và sổ yêu cầu vẫn ở `.claude/rules/codelead.md`.

## Quy trình
1. Nhận vấn đề hoặc báo cáo xong việc (thấy ở đầu mỗi tin). Báo xong một lô yêu cầu: kiểm theo skill `codelead-verify-batch`.
2. Xét phạm vi ảnh hưởng: ai dùng contract, asset id, asmdef nào. Giao subagent `Explore` tìm khắp `Assets/Features` rồi chỉ đọc đúng đoạn nó chỉ ra, không grep tay từng feature.
3. Nhắn các phiên liên quan (Leader: `SendMessage`; feature, service, @Core: hộp thư `python Management/tools/dispatch.py send <F> --from CodeLead "<việc>"`, thread chạy theo việc nên không SendMessage được); chấp nhận đề xuất hoặc đưa đề xuất khác. Các bên thống nhất một cách tốt nhất.
4. Đổi khung: @CodeLead tự sửa, báo cáo, rồi báo mọi phiên dùng thứ vừa đổi qua hộp thư. Phần còn lại: bên sở hữu tự sửa. @CodeLead biên dịch, chạy test, Play; có lỗi thì giao về đúng phiên qua hộp thư kèm lỗi và file; yêu cầu trong sổ thì `gd_req.py <F> reject <ref> "<lỗi>"`, lệnh tự gửi tin cho feature. `dispatch.py send` báo SERVER TẮT: tin đã lưu, nhắn Manager BÁO CHẶN.
5. Lỗi kéo dài (sửa 2 lần chưa xong hoặc nhiều phiên đổ lỗi nhau): phân tích sâu, gửi từng bên yêu cầu chính xác (file, thay đổi, thứ tự).
Xoá thư mục: @CodeLead tự xoá thư mục khung (`Scripts/Contracts`, `Scripts/Internal/Base`, `Scripts/View/Base` và bên dưới) và thư mục bên trong `Assets/_Composition`, qua `manage_asset delete`, không cần hỏi. Không thuộc @CodeLead: `rules.json`, xoá cả `Assets/_Composition` (hỏi người); đổi luật hay ranh giới (đề xuất cho người).
