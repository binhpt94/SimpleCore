# Bảng thuật ngữ

Dữ liệu cốt lõi: [data.md](data.md)

Không phải feature: không `code.json`, không node con, không `priority.json`. Node dùng một từ thì nhắc `glossary#<id>` và khai `glossary#<id>@<hash>` trong refs.

## Đã tự quyết
- **Làm lại theo luật glossary mới (2026-11-23, người yêu cầu): từ 73 còn 30.**
- **Tiêu chí giữ** (cần ít nhất một):
  - từ hiểu được nhiều cách, cần chốt một nghĩa;
  - nhiều node dùng và từng định nghĩa khác nhau;
  - khái niệm riêng của game, người lạ không đoán được.
- **Từ giữ và lí do:**
  - Chốt một nghĩa giữa hai từ dễ lẫn: `run` (lượt khác trận), `paused` và `wave-gap` (trận đứng yên khác trận vẫn chạy), `active-wave` (lúc thở không có đợt đang đánh), `wave-clear` (ra hết và không còn con nào), `down` và `defeated` (gục của hero khác bị hạ của quái), `reach-castle` (trong tầm, không phải chạm), `score` (số đợt đã dọn, khác số đợt HUD hiện), `scene` và `screen`, `castle-skill` (khác đòn tự bắn), `level`, `range`, `leave-game`.
  - Từng định nghĩa khác nhau ở nhiều node: `fighting-style`, `target`.
  - Khái niệm riêng của game: `battlefield`, `castle-zone`, `remembered-team`, `base-stats`, `break-wave`, `counter`, `mono-team`, `wave`, `owner-feature`, `participant`, `caller`, `popup-id`, `backdrop`.
- **Từ bỏ, theo nhóm:**
  - Tên đã nói đủ nghĩa: battle, playing, quit-run, slot, run-team, side, melee, ranged, hp, attack, attack-speed, nearest, auto-shot, cooldown, killer, unlocked-type, spawn-interval, record, new-record, upgrade-item, hero-level (gộp vào chi tiết của `level`), next-price, wave-income, number, scene-loading, scene-game, view, popup, popup-layer, top-popup.
  - Nghĩa thực chất là luật hay con số: run-end, respawn-wait, can-buy, upgrade-value, enemy-power, team-power, pressure, hero-power, load-step, run-step, dispose-step, clean, clean-start.
- **Gỡ luật khỏi nghĩa của từ giữ.** Mỗi luật đã có ở node sở hữu (ghi tên node không backtick: glossary không phụ thuộc node nào, không khai refs):
  - "mỗi hero một cách đánh, không đổi trong trận": feature-hero/hero-list (Quan hệ).
  - "gục không làm thua, không mất cấp": feature-hero/hero-respawn (Luật cơ bản).
  - "đội được nhớ chỉ trong lần mở game": feature-team-select/team-memory (Luật cơ bản).
  - "mỗi lúc chỉ có một đợt đang đánh": feature-wave/spawning (Quan hệ), feature-wave/wave-gap (Luật cơ bản).
  - "tầm bắn của thành tính từ đâu": feature-castle/castle-attack/auto-shot-values.
  - "nền che làm tối, chặn bấm", "chỉ popup trên cùng nhận bấm": feature-popup/popup-layer (Luật cơ bản).
  - "bằng kỉ lục không tính là phá": feature-run/run-score (Đã tự quyết), feature-run/run-score/score-rules.
  - "mua được khi đang đánh và đủ tiền": feature-upgrade/upgrade-purchase (Danh sách).
  - Ba bước tải, chạy, huỷ làm gì: các ý của feature-session-scene/session-steps.
  - Công thức sức quái, sức đội, áp lực, sức hero: feature-balance/balance-difficulty-curve (Quan hệ), feature-balance/balance-difficulty-curve/break-forecast, feature-balance/balance-team/hero-compare.
- **Nghĩa chỉ nói từ đó là gì, không nói nó vận hành ra sao.** Chi tiết chỉ để ví dụ hoặc phân biệt với từ gần nghĩa.
- **Chi tiết của ý không nhắc đường dẫn node.** Glossary không khai refs; mọi nghĩa tự đứng được.
- **Node không còn từ nào thì mục "Định nghĩa" ghi "Không dùng thuật ngữ riêng."**

## Chưa rõ
- Các node vòng 4 có định nghĩa riêng dạng `- **Từ**: nghĩa` chưa được rà theo tiêu chí này; rà khi sửa các node đó.
