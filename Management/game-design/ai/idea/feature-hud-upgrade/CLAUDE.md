# Thanh nâng cấp

Dữ liệu cốt lõi: [data.md](data.md)

## Người chơi được gì
- Mua nâng cấp ngay giữa lúc đánh, không mất nhịp.
- Biết ngay mình mua được gì với số tiền đang có.

## Liên quan
- `feature-upgrade`: mua được gì, giá bao nhiêu, mua xong ra sao.
- `feature-wallet`: đủ tiền hay chưa.
- `feature-hud`: phần còn lại của HUD trận; hai bên chia mép màn hình.

## Đã tự quyết
- **Tách khỏi `feature-upgrade` và `feature-hud`.** Luật vòng 2: HUD là feature riêng, HUD lớn thì tách cụm lớn. Ý `upgrade-bar` cũ của `feature-upgrade` chuyển sang đây.
- **Thanh cố định, không mở bảng.** Trận không dừng nên phải mua bằng một chạm mà không che màn chơi.

## Chưa rõ
- Thanh nằm ở mép nào, xếp nút thế nào: vòng 3.
