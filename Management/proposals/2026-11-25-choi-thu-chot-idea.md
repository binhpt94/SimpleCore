# Chơi thử để chốt IDEA

@CodeLead đã Play thường (Run In Background đã bật): trận chạy sạch từ Loading tới đợt 5, đã đánh IDEA done `auto-shot-values` và `layout-spec`.
Các node còn lại cần có người bấm, vì unity-mcp không giả lập chạm nút. Đề xuất: người chơi một buổi theo danh sách dưới, @CodeLead đọc console, chụp ảnh và đánh tiến độ trong lượt đó.

## Cách chạy
1. Nhắn Manager "bắt đầu chơi thử". Manager báo @CodeLead giữ Editor và theo dõi console.
2. Người bấm Play trong Unity, làm lần lượt các bước dưới, ghi lại bước nào sai.
3. Xong nhắn Manager kết quả; Manager chuyển @CodeLead đánh tiến độ, lỗi thì báo đúng phiên.

## Lượt 1: kĩ năng và nâng cấp
| Bước | Làm | Thấy đúng là | Node |
|---|---|---|---|
| 1 | Chờ quái tới gần thành, bấm nút Sấm sét | quái trong 6u quanh thành mất 80 máu; nút hiện số giây hồi, đếm từ 20 | `castle-skills/skill-values`, `hud-skill-button/skill-button-spec` |
| 2 | Bấm Sấm sét lần nữa khi đang hồi | không có gì xảy ra | `skill-button-spec` |
| 3 | Đủ tiền thì bấm nút nâng một hero (giá 30) | tiền trừ 30; hero phóng to 1.2 lần rồi về trong 0.3 giây; nút lên Cấp 2, giá mới 41 | `hero-stats/hero-level-values`, `upgrade-effect/effect-numbers`, `upgrade-purchase/purchase-rules` |
| 4 | Bấm nâng máu thành hoặc sức đánh thành (giá 40) | tiền trừ 40; thành phóng to 1.1 lần rồi về trong 0.3 giây; máu tối đa +150 hoặc sát thương tự bắn tăng | `castle-upgrade-effects/upgrade-effect-values`, `effect-numbers` |
| 5 | Bấm một nút khi không đủ tiền, và bấm nhanh hai lần | không mua; bấm nhanh chỉ mua một lần | `purchase-rules` |

## Lượt 2 và 3: thua và chơi lại
| Bước | Làm | Thấy đúng là | Node |
|---|---|---|---|
| 6 | Không mua gì, để thành bị phá (lâu thì chờ; xem ghi chú) | 1 giây sau hiện popup thua: số đợt đã dọn, kỉ lục; chỉ có nút "Chơi lại" | `run-flow/flow-rules`, `run-score/score-rules`, `lose-result/result-spec`, `lose-actions/actions-spec` |
| 7 | Bấm "Chơi lại" hai lần thật nhanh | vào trận mới một lần, đội mặc định Hiệp sĩ, Đấu sĩ rìu, Cung thủ, tiền 50, đợt 1 | `flow-rules`, `actions-spec`, `run-start` |
| 8 | Thua thêm một lượt, chơi lại lần nữa (tổng 3 lượt) | mỗi lượt bắt đầu sạch: không còn quái, popup, hero cũ; console không lỗi mới | `session-clean/clean-checks` |
| 9 | Lượt có điểm cao hơn kỉ lục | popup hiện "Kỉ lục mới!" | `score-rules`, `result-spec` |

## Cheat (sau 3 lượt trên, mỗi lần một cheat)
Manager nhắn phiên sở hữu bật, người Play xem, phiên đó tắt lại: Hud `mid-battle`, HudUpgrade `mixed-buttons`, PopupLose `new-record`, Popup `sample-popup` (bấm "Đóng"), SceneFlow `load-error`.

## Ghi chú
- Không mua gì thì tới đợt 5 thành vẫn 952/1000; chờ thua có thể lâu. Cheat `new-record` của PopupLose mở thẳng popup thua, dùng được cho bước 6, 7, 9 nếu không muốn chờ.
- Console có 5 lỗi engine "PlayerLoop internal function has been called recursively", không có stack; @CodeLead chưa thấy lỗi từ code feature. Người để ý xem lỗi có lặp khi chơi không.
- Cách khác: thêm cheat tự bấm, tự mua ở bản dev để @CodeLead tự kiểm. Cách này là đổi thiết kế cheat (qua @GameDesigner và người chốt) rồi mới code, chậm hơn chơi thử một buổi.
