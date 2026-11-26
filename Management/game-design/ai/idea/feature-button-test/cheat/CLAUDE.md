# Cheat: nút thử

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Ở bản dev, chọn state `pressed-12` thì phiên `ButtonTest` dựng nút "Thử" và nhãn "Đã bấm: 12" trên màn trận, đúng chỗ trong frame của `feature-button-test/test-button/button-spec`.
- Bấm nút sau khi dựng: N tăng tiếp từ 12 (13, 14…), để thử luôn luật `tap-adds-one`.
- Code cheat nằm trong thư mục `ButtonTest`, sau cờ dev; không lọt vào bản phát hành.

## Đã tự quyết
- **Một state.** Feature chỉ có một nút và một nhãn; state "Đã bấm: 0" chỉ khác số, không thêm thành phần. Đã loại: thêm state N = 0.
- **N = 12.** Hai chữ số cho thấy nhãn không còn ở giá trị đầu và chữ dài ra vẫn vừa chỗ. Đã loại: N = 1 (dễ lẫn với bấm nhầm một lần).
- **Không cần trận chạy.** Nút không đọc gì của trận; HUD chỉ vẽ để thấy chỗ không chồng nhau.
- **Art-concept (Codex):** chưa đặt. Nút thử không ra lối chơi; claude-concept của @UIDesigner là đủ.

## Chưa rõ
- Không có điểm chặn.
