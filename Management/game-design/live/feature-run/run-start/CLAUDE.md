# Bắt đầu lượt

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- Một lần vào trận là một lần bắt đầu lượt.
- Feature này báo lượt mới cho: `feature-castle` (máu, kĩ năng, cấp của thành), `feature-wallet` (tiền), `feature-upgrade` (mọi cấp nâng cấp), `feature-wave` (số đợt), `feature-hero` (đội và cấp từng hero).
- `feature-team-select` đưa đội đã chọn cho feature này; feature này đưa tiếp cho `feature-hero`.
- `feature-balance` chỉnh giá trị ban đầu; feature này không biết giá trị đó.

## Định nghĩa
- **Trạng thái ban đầu**: trạng thái mỗi feature có khi chưa có gì xảy ra trong trận.
- **Đội của lượt**: các hero người chơi đã chọn ở màn chọn đội ngay trước trận.

## Danh sách
Thứ được đặt lại đầu lượt:
- Máu thành: đầy.
- Tiền: mức ban đầu.
- Cấp nâng cấp của từng hero và của thành: cấp ban đầu.
- Số đợt đã dọn: chưa có đợt nào.
- Kĩ năng thành: trạng thái ban đầu.

Thứ không đặt lại:
- Kỉ lục (`feature-run/run-score`).
- Đội nhớ ở màn chọn đội (`feature-team-select`), chỉ là lựa chọn sẵn, không phải trạng thái của trận.

## Luật cơ bản
- Mọi thứ trong danh sách đặt lại đều xong trước khi đợt đầu tiên tới.
- Trong trận không đổi đội.

## Đã tự quyết
- **Kĩ năng thành cũng về ban đầu.** Quyết định chung chỉ nêu máu, tiền, cấp, số đợt; thêm kĩ năng cho trọn ý "bắt đầu lại từ đầu". Có sẵn hay phải chờ hồi lúc đầu lượt là việc của `feature-castle`.
- **Mỗi feature tự giữ trạng thái ban đầu.** Tránh trùng việc; khớp `feature-upgrade` (đã bỏ `upgrade-reset`, để việc đặt lại cho feature này ra lệnh).
- **Scene trận tải mới mỗi lượt** (`feature-run/run-flow`), nên đặt lại phần lớn đến từ việc dọn scene của `feature-session-scene`; lệnh "lượt mới" vẫn có để luật không phụ thuộc vào cách chia scene.

## Chưa rõ
- Không có điểm chặn.
