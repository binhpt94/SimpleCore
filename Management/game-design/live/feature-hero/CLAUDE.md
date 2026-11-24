# Hero

Dữ liệu cốt lõi: [data.md](data.md)

## Người chơi được gì
- Xem đội của mình tự đánh và thấy rõ khi chúng mạnh lên.
- Lí do chơi lại: mỗi hero một cách đánh, đội khác thì trận khác.

## Liên quan
- `feature-team-select`: chọn hero nào vào đội.
- `feature-castle`: hero xuất phát và hồi sinh ở thành.
- `feature-enemy`: hero đánh quái, quái đánh hero.
- `feature-upgrade`: nâng cấp từng hero.
- `feature-balance`: số liệu của từng hero.

## Đã tự quyết
- **Tách màn chọn đội ra `feature-team-select`.** Theo luật vòng 2 mới, mỗi feature phải giao riêng được cho một phiên; màn chọn đội là một màn hình riêng trong luồng lượt chơi. Đã loại: để chung trong feature này (bản trước).
- **Hero không có kĩ năng chủ động.** Kĩ năng người chơi bấm nằm ở thành (`feature-castle`). Đã loại: hero có nút kĩ năng.
- **Hero gục giữ nguyên nâng cấp.** Không phạt nặng việc hero gục. Đã loại: mất cấp khi gục.
- **Bỏ chi tiết chỉ số (máu, sức đánh, tầm đánh...) và cách tìm mục tiêu.** Đó là định nghĩa, để vòng 3.
- **Bỏ chữ "bản đầu dự kiến 6 hero" và "dự kiến 4".** Con số để vòng 4, không ghi theo bản phát hành.

- **Đưa góp ý GPT lần hỏi `r2-3` (cụm giao chiến, 2026-11-24).** 13 góp ý "thêm". Chia việc giao chiến: bên ra đòn chọn mục tiêu, bên trúng và sát thương; bên trúng tự trừ máu và báo hết máu đúng một lần (AI 7).
  - Ba ý cần `feature-combat` (`combat-arbiter`, `same-tick-snapshot`, `same-tick-sum`, `report-after-hp`) AI 5: hợp lí cho kết quả nhất quán, nhưng thêm một nơi ba feature cùng đi qua; xem `feature-combat`.
  - `shared-target-word` (AI 6): mở rộng nghĩa từ "Mục tiêu" trong bảng thuật ngữ cho cả hero và thành bị nhắm; chưa sửa bảng thuật ngữ, xét khi rà vòng 3.
  - **Loại `tinh-dot-cuoi-khi-thanh-vo`, AI 4:** tính đợt dọn sạch cùng lúc thành vỡ, trái luật "thua trước" đã chốt. Khai `gpt-dropped`.
- `hero-spawn-team` lấy từ câu hỏi chung vòng 2 (`sinh-doi-dau-tran`), không nhãn `by:`.

## Chưa rõ
- Danh sách hero cụ thể và cách đánh của từng hero: vòng 3.
