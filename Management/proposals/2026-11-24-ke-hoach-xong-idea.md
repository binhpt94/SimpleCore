# Kế hoạch xong IDEA, chạy song song

Người giao (2026-11-24): Manager tự vận hành để xong IDEA sớm, việc nào song song được thì làm song song.
Thay cho bảng việc ở `2026-11-24-tiep-idea-theo-claude-concept.md` và `2026-11-24-sua-tiep-idea-dung-giai-doan.md`.

## Tình trạng (16:31)
- Test EditMode 457/457 qua. Play vào trận và sang đợt 2 không lỗi.
- IDEA `done`: Wallet, Wave, Enemy, SceneFlow, phần lớn Hero, Popup (stack-rules, request-rules), SessionScene (participant-order), Run (start-values), Upgrade (item-set), Castle (castle-body), Hud (status), HudUpgrade (bar-buttons, button-display).
- Còn `doing` hoặc `todo`: đã đủ code nhưng chờ @CodeLead Play kiểm hình ở Game view ngang, hoặc chờ việc bên dưới.
- Không thuộc IDEA (bảng giai đoạn ghi "không có"): PopupPause, Home, TeamSelect, nút tạm dừng của Hud, luật tạm dừng của Run. @CodeLead xét đánh tiến độ IDEA cho các node này theo đúng bảng.

## Việc song song
| Phiên | Việc | Chờ ai |
|---|---|---|
| Enemy | chấm và gắn art Codex `Enemy/enemy-sprites` v1 (5 loại quái), giữ kích thước theo thiết kế | không |
| Hero | chấm và gắn art Codex `Hero/hero-sprites` v1; kiểm hiệu ứng lên cấp (phóng to 1.2 lần, 0.3 giây ở IDEA) | không |
| HudUpgrade | chấm và gắn art Codex `HudUpgrade/bar-icons` v1; làm cheat theo node `feature-hud-upgrade/cheat` | không |
| PopupLose | sprite khung và nút theo claude-concept (order @ClaudeImage); kiểm cheat theo node `feature-popup-lose/cheat` | font: người |
| Hud | đặt mỗi nút kĩ năng vào ô cố định theo kĩ năng, ô của kĩ năng không có thì để trống; đúng cả khi có 1 hay 3 kĩ năng | không (Sync chỉ để đối chiếu) |
| SceneFlow | gắn sprite `loading-bar` vào `LoadingScreen.prefab`, biên dịch | Editor rảnh |
| @ClaudeImage | làm order theo thứ tự tới (Hud đã giao, PopupLose sắp tới); bỏ qua `Hud/hud-sprites` | |
| @CodeLead | Play kiểm hình từng feature khi báo xong, đánh tiến độ; đối chiếu config các feature với `feature-balance/balance-config/number-table` (IDEA: dùng bộ số chuẩn, số nằm trong config) | người: Game view 1920x1080 |
| Balance | không code ở IDEA: bộ số nằm trong config của từng feature, @CodeLead đối chiếu | |
| Wave, Wallet, Upgrade, Run, Core, _Services/*, Codex | không có việc mới; sửa khi @CodeLead báo lỗi | |
| PopupPause, Home, TeamSelect | chờ PROTOTYPE | |

## Chặn, chỉ người gỡ được
1. Mở phiên @CodeLead (hiện không chạy).
2. Game view của Editor đặt 1920x1080 ngang.
3. Sync sang live: gốc; `feature-castle`, `feature-upgrade`, `feature-hero`, `feature-popup`, `feature-hud`; `feature-popup/cheat`; `skill-button-spec`; `upgrade-effect-values`; `effect-numbers`.
4. Font TMP có dấu tiếng Việt, và cho phép thư mục `TextMesh Pro` ở gốc Assets (STR-06) trong `rules.json`.
5. Huỷ order `claudeimage/orders/Hud/hud-sprites` (đặt nhầm).

## Xong IDEA khi
Mọi node lá có mức IDEA đều `done` do @CodeLead đánh sau khi Play kiểm.
