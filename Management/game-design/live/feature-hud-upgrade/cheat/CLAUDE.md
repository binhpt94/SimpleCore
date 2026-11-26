# Cheat: thanh nâng cấp

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Ở bản dev, chọn state `mixed-buttons` thì game dựng trận với số tiền và cấp từng mục như ép, không cần chơi tới.
- Giá trị ép đi qua đúng đường của game: tiền qua `feature-wallet`, cấp qua `feature-upgrade`; giá và sáng/mờ do `feature-upgrade/upgrade-purchase/purchase-rules` tính như thường, thanh nâng cấp chỉ hiện (`feature-hud-upgrade/button-state/button-display`).
- Code cheat nằm trong thư mục `HudUpgrade`, sau cờ dev; không lọt vào bản phát hành.

## Đã tự quyết
- **Một state duy nhất.** Trộn nút sáng và nút mờ trong cùng một state nên có đủ mọi thành phần; state "tất cả sáng" hay "tất cả mờ" chỉ đổi trạng thái, không thêm phần nào, nên không tách. Đã loại: ba state sáng, mờ, trộn.
- **Giá đúng bảng giá.** Cấp chọn rải để mỗi nút một giá khác nhau và có một nút mờ vì thiếu tiền (kĩ năng cấp 3, giá 135 > 120).
- **Frame `use` toàn bộ số** từ `feature-hud-upgrade/bar-layout/bar-buttons` (thanh, nhóm, vị trí nút), `feature-hud-upgrade/button-state/button-display` (hình, cấp, xu, giá trong nút), `feature-hud/hud-layout/layout-spec` (cỡ nút, dải dưới). Chỉ tự ghi bề ngang màn 1920 (màn của vòng 1).
- **Đội của state là đội IDEA (2026-11-25, sửa lệch).** Bản trước ép Pháp sư ở chỗ 3, nhưng IDEA chỉ có Hiệp sĩ, Đấu sĩ rìu, Cung thủ (`feature-hero/hero-list/hero-roster-values`); state phải ép được ở IDEA. Giữ đúng cấp và giá: Đấu sĩ rìu cấp 5 thay Pháp sư cấp 5 (giá hero không theo loại, vẫn 100), thứ tự chỗ theo đội mặc định. Đã loại: thêm Pháp sư vào IDEA.
- **Nút nâng cấp không có chữ tên mục (2026-11-25).** Label của sáu nút để cả trong ngoặc đơn: đó là chú thích, không phải chữ trên nút. Bản trước tool in "Hero chỗ 1", "Máu thành"... đè lên hình và chữ cấp trong ảnh claude-concept. Nút thật chỉ có hình, cấp, xu, giá (`feature-hud-upgrade/button-state/button-display`).
- **Ép được ở IDEA.** Ba mục thành và nút kĩ năng thành có từ IDEA; ở IDEA nâng kĩ năng chỉ tác động Sấm sét, nút vẫn như nhau.
- **Ảnh art-concept của thanh nâng cấp chỉ đặt ở node này**, một ảnh cho state `mixed-buttons`, style `ui-flat`.
- **Bỏ tạm dừng, quảng cáo và popup hồi sinh (2026-11-26, người quyết).** Bỏ "tạm dừng" khỏi ví dụ state tất cả mờ.

## Chưa rõ
- Không có điểm chặn.
