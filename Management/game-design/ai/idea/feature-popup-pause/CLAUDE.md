# Popup tạm dừng

Dữ liệu cốt lõi: [data.md](data.md)

## Người chơi được gì
- Bị ngắt giữa chừng (điện thoại, tin nhắn) mà không mất lượt.
- Thoát lượt đang dở khi muốn.

## Liên quan
- `feature-hud`: nút tạm dừng.
- `feature-run`: tạm dừng và bỏ lượt.
- `feature-popup`: dịch vụ hiện popup.

## Đã tự quyết
- **Là feature riêng.** Luật vòng 2: mỗi popup là một feature.
- **Bỏ lượt vẫn giữ điểm các đợt đã dọn.** Điểm là số đợt đã dọn sạch (`feature-run`); bỏ lượt giống thua, chỉ đợt dở không tính. Đã loại: bỏ lượt thì không tính điểm.
- **Không có cài đặt âm thanh trong popup.** Vòng 1 chưa nhắc; thêm khi người yêu cầu.

## Chưa rõ
- Game tự tạm dừng khi chuyển sang ứng dụng khác hay không: vòng 3.
