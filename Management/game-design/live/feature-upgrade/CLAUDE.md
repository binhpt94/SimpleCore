# Nâng cấp

Dữ liệu cốt lõi: [data.md](data.md)

## Người chơi được gì
- Quyết định chính trong trận: tiêu tiền vào hero nào hay vào thành, và lúc nào.
- Thấy kết quả ngay: bấm là mạnh lên liền, cứu được tình thế đang nguy.

## Liên quan
- `feature-wallet`: trừ tiền.
- `feature-hero`: nâng cấp từng hero.
- `feature-castle`: nâng cấp thành.
- `feature-hud-upgrade`: thanh nút người chơi bấm để mua.
- `feature-run`: mọi nâng cấp mất khi bắt đầu lượt mới.
- `feature-balance`: giá và mức tăng.

## Đã tự quyết
- **Tách thanh nút ra `feature-hud-upgrade`.** Theo luật vòng 2 mới, HUD là feature riêng. Feature này lo mua được gì, giá ra sao, mạnh lên thế nào.
- **Bỏ ý `upgrade-bar`.** Chuyển sang `feature-hud-upgrade`.
- **Bỏ ý `upgrade-reset`.** Đặt lại khi bắt đầu lượt là việc của `feature-run` (`run-reset`), tránh hai feature cùng giữ một việc.
- **Không bán lại, không hoàn tiền.** Giữ đơn giản.
- **Bỏ "4 nút hero + 3 nút thành, tổng 7 nút".** Con số để vòng 4.

## Chưa rõ
- Mỗi hero nâng một mục chung hay từng chỉ số: vòng 3 (hướng hiện tại: một mục chung để ít nút).
