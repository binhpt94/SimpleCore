# Thông tin trận

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- Một thanh máu cho một thành; một ô tiền; một ô số đợt.
- `feature-castle` báo máu hiện tại và máu tối đa; `feature-wallet` báo số tiền; `feature-wave` báo đợt đang đánh.
- Vị trí trên màn hình ở `feature-hud/hud-layout`.

## Định nghĩa
- **Máu thành**: độ bền còn lại của thành (`feature-castle/castle-health`); về không là thành bị phá.
- **Máu tối đa**: máu khi thành đầy; nâng cấp độ bền làm nó lớn hơn (`feature-castle/castle-upgrades`).
- **Đợt đang đánh**: đợt quái hiện tại của lượt, tính cả lúc thở giữa hai đợt là đợt sắp tới.

## Danh sách
- Thanh máu thành.
- Số tiền kèm biểu tượng tiền.
- Số thứ tự đợt đang đánh.

## Luật cơ bản
- Thanh máu dài theo tỉ lệ máu hiện tại trên máu tối đa.
- Thông tin đổi ngay khi feature chủ báo đổi.
- HUD không nhận thao tác trên ba thông tin này.

## Đã tự quyết
- **Thanh máu thay vì chỉ con số.** Người chơi phổ thông đọc thanh nhanh hơn. Có kèm con số hay không: vòng 4.
- **Lúc thở giữa hai đợt hiện đợt sắp tới.** Đợt cũ đã dọn; người chơi cần biết sắp gặp đợt mấy. Đã loại: hiện đợt vừa dọn.

## Chưa rõ
- Cách `feature-wave` gọi đợt trong lúc thở cần thống nhất với vòng 3 của `feature-wave`.
