# Lượt chơi

Dữ liệu cốt lõi: [data.md](data.md)

## Người chơi được gì
- Vào trận nhanh, thua thì chơi lại ngay với đội khác.
- Kỉ lục số đợt để có lí do chơi lại.

## Liên quan
- `feature-home`: màn bắt đầu.
- `feature-team-select`: màn chọn đội; đội vừa chọn vào trận.
- `feature-castle`: báo khi thành bị phá.
- `feature-wave`: số đợt đã dọn; dừng thả quái khi lượt kết thúc.
- `feature-wallet`, `feature-upgrade`, `feature-hero`: đặt lại khi bắt đầu lượt.
- `feature-popup-lose`, `feature-popup-pause`: hai popup của lượt chơi.
- `feature-scene-flow`: chuyển scene nếu các màn nằm ở scene khác nhau.

## Đã tự quyết
- **Tách màn bắt đầu, popup thua, popup tạm dừng thành feature riêng.** Theo luật vòng 2 mới: mỗi popup là một feature; màn bắt đầu là màn hình riêng giao được cho một phiên. Feature này chỉ nối chúng theo thứ tự và giữ dữ liệu của lượt.
- **Bỏ ý `run-lose-screen`, thêm ý `run-replay`.** Nội dung popup thua chuyển sang `feature-popup-lose`; còn lại quyết định "chơi lại về màn chọn đội".
- **Không có hồi sinh khi thua.** Không có quảng cáo hay tiền lâu dài. Đã loại: hồi sinh một lần.
- **Lúc tạm dừng có mua nâng cấp được không:** hướng hiện tại là không (giữ áp lực mua giữa trận); luật này để vòng 3.

## Chưa rõ
- Các màn nằm chung một scene hay tách scene: vòng 3, cùng `feature-scene-flow`.
