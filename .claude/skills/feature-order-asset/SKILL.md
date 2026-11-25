---
name: feature-order-asset
description: Chỉ phiên Features và Services. Đặt order ảnh hay model 3D cho feature (studio - @ClaudeImage, @Codex, @Blender; check tự đưa order vào hộp thư của tool), rồi chấm từng item đã giao bằng studio.py review (mức 0 tự gửi tool làm lại). Dùng khi feature cần sprite, icon, ảnh hay model, hoặc khi hộp thư báo order đã giao.
---
# Đặt và chấm order ảnh, model
Khung `order.md`, bảng mức chấm và phiếu nợ art nằm ở `.claude/rules/studio.md` (mục "Khung order", "Mức chấm", "Phiếu nợ art"): Read các mục đó trước khi viết order đầu tiên của phiên.

## Đặt order
1. Chỉ khi feature đã mở khoá code (cùng cổng thiết kế với code).
2. Chọn tool:
   - giai đoạn IDEA: `claudeimage` (`kind: image`) là đủ, hình phẳng, rõ nghĩa, đúng kích thước;
   - cần ảnh đẹp hơn (PROTOTYPE, FULL): `codex` (`kind: image`); hỏi GPT một điều: `codex`, `kind: consult`;
   - model 3D: `blender` (`kind: model`).
   Claude-concept của node thiết kế có bảng phần (`gd_pack.py <Tên>` in tổng số hình): đặt sprite theo bảng đó, không chép phần vào `Assets`.
3. Write mới `Management/studio/<tool>/orders/<Tên feature>/<id>/order.md` theo khung (id: chữ thường, số, gạch nối). Chỉ tạo một lần; muốn thêm hay bớt item thì đặt order mới.
   - Mỗi dòng Danh sách là đúng một kết quả; `count` bằng số dòng.
   - Tiêu chí nhận viết điều đo được (kích thước, nền trong suốt, đọc rõ ở 64 px...). Phong cách thì đưa ảnh tham chiếu (`references:`).
4. `python Management/tools/studio.py check`; qua thì order tự vào hộp thư của tool (mỗi order một lần), setup_server.py bật phiên tool lên làm. Không nhắn tool (guard chặn SendMessage tới tool). Xem: `python Management/tools/dispatch.py list`. Lệnh báo SERVER TẮT thì order vẫn được lưu: báo trong báo cáo, chờ người bật server.

## Chấm hàng đã giao
1. Tin "order đã giao" tới ở đầu tin (hộp thư, `deliver` tự gửi; item nào "chưa chuẩn" thì tin nói rõ). Xem: `python Management/tools/studio.py status`, ảnh ở `Resources/<Tên>/<tool>/<id>/v<n>/`. Thứ đo được thì đo: `studio.py measure <Tên>/<id> [<item>]`.
2. Chấm từng item: `python Management/tools/studio.py review <Tên>/<id> <item> <0-3> "<phản hồi>"`.
   - 0 làm lại ngay (bắt buộc có phản hồi); 1 lỗi nhưng chưa cần làm ngay; 2 đúng phần lớn; 3 đạt.
   - Phản hồi trong ngoặc kép, không dùng `;` `&` `|` `<` `>` `$` `(` `)` `{` `}` hay dấu ngoặc kép bên trong.
3. Mức 1–3 được chép ngay vào `FeatureAssets/<deliver>/Generated/<id>/`: `refresh_unity` để có `.meta`. Mức 1, 2 ghi vào `QUALITY.md` ở đó.
4. Có item mức 0: chính lệnh `review … 0` tự đưa tin làm lại vào hộp thư của tool, không nhắn thêm.
5. Order không cần nữa (đặt trùng, thiết kế đổi): `python Management/tools/studio.py cancel <Tên>/<id> "<lí do>"`, không để nó nằm ở trạng thái mới.
