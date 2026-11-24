# Nhớ đội lượt trước

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- Một đội được nhớ cho cả game đang mở.
- Đội được nhớ đổ vào các chỗ của `feature-team-select/team-slots` khi màn mở.
- Đội được nhớ cập nhật khi người chơi bấm vào trận.
- Không liên quan tới `feature-run/run-start`: đặt lại đầu lượt không xoá đội được nhớ.

## Định nghĩa
- `glossary#remembered-team`

## Danh sách
- Đội được nhớ (các hero theo chỗ).

## Luật cơ bản
- Bấm vào trận: đội vừa chọn thành đội được nhớ.
- Mở màn chọn đội: các chỗ lấy theo đội được nhớ.
- Tắt game: đội được nhớ mất.

## Đã tự quyết
- **Chỉ nhớ trong lúc game mở.** Giữ đúng quyết định chung "kỉ lục là thứ duy nhất giữ qua khi tắt game". Đã loại: lưu đội vào máy.
- **Cập nhật lúc vào trận, không cập nhật lúc chọn dở.** Đội được nhớ luôn là một đội đủ đã từng đánh.
- **Màn chọn đội không có nút quay lại màn bắt đầu.** Luồng ở `feature-run/run-flow` chỉ đi tới trận. Đã loại: nút quay lại (thêm một đường chuyển màn không cần cho vòng lặp).
  **Lệch đã ghi (2026-11-25):** `feature-game-flow` (vòng 2, ý `back-from-team`) thêm nút "Quay lại" từ PROTOTYPE, không đổi đội được nhớ. Khi vào PROTOTYPE sẽ sửa quyết định này và frame của nhánh chỗ trong đội.

## Chưa rõ
- Mở game lần đầu, chưa có đội được nhớ, thì các chỗ trống hay xếp sẵn một đội: vòng 4.
