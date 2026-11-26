# Bắt đầu lượt

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- Một lần vào trận là một lần bắt đầu lượt.
- Feature này báo lượt mới cho: `feature-castle` (máu, kĩ năng, cấp của thành), `feature-wallet` (tiền), `feature-upgrade` (mọi cấp nâng cấp), `feature-wave` (số đợt), `feature-hero` (đội và cấp từng hero).
- `feature-team-select` đưa đội đã chọn cho feature này; feature này đưa tiếp cho `feature-hero`.
- `feature-balance` chỉnh giá trị ban đầu; feature này không biết giá trị đó.

## Định nghĩa
- Không dùng thuật ngữ riêng.

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
- **Chơi lại không tải lại scene; đặt lại bằng lệnh "lượt mới" (2026-11-23, sửa lệch).** Bản cũ ghi scene trận tải mới mỗi lượt, trái với `feature-scene-flow/scene-list` (mọi màn trong scene Game, không chuyển scene giữa các lượt) và `feature-run/run-flow`. Trận dựng mới và huỷ hết mỗi lượt bên trong scene Game; mọi feature tự đặt lại khi nhận lệnh "lượt mới". Đã loại: tải lại scene mỗi lượt.

## Chưa rõ
- Không có điểm chặn.
