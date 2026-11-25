# Thời điểm từng bước của luồng hồi sinh

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
Dòng thời gian từ lúc thành bị phá lần đầu (t = 0), để đọc cả luồng; mỗi việc là yêu cầu của node ghi ở cột cuối, không phải của node này:
| t (giây) | Việc | Ai làm | Ghi ở |
|---|---|---|---|
| 0 | Thành báo bị phá; trận dừng; bật cờ "đã đề nghị" | `feature-castle`, `feature-run` | `feature-run/run-flow/flow-rules` (`lose-stop-same-moment`, `offer-flag`) |
| 0 – 0.5 | Thành thu nhỏ còn 0 | `feature-castle` | `feature-castle/castle-core/castle-body-values` (`castle-hit-feedback`) |
| 1.0 | Mở `PopupRevive/Main` | `feature-run` nhờ dịch vụ popup | `feature-run/run-flow/flow-rules` (`lose-popup-delay`) |
| (người chơi chọn, có thể xem quảng cáo) | | popup hồi sinh, `feature-ads` | `feature-popup-revive/revive-popup/revive-spec` |
| T | Popup đóng, báo lựa chọn | popup hồi sinh | như trên |
| T – T+0.5 | Hồi sinh: thành phóng lên đủ cỡ, máu 30% | `feature-castle` | `feature-castle/castle-core/castle-body-values` (trường hợp biên hồi sinh) |
| T+0.5 | Trận chạy tiếp; tin dọn sạch đang giữ được thả | `feature-run`, `feature-wave` | `feature-run/run-flow/flow-rules` (`revive-step`, `held-clear`), nhánh dọn đợt của `feature-wave` |
| T (không hồi sinh) | Chốt điểm, mở popup thua ngay | `feature-run` | `feature-run/run-flow/flow-rules` (`revive-step`) |

Việc của popup hồi sinh: ý `no-own-timer`; nội dung và nút ở `feature-popup-revive/revive-popup/revive-spec`.

## Thông số
| Thông số | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Đồng hồ do popup hồi sinh giữ | 0 | | Mọi mốc thời gian thuộc lượt chơi và thành |

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Luồng hồi sinh | Không có: thành bị phá là thua | Có đủ dòng thời gian trên | Như PROTOTYPE |

## Trường hợp biên
- Không có trường hợp biên riêng của popup trong luồng này. Các trường hợp trước đây ghi ở đây đã chuyển về feature làm việc đó (xem "Đã tự quyết").

## Đã tự quyết
- **Tách việc về đúng feature (2026-11-26, người quyết qua @Manager: việc chéo feature thì tách ý về node của feature làm việc đó).** Bản trước có 6 ý và 5 trường hợp biên mà việc thật của feature khác, nên sổ yêu cầu giao cho PopupRevive việc nó không có code:
  - `popup-after-1s`: trùng `lose-popup-delay` của `feature-run/run-flow/flow-rules` (lần bị phá đầu mở popup hồi sinh); bỏ ở đây.
  - `resume-after-anim`, `fail-to-lose-now`: trùng `revive-step` của `flow-rules`; bỏ ở đây.
  - `offer-flag`, `held-clear`: chuyển thành ý của `flow-rules` (lượt chơi giữ cờ và giữ tin).
  - `revive-anim`: chuyển thành trường hợp biên hồi sinh của `feature-castle/castle-core/castle-body-values`.
  - Phần của `feature-wave` trong `held-clear`: sửa trường hợp biên "thành bị phá cùng lúc con cuối bị hạ" ở `feature-wave/wave-clear/clear-rules`.
  - Trường hợp biên hero đang chờ hồi sinh: chuyển về `feature-hero/hero-respawn/respawn-values`.
  - Trường hợp biên Băng giá đang làm chậm: chuyển về `feature-castle/castle-skills/skill-values`.
  - Trường hợp biên lần bị phá thứ hai ngay sau hồi sinh, rời game trong 1 giây trễ: chuyển về `flow-rules`.
  - Trường hợp biên tiền hạ quái cùng khung hình: đã có ở `feature-wallet/income/income-amounts` và `feature-enemy/enemy-reward/reward-values`; bỏ ở đây.
  - Giữ node (một ý của popup) để nhánh thứ tự hồi sinh vẫn có node vòng 4 và bảng dòng thời gian vẫn đọc được ở một chỗ.
- **Không bất tử sau hồi sinh; từ chối thì popup thua hiện ngay; đợt dọn cùng lúc được tính khi hồi sinh.** Các quyết định này giữ nguyên, nay nằm ở node của lượt chơi, thành và đợt.

## Chưa rõ
- Không có điểm chặn.
