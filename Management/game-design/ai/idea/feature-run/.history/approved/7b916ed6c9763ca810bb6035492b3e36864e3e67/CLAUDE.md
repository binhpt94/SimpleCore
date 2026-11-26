# Lượt chơi

Dữ liệu cốt lõi: [data.md](data.md)

## Người chơi được gì
- Vào trận nhanh, thua thì chơi lại ngay với đội khác.
- Kỉ lục số đợt để có lí do chơi lại.

## Liên quan
- `feature-home`: màn bắt đầu.
- `feature-team-select`: màn chọn đội; đội vừa chọn vào trận.
- `feature-castle`: báo khi thành bị phá.
- `feature-wave`: số đợt đã dọn; dừng thả quái khi lượt kết thúc.
- `feature-wallet`, `feature-upgrade`, `feature-hero`: đặt lại khi bắt đầu lượt.
- `feature-popup-lose`, `feature-popup-pause`: hai popup của lượt chơi.
- `feature-scene-flow`: chuyển scene nếu các màn nằm ở scene khác nhau.

## Đã tự quyết
- **Tách màn bắt đầu, popup thua, popup tạm dừng thành feature riêng.** Theo luật vòng 2 mới: mỗi popup là một feature; màn bắt đầu là màn hình riêng giao được cho một phiên. Feature này chỉ nối chúng theo thứ tự và giữ dữ liệu của lượt.
- **Bỏ ý `run-lose-screen`, thêm ý `run-replay`.** Nội dung popup thua chuyển sang `feature-popup-lose`; còn lại quyết định "chơi lại về màn chọn đội".
- **Có hồi sinh thành một lần mỗi lượt bằng quảng cáo (2026-11-24, người yêu cầu; thay quyết định cũ "không có hồi sinh khi thua").** Ý `run-revive-offer`, `run-revive-once`; `run-end-owner` sửa thành "thành bị phá mà không hồi sinh". Quảng cáo không xem được thì thua như thường, không có hồi sinh miễn phí. Đã loại: hồi sinh không giới hạn.
- **Lúc tạm dừng có mua nâng cấp được không:** hướng hiện tại là không (giữ áp lực mua giữa trận); luật này để vòng 3.

- **Đưa góp ý GPT lần hỏi `r2-1` (2026-11-24).**
  - `bo-luot-ve-man-bat-dau` (AI 7): đường bỏ lượt thuộc lượt chơi; khớp nhánh tạm dừng đã chốt.
  - `run-pause` viết lại theo `trang-thai-tam-dung` (AI 7): lượt chơi giữ trạng thái tạm dừng chung; đi cặp với `pause-run-ownership` của `feature-popup-pause`.
  - `run-scope` (góp ý `pham-vi-luot-choi`, làm rõ `run-flow`) tách thành ý riêng thay vì viết đè `run-flow`: viết đè sẽ mất thứ tự bốn màn đã chốt.
- **Đưa góp ý GPT lần hỏi `r2-2` (cụm lượt chơi, 2026-11-24).**
  - Loại bản `r2-1#pham-vi-luot-choi` của `run-scope` (lượt tính từ chọn đội), chấm lại AI 4: lệch nghĩa từ "Lượt" trong bảng thuật ngữ và với mốc vào trận của nhánh bắt đầu lượt; đã có góp ý `r2-2#moc-vao-luot` thay đúng chỗ. Khai `gpt-dropped`.
  - `run-scope` viết lại theo `moc-vao-luot` (AI 7): lượt tính từ lúc vào trận, khớp nghĩa từ "Lượt" trong bảng thuật ngữ; giải quyết chỗ lệch của bản `r2-1` (lượt tính từ chọn đội, AI 5).
  - `run-flow` đổi chữ "Một lượt đi theo thứ tự" thành "Luồng màn đi theo thứ tự" cho khớp `run-scope`; thứ tự bốn màn giữ nguyên. Góp ý `quyen-quyet-dinh-man-ke` (làm rõ `run-flow`) tách thành ý riêng `run-routes`, vì viết đè sẽ mất thứ tự màn.
  - `run-record` viết lại theo `pham-vi-giu-ki-luc` (AI 7), nhưng không sửa `run-reset` của gốc như GPT đề nghị: đội được nhớ là của `feature-team-select`, không phải dữ liệu của lượt (lí do ở `CLAUDE.md` của gốc).
  - `run-switches-screens`, `run-disposes-battle`, `team-handoff`, `run-gives-team` (AI 7): chia việc rõ giữa lượt chơi, chọn đội, hero, chuyển scene và vòng đời scene.
- **Đưa góp ý GPT lần hỏi `r2-3` (cụm lượt chơi, không rào, 2026-11-24).** Tách điều hướng màn sang `feature-game-flow` (`split-game-flow`, `flow-switches-screens`, `team-handoff-via-flow`, AI 6) đặt cạnh các ý cũ `run-routes`, `run-switches-screens`, `team-handoff` (trái nhau, điểm sẽ chốt một bên). Ba góp ý "làm rõ" (`xac-nhan-bat-dau-luot`, `cho-huy-tran-xong`) tách thành ý riêng để không mất nhãn của ý cũ. `memory-on-start`, `memory-session` (AI 6) trùng nhánh nhớ đội đã chốt.
  - **Loại `thu-tu-chot-diem` (làm rõ `run-end-owner`), AI 4:** tính đợt cuối nếu tin dọn đợt tới trước tin thành bị phá làm kết quả phụ thuộc thứ tự tin; trái luật "thua trước" đã chốt. Khai `gpt-dropped`.
- **Người chấm cặp ý trái nhau (2026-11-24): `run-switches-screens` 2, `team-handoff` 2.** Loại hai ý của GPT đó (khai `gpt-dropped`); việc đổi màn và nhận đội đi qua `feature-game-flow`. Loại luôn `run-routes` (AI 4, cũng của GPT): lượt chơi quyết màn đích trái với ý người vừa chọn, "điều hướng màn là nơi duy nhất chọn màn đích".
- **Ba ý từ câu hỏi chung vòng 2** (`_general/r2-all`): `run-reset-owner`, `run-end-owner`, `run-pause-request` (AI 7), không nhãn `by:` vì góp ý không gắn node.

## Chưa rõ
- Các màn nằm chung một scene hay tách scene: vòng 3, cùng `feature-scene-flow`.
