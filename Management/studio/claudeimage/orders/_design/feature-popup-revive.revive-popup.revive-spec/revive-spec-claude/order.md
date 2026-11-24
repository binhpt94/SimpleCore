---
tool: claudeimage
kind: art
count: 1
deliver: concept
format: png 1920x1080
style: ui-flat
design: feature-popup-revive/revive-popup/revive-spec
---
# Claude concept: popup hồi sinh

## Mô tả
Hình tham chiếu cho popup hồi sinh, style `ui-flat`. Bố cục lấy đúng frame `main`, đọc bằng `python Management/tools/gd_frame.py live/feature-popup-revive/revive-popup/revive-spec`.

Chỉ vẽ phần node này tự định (nhóm "của node" trong `gd_frame.py`): nút chính "Xem quảng cáo" 400x120 và nút phụ "Kết thúc lượt" 260x120. Hai nút cùng hàng, cùng chỗ và cùng kiểu với hai nút của popup thua. Nếu phần nút của `feature-popup-lose/lose-actions/actions-spec` dùng được, khai `same-as` thay vì vẽ lại. Khung popup là hình mượn của dịch vụ popup, tool giữ hình cơ bản. Nền che bỏ qua. Chữ do tool in.

## Danh sách
- main: popup hồi sinh đủ tiêu đề, dòng hồi sinh, dòng chờ quảng cáo và hai nút, đúng frame `main`

## Tiêu chí nhận
- Ảnh ghép đúng kích thước tool dựng từ frame; hai nút đúng hộp của frame.
- Nút "Xem quảng cáo" sáng hơn và nổi hơn hẳn nút "Kết thúc lượt".
- Nút phẳng, viền dày tối thiểu 3 px, cùng bán kính bo với nút popup thua.
