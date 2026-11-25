---
name: codelead-resolve-issue
description: Chỉ phiên FEATURE=@CodeLead. Xử lý một vấn đề hay báo cáo xong việc của phiên Basic, Features, Services - xét phạm vi ảnh hưởng, nhắn các bên và thống nhất, đổi khung hay để bên sở hữu sửa, biên dịch, test, Play, báo lỗi về đúng phiên, lỗi kéo dài, xoá thư mục khung. Dùng khi nhận tin vấn đề hoặc báo cáo mới.
---
# @CodeLead: xử lý một vấn đề
Tiêu chí mặc định (từ chối khi không thoả), khuôn BÁO CHẶN và sổ yêu cầu vẫn ở `.claude/rules/codelead.md`.

## Quy trình
1. Nhận vấn đề hoặc báo cáo xong việc (thấy ở đầu mỗi tin).
2. Xét phạm vi ảnh hưởng: đọc code các bên liên quan, ai dùng contract, asset id, asmdef nào.
3. Nhắn các phiên liên quan; chấp nhận đề xuất hoặc đưa đề xuất khác. Các bên thống nhất một cách tốt nhất.
4. Đổi khung: @CodeLead tự sửa, báo cáo, rồi nhắn mọi phiên dùng thứ vừa đổi. Phần còn lại: bên sở hữu tự sửa. @CodeLead biên dịch, chạy test, Play; có lỗi thì báo về đúng phiên kèm lỗi và file.
5. Lỗi kéo dài (sửa 2 lần chưa xong hoặc nhiều phiên đổ lỗi nhau): phân tích sâu, gửi từng bên yêu cầu chính xác (file, thay đổi, thứ tự).
Xoá thư mục: @CodeLead tự xoá thư mục khung (`Scripts/Contracts`, `Scripts/Internal/Base`, `Scripts/View/Base` và bên dưới) và thư mục bên trong `Assets/_Composition`, qua `manage_asset delete`, không cần hỏi. Không thuộc @CodeLead: `rules.json`, xoá cả `Assets/_Composition` (hỏi người); đổi luật hay ranh giới (đề xuất cho người).
