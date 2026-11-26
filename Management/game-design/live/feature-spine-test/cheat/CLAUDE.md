# Cheat: nhân vật thử Spine

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Ở bản dev, chọn state `run-flipped` thì phiên `SpineTest` dựng nhân vật Spine đang run, quay mặt phải, cùng bốn nút và nhãn trên màn trận, đúng chỗ trong frame của `feature-spine-test/spine-checks/spine-spec`.
- Bấm nút sau khi dựng: đổi animation và lật mặt như thường, để thử luôn các luật của node vòng 4.
- Code cheat nằm trong thư mục `SpineTest`, sau cờ dev; không lọt vào bản phát hành.

## Đã tự quyết
- **Một state.** Trạng thái mở màn (idle, mặt trái) chỉ khác giá trị, không thêm thành phần. Đã loại: thêm state idle mặt trái; state đứng ở giữa cú nhảy (chỉ khác tư thế, không thêm thành phần).
- **run, mặt phải**: khác cả animation lẫn mặt so với lúc mở màn, nên một ảnh cho thấy đã đổi animation và đã lật, và nhãn đã đổi chữ.
- **Không cần trận chạy.** Nhân vật không đọc gì của trận.
- **Art-concept (Codex):** chưa đặt. Nhân vật là bộ mẫu Spine có sẵn, nút thử không ra lối chơi; claude-concept của @UIDesigner là đủ.

## Chưa rõ
- Không có điểm chặn.
