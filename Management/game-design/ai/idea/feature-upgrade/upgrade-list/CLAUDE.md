# Danh sách nâng cấp

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- MỘT hero trong đội có MỘT mục nâng cấp; hero không ở trong đội thì không có mục.
- MỘT thành có BA mục nâng cấp.
- Đội lấy từ `feature-team-select` qua `feature-run` khi vào trận.
- Mỗi mục có MỘT cấp; nhiều lần mua thì cấp tăng nhiều lần.
- Mua xong thì gì xảy ra: `feature-upgrade/upgrade-effect`. Mua thế nào, giá ra sao: `feature-upgrade/upgrade-purchase`.
- Nút bấm cho từng mục: `feature-hud-upgrade`.

## Định nghĩa
- **Mục nâng cấp**: một thứ mua được nhiều lần, mỗi lần làm một hero hoặc một mặt của thành mạnh hơn.
- **Cấp**: số lần mục đó đã được mua trong lượt, tính từ cấp thấp nhất.
- **Cấp hero**: mục nâng cấp của một hero, nâng mọi chỉ số của hero đó cùng lúc.

## Danh sách
- Mục hero: "cấp hero" cho từng hero trong đội.
- Mục thành:
  - Máu thành.
  - Sức đánh của thành (đòn tự bắn).
  - Kĩ năng của thành (một mục chung cho mọi kĩ năng thành).

## Luật cơ bản
- Vào trận, mọi mục ở cấp thấp nhất.
- Danh sách không đổi trong suốt lượt.
- Mua một mục chỉ tăng cấp mục đó, không ảnh hưởng mục khác.
- Lượt mới thì mọi mục về cấp thấp nhất (`feature-run` đặt lại).

## Đã tự quyết
- **Một mục chung cho mỗi hero.** Trả lời câu "Chưa rõ" ở vòng 2 theo hướng đã nêu: ít nút, thanh nâng cấp gọn. Đã loại: nâng từng chỉ số của hero.
- **Một mục kĩ năng chung cho mọi kĩ năng thành.** Giữ đúng ba mục thành như quyết định chung; mỗi kĩ năng mạnh lên theo cách riêng (xem `upgrade-effect`). Đã loại: mỗi kĩ năng một mục.
- **Danh sách theo đội đã chọn.** Hero không ở trong đội thì không có gì để nâng.

## Chưa rõ
- Không có điểm chặn.
