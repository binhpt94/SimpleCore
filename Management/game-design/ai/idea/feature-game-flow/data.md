---
based-on: ca84fa282d14
node-by: gpt@r2-all-2
by: play-opens-team=gpt@r2-1#choi-mo-chon-doi, start-hands-team=gpt@r2-1#vao-tran-ban-giao-doi, show-battle-when-ready=gpt@r2-1#hien-tran-sau-chuan-bi, replay-to-team=gpt@r2-1#choi-lai-ve-chon-doi, lose-to-home=gpt@r2-1#thua-ve-man-bat-dau, back-from-team=gpt@r2-1#quay-lai-tu-chon-doi, flow-lock=gpt@r2-1#khoa-yeu-cau-chuyen-man, retry-on-build-fail=gpt@r2-1#cho-thu-lai-khi-dung-tran-loi, leave-choice-after-dispose=gpt@r2-1#nhan-lua-chon-sau-khi-don, flow-only-picks-target=gpt@r2-1#quyen-chon-man-dich, flow-owns-screens=gpt@r2-1#quyen-doi-man-chinh, flow-from-scene-ready=gpt@r2-1#ban-giao-khi-game-san-sang
gpt-dropped: r2-1#bo-luot-ve-man-bat-dau=bo-luot-ve-man-bat-dau
---
# Điều hướng màn

Feature giữ việc chuyển giữa màn bắt đầu, màn chọn đội và màn trận bên trong scene Game. Tách khỏi `feature-run`, để lượt chơi chỉ lo phần trong trận: vòng đời trận, đội đã chốt, điểm và kỉ lục.

## Ý cốt lõi

### flow-owns-screens | AI 7 | Điều hướng màn là nơi duy nhất đổi màn chính đang hiện giữa màn bắt đầu, màn chọn đội và màn trận
Popup thua vẫn do feature sở hữu nhờ dịch vụ popup hiện và đóng.

### flow-only-picks-target | AI 7 | Điều hướng màn là nơi duy nhất chọn màn đích cho mọi yêu cầu đổi màn hợp lệ trong scene Game
Lượt chơi chỉ bàn giao lựa chọn đã xử lí, không tự chọn đích.

### flow-from-scene-ready | AI 7 | Mỗi lần scene Game được tải, điều hướng màn hiện màn bắt đầu khi chuyển scene báo Game đã sẵn sàng
Tín hiệu sẵn sàng chỉ mở màn bắt đầu một lần cho mỗi lần tải Game.

### leave-choice-after-dispose | AI 7 | Điều hướng màn nhận lựa chọn rời trận từ lượt chơi sau khi lượt chơi báo đã dọn trận xong
Nút Chơi và Vào trận gửi thẳng cho điều hướng màn; lựa chọn rời trận đi qua lượt chơi trước.

### play-opens-team | AI 7 | Nút Chơi trên màn bắt đầu xin điều hướng màn mở màn chọn đội

### start-hands-team | AI 7 | Điều hướng màn chuyển nguyên đội đã xác nhận cho lượt chơi khi nhận yêu cầu Vào trận
Lượt chơi giữ đội đã chốt và cấp cho hero khi dựng trận.

### show-battle-when-ready | AI 7 | Điều hướng màn chỉ hiện màn trận sau khi lượt chơi báo trạng thái đầu lượt đã sẵn

### replay-to-team | AI 7 | Chọn Chơi lại trên popup thua thì về màn chọn đội
Người chơi đổi đội hoặc xác nhận lại đội được điền sẵn.

### lose-to-home | AI 7 | Chọn Về màn bắt đầu trên popup thua thì về màn bắt đầu

### back-from-team | AI 6 | Nút Quay lại trên màn chọn đội đưa người chơi về màn bắt đầu
Đường rời màn chọn đội trước khi vào trận; đội được nhớ không đổi.

### flow-lock | AI 7 | Điều hướng màn bỏ qua yêu cầu đổi màn mới trong lúc đang xử lí một lần đổi màn
Khoá tới khi màn đích sẵn sàng, hoặc tới khi đã khôi phục màn nguồn sau lỗi; hai lần bấm Vào trận chỉ dựng một trận.

### retry-on-build-fail | AI 5 | Dựng trận thất bại thì màn chọn đội cho thử lại với đội vừa xác nhận
Lượt chưa bắt đầu nên đội được nhớ chưa đổi. Gần mức trường hợp biên của vòng sau.
