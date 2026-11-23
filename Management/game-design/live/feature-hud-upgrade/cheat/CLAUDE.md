# Cheat: thanh nâng cấp

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Ở bản dev, chọn state `mixed-buttons` thì game dựng trận với số tiền và cấp từng mục như ép, không cần chơi tới.
- Giá trị ép đi qua đúng đường của game: tiền qua `feature-wallet`, cấp qua `feature-upgrade`; giá và sáng/mờ do `feature-upgrade/upgrade-purchase/purchase-rules` tính như thường, thanh nâng cấp chỉ hiện (`feature-hud-upgrade/button-state/button-display`).
- Code cheat nằm trong thư mục `HudUpgrade`, sau cờ dev; không lọt vào bản phát hành.

## Đã tự quyết
- **Một state duy nhất.** Trộn nút sáng và nút mờ trong cùng một state nên có đủ mọi thành phần; state "tất cả sáng" hay "tất cả mờ (tạm dừng)" chỉ đổi trạng thái, không thêm phần nào, nên không tách. Đã loại: ba state sáng, mờ, trộn.
- **Giá đúng bảng giá.** Cấp chọn rải để mỗi nút một giá khác nhau và có một nút mờ vì thiếu tiền (kĩ năng cấp 3, giá 135 > 120).
- **Frame `use` toàn bộ số** từ `feature-hud-upgrade/bar-layout/bar-buttons` (thanh, nhóm, vị trí nút), `feature-hud-upgrade/button-state/button-display` (hình, cấp, xu, giá trong nút), `feature-hud/hud-layout/layout-spec` (cỡ nút, dải dưới). Chỉ tự ghi bề ngang màn 1920 (màn của vòng 1).
- **Ảnh art-concept của thanh nâng cấp chỉ đặt ở node này**, một ảnh cho state `mixed-buttons`, style `ui-flat`.

## Chưa rõ
- Không có điểm chặn.
