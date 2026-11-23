# Mua nâng cấp

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- MỘT chạm trên `feature-hud-upgrade` là MỘT yêu cầu mua MỘT cấp của MỘT mục.
- MỘT lần mua là MỘT lần xin `feature-wallet` trừ tiền (`feature-wallet/balance-spend`).
- Mua thành công thì mục lên cấp và hiệu lực theo `feature-upgrade/upgrade-effect`.
- Trạng thái tạm dừng do `feature-run` giữ (`run-pause`).
- Giá theo cấp của từng mục do `feature-balance` quyết.

## Định nghĩa
- Không dùng thuật ngữ riêng.

## Danh sách
- Điều kiện để mua thành công: đang trong trận, không tạm dừng, ví trừ được giá cấp sau.
- Thông tin mỗi mục đưa ra cho thanh nâng cấp: cấp hiện tại, giá cấp sau, mua được hay không.

## Luật cơ bản
- Mua thành công: ví trừ giá cấp sau, mục lên một cấp, giá cấp sau mới cao hơn giá vừa trả.
- Không đủ tiền: không trừ tiền, không lên cấp.
- Đang tạm dừng: không mua được.
- Không có cấp tối đa; mục nào cũng mua tiếp được nếu đủ tiền.
- Cấp đã mua không bán lại, không hoàn tiền.
- Mỗi khi cấp hoặc số dư đổi, thông tin "mua được hay không" của mọi mục được cập nhật cho thanh nâng cấp.

## Đã tự quyết
- **Không mua lúc tạm dừng.** Theo hướng của `feature-run` ở vòng 2: giữ áp lực chọn giữa trận. Đã loại: mua thoải mái lúc dừng.
- **Giá tăng theo cấp của từng mục, không theo tổng số lần mua.** Người chơi đọc được giá của từng nút độc lập; không bị phạt vì mua đều. Đã loại: giá tăng theo tổng số lần mua.
- **Feature này giữ giá, ví không biết.** Ví chỉ trừ con số được đưa.

## Chưa rõ
- Không có điểm chặn.
