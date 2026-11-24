# Cheat: popup tạm dừng

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Ở bản dev, chọn một state thì game dựng ngay trận đứng yên với popup tạm dừng đang mở ở bước tương ứng, không cần vào trận rồi bấm tạm dừng.
- Giá trị ép đi qua đúng đường của game: trận vào tạm dừng như `feature-run/run-pause/pause-rules`; popup mở và đổi bước như `feature-popup-pause/pause-popup/pause-popup-spec`.
- Code cheat nằm trong thư mục `PopupPause`, sau cờ dev; không lọt vào bản phát hành.

## Đã tự quyết
- **Hai state: bước chính và bước hỏi.** Hai bước có thành phần khác nhau (bước chính có tiêu đề "Tạm dừng" và nút "Tiếp tục"; bước hỏi có câu hỏi và nút "Không"); không bước nào chỉ ẩn bớt phần của bước kia, nên tách hai state theo luật node cheat. Đã loại: chỉ một state bước hỏi (thiếu tiêu đề và nút "Tiếp tục").
- **Đợt 5 khi tạm dừng.** Chỉ để trận phía sau có quái, hero đang đứng yên; số đợt không hiện trên popup.
- **Frame `use` toàn bộ số** từ `pause-popup-spec` (tiêu đề, câu hỏi, hai nút), `feature-popup/popup-layer/stack-rules` (khung popup), `feature-hud/hud-layout/layout-spec` (cao nút). Không tự ghi số nào ngoài nền che toàn màn 1920x1080 (màn của vòng 1).
- **Không dùng ở IDEA (2026-11-25).** IDEA chưa có tạm dừng (`feature-run/run-pause/pause-rules`, `feature-popup-pause/pause-popup/pause-popup-spec`); cheat có từ PROTOTYPE.
- **Ảnh art-concept của popup tạm dừng chỉ đặt ở node này**, mỗi state một ảnh, style `ui-flat`.

## Chưa rõ
- Không có điểm chặn.
