# Dọn sạch đợt

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- MỘT đợt dọn sạch cho MỘT khoản thưởng và MỘT điểm.
- Thưởng dọn đợt cộng thêm vào tiền hạ quái (`feature-enemy`); cả hai vào `feature-wallet`.
- Điểm của lượt là số đợt dọn sạch, do `feature-run` giữ; node này chỉ báo.
- Dọn sạch xong thì giao cho `feature-wave/wave-gap`.
- Mức thưởng so với giá nâng cấp là của `feature-balance/balance-economy`.

## Định nghĩa
- `glossary#wave-clear`
- `glossary#score` (số đợt đã dọn sạch)

## Danh sách
Khi dọn sạch đợt, ba việc xảy ra:
- Cộng thưởng dọn đợt vào `feature-wallet`.
- Số đợt đã dọn sạch tăng một (báo `feature-run`).
- Chuyển sang lúc thở (`feature-wave/wave-gap`).

## Luật cơ bản
- Chỉ xét dọn sạch khi quái của đợt đã ra hết.
- Thưởng dọn đợt tăng theo số đợt.
- Thưởng như nhau dù thành còn nhiều hay ít máu, dù hero nào còn sống.
- Thành bị phá thì lượt kết thúc; đợt đang đánh không được tính là dọn sạch.

## Đã tự quyết
- **Thưởng chỉ theo số đợt.** Dễ hiểu, không phạt thêm người đang yếu. Đã loại: thưởng theo máu thành còn lại.
- **Điểm do `feature-run` giữ.** Tránh hai feature cùng giữ điểm.
- **Sang lúc thở thay vì màn nghỉ.** Nâng cấp mua được mọi lúc (vòng 1), nên không cần màn riêng.
- **Viết lại theo luật vòng 3 mới (2026-11-23).** Bỏ công thức thưởng, thời gian hiện chữ, trường hợp biên (thua cùng lúc dọn sạch, tạm dừng), bỏ hiệu ứng hiển thị (thuộc `feature-hud`).

## Chưa rõ
- Không có điểm chặn.
