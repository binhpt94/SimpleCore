# Bảng thuật ngữ

Dữ liệu cốt lõi: [data.md](data.md)

Không phải feature: không `code.json`, không node con, không `priority.json`. Node dùng một từ thì nhắc `glossary#<id>` và khai `glossary#<id>@<hash>` trong refs.

## Đã tự quyết
- **Gom định nghĩa từ mục "Định nghĩa" của mọi node vòng 3 (2026-11-23, người yêu cầu).** Mục "Định nghĩa" của các node đó giờ chỉ còn danh sách `glossary#<id>`.
- **Gộp từ trùng nghĩa:**
  - Máu, máu tối đa, máu hiện tại, máu thành → `hp` (một định nghĩa cho hero, quái, thành).
  - Tầm đánh, tầm bắn của thành → `range`.
  - Chỉ số gốc (quái, độ khó theo đợt) → `base-stats`.
  - Cận chiến, bắn xa của hero và của quái → `melee`, `ranged`.
  - Tới thành (thân thành, quái đi và đánh) → `reach-castle`.
  - Hồi chiêu, sẵn sàng, kĩ năng thành, đòn tự bắn (nhiều node) → một ý mỗi từ.
  - Giá cấp sau và giá một lần nâng → `next-price`.
  - Cấp của mục nâng cấp (danh sách nâng cấp, hiệu ứng nâng cấp thành) → `level`; cấp hero là trường hợp riêng → `hero-level`.
  - Điểm, số đợt đã dọn sạch → `score`. Kỉ lục, kỉ lục mới → `record`, `new-record`.
- **Chọn một nghĩa khi các node nói khác nhau:**
  - **Cách đánh** (`fighting-style`): theo danh sách hero (nhóm cận chiến/bắn xa cộng kiểu đòn), vì đó là node sở hữu hero. Thẻ hero ghi "vai trò trong trận"; đội hero cân nhau ghi "tổ hợp năm chỉ số và chỗ đứng". Hai cách nói đó là hệ quả của cách đánh, đã đưa vào phần chi tiết ("quyết hero đứng chắn ở trước hay đánh từ phía sau"). Đã loại: định nghĩa bằng năm chỉ số (hero cùng nhóm, cùng kiểu đòn sẽ thành khác cách đánh chỉ vì chỉ số khác).
  - **Mục tiêu** (`target`): một nghĩa chung cho hero và thành. Cách chọn (gần hero nhất, gần thành nhất trong tầm) là luật, nằm ở `feature-hero` và `feature-castle`.
  - **Đợt đang đánh** (`active-wave`): theo thả quái (đợt có quái đang ra hoặc còn sống). Việc HUD hiện đợt sắp tới trong lúc thở là luật hiển thị của thông tin trận, không phải nghĩa của từ.
  - **Mua được** (`can-buy`): "đang đánh và đủ tiền", gom "trong trận, không tạm dừng" thành `playing`.
- **Không đưa vào:** tên riêng từng hero, quái, kĩ năng, chữ trên nút (các tên đó nằm trong danh sách của node sở hữu).
- **Lọc lại bảng (2026-11-23, người yêu cầu): từ 125 còn 73.**
  - Giữ một từ khi: từ hiểu được nhiều cách và cần chốt một nghĩa; nhiều node từng định nghĩa khác nhau; hoặc là khái niệm riêng của game mà người lạ không đoán được.
  - Bỏ một từ khi: tên đã nói đủ nghĩa; hoặc nghĩa thực chất là luật hay con số (luật nằm ở node vòng 3-4 sở hữu nó).
  - Đã bỏ vì tên đủ nghĩa: initial-state, resume, go-home, hero-roster, full-team, picked-hero, hero-card, move-speed, castle-destroyed, in-range, ready, wave-number, spawn-time, all-spawned, next-wave-start, purchase, money-balance, start-money, config, top-edge, bottom-edge, pause-button, popup-open-close, stacking, game-name, play-button, saved-data.
  - Đã bỏ vì nghĩa là luật: kill-reward, quit-confirm, replay, respawn, idle, slow, skill-power, enemy-type, wave-difficulty, wave-clear-bonus, income-source, spend, upgrade-effect, single-target, front-cleave, splash, role-mark.
  - Đã bỏ vì là chi tiết giao diện của riêng một node: upgrade-bar, hero-group, castle-group, button-lit, button-dim, item-icon, scene-request, scene-cover.
  - Gỡ phần luật khỏi nghĩa (luật vẫn nằm ở node sở hữu): run-end, side, run-team, remembered-team, ranged, hp, reach-castle, target, wave, record, scene-game, quit-run, defeated, hero-level, clean-start.
- **Nghĩa chỉ nói từ đó là gì, không nói nó vận hành ra sao.** Chi tiết chỉ để ví dụ hoặc phân biệt với từ gần nghĩa.
- **Chi tiết của ý không nhắc đường dẫn node.** Glossary không khai refs; mọi nghĩa tự đứng được.

## Chưa rõ
- `fighting-style` ("mỗi hero một cách đánh, không đổi trong trận") và `down` ("gục không làm thua, không mất cấp") còn mang luật trong chi tiết: vế "không đổi trong trận" và "không làm thua" chưa có ở `feature-hero`. Chuyển vào node đó rồi gỡ khỏi glossary.
- Các node vòng 4 có định nghĩa riêng dạng `- **Từ**: nghĩa` chưa được gom (vòng 4 không có mục "Định nghĩa"); gom khi sửa các node đó.
