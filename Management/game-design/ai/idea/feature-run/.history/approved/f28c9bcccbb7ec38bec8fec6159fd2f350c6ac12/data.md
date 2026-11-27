---
based-on: ca84fa282d14
by: run-scope=gpt@r2-2#moc-vao-luot, run-disposes-battle=gpt@r2-2#dieu-phoi-huy-tran, run-gives-team=gpt@r2-2#cap-doi-cho-hero, run-record=gpt@r2-2#pham-vi-giu-ki-luc, split-game-flow=gpt@r2-3#tach-luong-man-game, flow-switches-screens=gpt@r2-3#chu-doi-man-game, flow-locks-switch=gpt@r2-3#khoa-chuyen-man, team-handoff-via-flow=gpt@r2-3#chuyen-doi-da-chot, run-start-confirm=gpt@r2-3#xac-nhan-bat-dau-luot, flow-waits-dispose=gpt@r2-3#cho-huy-tran-xong, memory-on-start=gpt@r2-3#moc-nho-doi, memory-session=gpt@r2-3#thoi-han-nho-doi
gpt-dropped: r2-1#pham-vi-luot-choi=run-scope-from-team, r2-3#thu-tu-chot-diem=clear-first-counts, r2-2#quyen-quyet-dinh-man-ke=run-routes, r2-2#thuc-hien-doi-man-game=run-switches-screens, r2-2#ban-giao-doi-vao-tran=team-handoff, r2-1#bo-luot-ve-man-bat-dau=bo-luot-ve-man-bat-dau, r2-1#trang-thai-tam-dung=run-pause
---
# Lượt chơi

Một lượt chơi từ lúc bấm chơi, qua chọn đội, vào trận, tới khi thành bị phá. Feature này nối các màn hình theo thứ tự, đặt lại mọi thứ khi bắt đầu lượt, tính điểm và giữ kỉ lục.

## Ý cốt lõi

### run-flow | AI 8 | Luồng màn đi theo thứ tự: màn bắt đầu, chọn đội, trận, báo thua
Các màn là `feature-home`, `feature-team-select`, trận, `feature-popup-lose`.

### run-scope | AI 7 | Lượt bắt đầu khi người chơi vào trận, kết thúc khi thành bị phá
Màn bắt đầu và chọn đội diễn ra trước lượt; khớp nghĩa từ "Lượt" trong bảng thuật ngữ.

### run-start-confirm | AI 7 | Lượt chơi xác nhận lượt đã bắt đầu khi màn trận đã hiện và trạng thái đầu lượt đã sẵn
Mốc này sau khi dựng trận và HUD đã hiện đúng; `feature-wave` dùng nó để mở đợt đầu.

### split-game-flow | AI 6 | Điều hướng màn trong scene Game tách sang `feature-game-flow`
Lượt chơi giữ vòng đời trận, đội đã chốt, điểm và kỉ lục.

### flow-switches-screens | AI 6 | `feature-game-flow` đổi màn trong scene Game, và hiện màn bắt đầu khi scene Game sẵn sàng
Popup thua vẫn do feature sở hữu nhờ dịch vụ popup hiện.

### flow-locks-switch | AI 7 | Từ yêu cầu đổi màn hợp lệ đầu tiên tới khi màn đích sẵn sàng, mọi yêu cầu đổi màn khác bị khoá
Hai lần bấm Vào trận liền nhau chỉ tạo một lần dựng trận.

### run-reset | AI 8 | Mỗi lượt bắt đầu lại từ đầu: thành, tiền, nâng cấp, đợt đều về ban đầu

### run-reset-owner | AI 7 | Lượt chơi điều phối việc đặt lại lượt mới ở từng feature, kể cả khi trận dùng lại scene
Mỗi feature tự đặt lại dữ liệu lượt mình sở hữu khi lượt chơi báo bắt đầu lượt mới. Theo câu hỏi chung vòng 2 (`khoi-tao-luot-moi`).

### run-disposes-battle | AI 7 | Lượt chơi điều phối việc huỷ trận khi người chơi rời màn trận
Đường chơi lại cần huỷ trận ngay trong scene Game, không chờ rời scene.

### flow-waits-dispose | AI 7 | Màn đích chỉ hiện sau khi lượt chơi báo đã huỷ trận xong
Chơi lại và về màn bắt đầu sau khi thua đều chờ cùng tín hiệu.

### team-handoff-via-flow | AI 6 | `feature-team-select` gửi đội đã xác nhận qua `feature-game-flow` để lượt chơi dựng trận
Lượt chơi giữ đội này cho hero trong lượt; màn chọn đội giữ đội điền sẵn cho lần sau.

### run-gives-team | AI 7 | Lượt chơi cấp đội đã chốt cho `feature-hero` khi dựng trận
Hero đọc đội từ một nguồn để sinh đúng hero và giữ nguyên đội suốt trận.

### memory-on-start | AI 6 | Màn chọn đội chỉ cập nhật đội được nhớ khi lượt chơi xác nhận lượt đã bắt đầu
Chỉnh đội rồi rời màn mà không vào trận thì vẫn giữ đội của lượt gần nhất.

### memory-session | AI 6 | Màn chọn đội giữ đội được nhớ trong lần mở game hiện tại
Mở lại game thì chỉ còn kỉ lục.

### run-end-owner | AI 7 | Lượt chơi là nơi duy nhất chốt lượt đã kết thúc khi thành bị phá
Chốt ở một nơi kể cả khi thành bị phá sát lúc đợt được dọn sạch. Theo câu hỏi chung vòng 2 (`quyen-chot-luot`).

### run-score | AI 8 | Điểm của lượt là số đợt đã dọn sạch
Đã loại: điểm theo số quái hạ.

### run-record | AI 7 | Lượt chơi chỉ giữ kỉ lục qua các lượt, và lưu kỉ lục cả khi tắt game
Màn bắt đầu và popup thua nhận kỉ lục từ cùng nguồn này. Đội được nhớ là của `feature-team-select`, không phải dữ liệu của lượt.

### run-replay | AI 7 | Chơi lại thì về màn chọn đội để đổi đội nếu muốn
Đã loại: vào thẳng trận với đội cũ.
