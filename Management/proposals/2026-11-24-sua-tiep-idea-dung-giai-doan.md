# Sửa đề xuất "Làm tiếp mức IDEA theo claude-concept": bỏ việc không thuộc IDEA

Đề xuất `2026-11-24-tiep-idea-theo-claude-concept.md` giao nhầm một số việc của PROTOTYPE và FULL vào IDEA. PopupPause đã chỉ ra. File này thay các dòng sai của bảng "Việc mỗi phiên"; các dòng còn lại giữ nguyên.

## Bảng giai đoạn đã chốt (live)
| Node | IDEA |
|---|---|
| `feature-popup-pause/pause-popup/pause-popup-spec` | không có popup tạm dừng |
| `feature-run/run-pause/pause-rules` | không có nút tạm dừng, popup tạm dừng hay bỏ lượt |
| `feature-hud/hud-pause-button/pause-button-spec` | không có nút tạm dừng |
| `feature-hud/hud-layout/layout-spec` | chỉ có máu, tiền, đợt và 3 nút kĩ năng |
| `feature-home/home-screen/home-screen-spec` | không có màn bắt đầu, vào thẳng trận |
| `feature-team-select/*` (card-spec, slots-spec, memory-rules) | không có màn chọn đội, đội mặc định cố định |
| `feature-run/run-flow/flow-rules` | vào thẳng trận, không có Về màn bắt đầu, không có Bỏ lượt |

## Dòng thay thế
| Phiên | Việc ở IDEA |
|---|---|
| PopupPause | không làm; chờ PROTOTYPE |
| TeamSelect | không làm; chờ PROTOTYPE |
| Home | không làm; chờ PROTOTYPE |
| Hud | không làm nút tạm dừng; chỉ cheat của Hud và sprite cho máu, tiền, đợt, nút kĩ năng theo claude-concept |
| Run | không làm luật tạm dừng; chỉ xong luồng và điểm đang `doing` |

## Còn lại
- Ngày chuyển sang PROTOTYPE do người quyết; tới lúc đó các phiên trên mới làm.
