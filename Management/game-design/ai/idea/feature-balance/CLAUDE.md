# Cân bằng số liệu

Dữ liệu cốt lõi: [data.md](data.md)

## Người chơi được gì
- Áp lực tăng đều, không quá dễ, không gãy đột ngột.
- Mỗi lựa chọn (đội, nâng cấp) đều có ý nghĩa.

## Liên quan
- `feature-hero`, `feature-castle`, `feature-enemy`, `feature-wave`, `feature-upgrade`, `feature-wallet`: các feature giữ số liệu.

## Đã tự quyết
- **Balance là feature riêng.** Luật vòng 2: balance cắt ngang nhiều feature.
- **Số liệu nằm trong cấu hình của từng feature, không gom vào một chỗ dùng chung.** Giữ các feature độc lập theo luật của project. Feature này là nơi quyết và đối chiếu các số đó.

- **Đưa góp ý GPT lần hỏi `r2-1` (2026-11-24).**
  - `balance-recovery` (AI 7): đưa cảm giác chủ đạo của vòng 1 vào cân bằng.
  - `balance-team-fair` viết lại theo `balance-team-matchups` (AI 7): nói đội nào mạnh trước kiểu sức ép nào, giữ câu "không đội nào luôn thắng" ở chi tiết.
  - `balance-run-pressure` (thay `balance-curve`), AI 5, đặt cạnh ý cũ: hướng đúng, nhưng bỏ "lượt nào rồi cũng thua" trong khi chơi vô tận cần điểm gãy và nhánh dự đoán đợt gãy dựa vào nó. `balance-curve` giữ AI 7.
  - `balance-shared-tuning` (thay `balance-tunable`), AI 6, đặt cạnh ý cũ: rõ hơn phần đối chiếu giữa các feature, nhưng bỏ "chỉnh số không sửa code" là luật project vẫn cần. `balance-tunable` giữ AI 7.

- **Người chấm cặp ý trái nhau (2026-11-24): `balance-curve` 2, `balance-tunable` 2.** Xoá hai ý đó; giữ `balance-run-pressure` và `balance-shared-tuning` của GPT. Hệ quả: "lượt nào rồi cũng thua" và "chỉnh số không sửa code" không còn là ý của node này; việc chỉnh số trong cấu hình vẫn là luật project, và điểm gãy vẫn có ở nhánh dự đoán đợt gãy.

## Chưa rõ
- Có công cụ xem chung các số (bảng, mô phỏng) hay không: vòng 3.
