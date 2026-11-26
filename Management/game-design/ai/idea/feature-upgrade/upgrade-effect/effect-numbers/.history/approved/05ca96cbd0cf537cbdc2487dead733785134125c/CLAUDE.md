# Báo lên cấp cho bên giữ chỉ số

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Lần mua được chấp nhận (`feature-upgrade/upgrade-purchase/purchase-rules`: trừ tiền xong, tăng cấp): báo "mục X lên cấp L" trong cùng khung hình.
  - Mục hero: báo `feature-hero`, kèm chỗ của hero đó trong đội.
  - Mục máu thành, sức đánh thành, kĩ năng thành: báo `feature-castle`, kèm tên mục.
- Bên nhận tự tính chỉ số theo cấp mới và áp ngay; nâng cấp không giữ, không tính chỉ số.
- Nhiều lần mua trong một khung hình: mỗi lần một lời báo, theo thứ tự mua.

## Thông số
| Thông số | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Số lời báo mỗi lần mua | 1 | lời báo | `report-each-level` |
| Mức tăng cấp mỗi lời báo | 1 | cấp | Mỗi lần mua một cấp |
| Độ trễ từ lúc mua tới lúc báo | 0 | khung hình | `report-same-frame` |
| Bên nhận mục hero | `feature-hero` | | `report-right-owner` |
| Bên nhận mục máu, sức đánh, kĩ năng thành | `feature-castle` | | `report-right-owner` |

Mức tăng mỗi cấp (tham khảo, không phải của node này):
- Hero: máu và sát thương + 15% cấp 1 mỗi cấp, tốc độ đánh + 3% mỗi cấp tối đa 60% (`feature-hero/hero-stats/hero-level-values`).
- Thành: máu tối đa + 150 mỗi cấp, sát thương tự bắn + 20% cấp 1, ba kĩ năng + 20% hiệu lực cấp 1, hồi chiêu không đổi (`feature-castle/castle-upgrade-effects/upgrade-effect-values`).

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Báo lên cấp cho hero | có | có | có |
| Báo lên cấp cho thành (máu, sức đánh, kĩ năng) | có | có | có |

## Trường hợp biên
- **Mua cấp cho hero đang gục**: vẫn báo ngay; `feature-hero` chỉ đổi cấp và dùng cấp mới khi hồi sinh (`hero-level-values`).
- **Mua nhiều cấp liên tiếp trong một khung hình**: báo từng cấp một (2, rồi 3...), không gộp thành một lời báo "lên cấp 3".
- **Lần mua bị từ chối** (không đủ tiền, đang tạm dừng, lượt đã kết thúc): không báo gì.
- **Lượt mới bắt đầu**: cấp về 1 do lệnh đặt lại của `feature-run`; nâng cấp không gửi lời báo "lên cấp 1".

## Đã tự quyết
- **Viết lại node chỉ còn luật báo lên cấp (2026-11-25, người chọn hướng A).** Bản trước chép công thức cấp hero, mức tăng máu, sức đánh, kĩ năng thành và các trường hợp biên của chúng, trong khi node cha ghi "feature này chỉ báo mục X lên cấp Y" và "không giữ chỉ số". Sổ yêu cầu vì vậy giao các con số cho Upgrade dù Upgrade không có code cho chúng. Các ý `hero-plus-15`, `hero-speed-3`, `hero-hp-gain`, `hero-fallen`, `castle-hp-150`, `castle-attack-20`, `castle-skill-20` bỏ khỏi node này; luật đó vẫn có ở `hero-level-values` (`level-formula`, `level-hp-gain`) và `upgrade-effect-values` (`hp-per-level`, `attack-per-level`, `skill-per-level`). Đã loại: giữ bản chép (hai nơi cùng một luật, sẽ lệch).
- **Báo từng cấp, không gộp.** Bên nhận chỉ cần một luật "lên 1 cấp"; khớp trường hợp biên "mua nhiều cấp liên tiếp" của `hero-level-values`. Đã loại: báo cấp cuối cùng một lần.
- **Không báo khi đặt lại lượt.** Việc đặt lại thuộc `feature-run`; mỗi feature tự về trạng thái ban đầu.

## Chưa rõ
- Hai đề xuất cho người ở bản trước (Băng giá không có trần thời gian chậm, Cổ vũ hồi đầy từ cấp 13) thuộc nhánh thành; chúng vẫn đúng với số ở `upgrade-effect-values` nhưng không còn ghi ở node này.
