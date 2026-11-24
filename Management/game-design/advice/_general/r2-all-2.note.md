# Nhận xét của @DesignAdvisor về _general/r2-all-2

Nhận xét cho người chấm. Không phải lời của GPT, và không phải lí do để bỏ góp ý.
Lần hỏi này bỏ các rào do @GameDesigner tự thêm ở các lần trước (không gộp, không đổi key, không feature mới, chỉ quyết định vòng 2). Câu hỏi chỉ giữ một yêu cầu của người: không ghi dữ liệu theo giai đoạn.

- **`gop-giao-dien-tran`** (gộp `feature-hud-upgrade` vào `feature-hud`): đi ngược luật vòng 2 trong `.claude/rules/gamedesigner.round2.md` ("HUD lớn thì mỗi cụm lớn tách thành một feature"). Đó là file luật của project, không phải rào bên nhờ tự thêm. `CLAUDE.md` của `feature-hud` từng loại phương án "một HUD duy nhất (quá lớn cho một phiên)".
- **`tach-dieu-huong-man`** (feature mới `feature-game-flow`): tách việc điều hướng màn khỏi `feature-run`. Hiện `feature-run/run-flow` (đã chốt) giữ việc này. Góp ý `quyen-quyet-dinh-man-ke` và `thuc-hien-doi-man-game` ở `advice/feature-run/r2-2.md` lại giao nó cho `feature-run`. Hai lần hỏi cho hai hướng khác nhau.
- **`phan-xu-sat-thuong`** (feature mới `feature-combat`): chưa node nào giữ việc phân xử đòn trúng cùng lúc. Nếu nhận, đây là nhánh mới chạm `feature-hero/hero-targeting`, `feature-enemy/enemy-behavior`, `feature-castle/castle-attack`.
- **`tinh-so-tien-thuong`**: mức thưởng hiện nằm ở ba chỗ: `feature-enemy/enemy-reward/reward-values`, `feature-wave/wave-clear/clear-rules` và `feature-wallet/income/income-amounts`. Góp ý chọn `feature-wallet` làm nơi tính, nên đây là việc trùng thật cần gom về một chỗ.
- **`tam-dung-khi-ra-nen`**: `CLAUDE.md` của `feature-popup-pause` để ngỏ câu hỏi này cho vòng 3; xem `feature-run/run-pause/pause-rules` đã chốt gì chưa.
