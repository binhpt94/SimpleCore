# Cheat: HUD trận

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Ở bản dev, chọn state `mid-battle` thì game dựng trận với máu thành, tiền, đợt và hồi chiêu như ép, không cần chơi tới.
- Giá trị ép đi qua đúng đường của game: máu và hồi chiêu qua `feature-castle`, tiền qua `feature-wallet`, đợt qua `feature-wave`; HUD chỉ hiện như thường (`feature-hud/hud-status/status-spec`, `feature-hud/hud-skill-button/skill-button-spec`).
- Code cheat nằm trong thư mục `Hud`, sau cờ dev; không lọt vào bản phát hành.

## Đã tự quyết
- **Một state duy nhất.** Một kĩ năng đang hồi và hai kĩ năng sẵn sàng trong cùng state nên có đủ hai trạng thái nút; thông báo đợt đang hiện. State khác (đầu lượt) chỉ đổi số hay ẩn bớt phần, không thêm phần nào, nên không tách. Đã loại: thêm state "đầu lượt" (1000/1000, 50, đợt 1).
- **Máu 30%.** Thanh còn ngắn rõ, đọc được cả thanh và số; vẫn hợp lệ ở mọi cấp máu (máu tối đa cấp 1 là 1000).
- **Băng giá còn 12/35 giây.** Lớp phủ 0.34 và số 12 cùng thấy được; Băng giá là kĩ năng hồi lâu nhất.
- **Frame `use` toàn bộ số** từ `feature-hud/hud-layout/layout-spec`, `feature-hud/hud-status/status-spec`, `feature-hud/hud-skill-button/skill-button-spec`. Chỉ tự ghi bề ngang màn 1920 (màn của vòng 1). Thanh nâng cấp không vẽ ở đây (thuộc `feature-hud-upgrade`); state trận đầy đủ gộp nhiều feature để ở cheat của `feature-run`.
- **Ép ở IDEA (2026-11-25):** máu 300/1000, tiền 245, đợt 7, Sấm sét sẵn sàng ép được. Bỏ qua ở IDEA: Băng giá và Cổ vũ (chỉ có Sấm sét, `feature-castle/castle-skills/skill-values`), thông báo "Đợt 7" (không bắt buộc ở `feature-hud/hud-status/status-spec`). Như vậy ở IDEA không ép được trạng thái "đang hồi": thêm Sấm sét đang hồi là đổi state, chưa làm vì PROTOTYPE có đủ. Từ PROTOTYPE dựng đủ state.
- **Ảnh art-concept của HUD chỉ đặt ở node này**, một ảnh cho state `mid-battle`, style `ui-flat`.
- **Bỏ tạm dừng, quảng cáo và popup hồi sinh (2026-11-26, người quyết).** Bỏ state tạm dừng và nút tạm dừng (`hud-pause-button`) khỏi cheat.

## Chưa rõ
- Không có điểm chặn.
