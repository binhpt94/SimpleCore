# Cheat: popup thua

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Ở bản dev, chọn state `new-record` thì game dựng ngay popup thua với các giá trị ép, không cần chơi tới lúc thành bị phá.
- Giá trị ép đi qua đúng đường của game: điểm, kỉ lục, cờ kỉ lục mới như `feature-run/run-score/score-rules` sẽ chốt; popup đọc chúng như bình thường (`feature-popup-lose/lose-result/result-spec`).
- Code cheat nằm trong thư mục `PopupLose`, sau cờ dev; không lọt vào bản phát hành.

## Đã tự quyết
- **Một state duy nhất: phá kỉ lục.** State này có đủ mọi thành phần của popup (thêm dòng "Kỉ lục mới!"). State "không phá kỉ lục" chỉ ẩn bớt dòng đó nên không tách riêng theo luật node cheat. Đã loại: hai state thường và phá kỉ lục.
- **Frame `use` toàn bộ số** từ `result-spec` (các dòng chữ), `actions-spec` (hai nút), `feature-popup/popup-layer/stack-rules` (khung popup), `feature-hud/hud-layout/layout-spec` (cao nút). Không tự ghi số nào ngoài kích thước nền che toàn màn 1920x1080 (màn của vòng 1).
- **Ảnh art-concept của popup thua chỉ đặt ở node này**, một ảnh cho state `new-record`, style `ui-flat`.

## Chưa rõ
- Không có điểm chặn.
