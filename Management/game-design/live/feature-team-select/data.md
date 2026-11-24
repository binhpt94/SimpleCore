---
based-on: 53af9956f851
by: team-compare=gpt@r2-1#hien-vai-tro-hero
gpt-dropped: r2-1#chon-san-doi-mac-dinh=team-default
---
# Chọn đội

Màn hình trước mỗi trận, nơi người chơi chọn những hero sẽ vào đội. Đây là quyết định lớn nhất trước khi đánh.

## Ý cốt lõi

### team-pick | AI 8 | Trước mỗi trận, người chơi chọn đội hero từ toàn bộ danh sách hero
Đội đủ người mới vào trận được. Đã loại (người loại ở vòng 1): thành tự sinh hero.

### team-valid-before-start | AI 7 | Màn chọn đội kiểm đội hợp lệ trước khi gửi yêu cầu vào trận
Dùng danh sách hero của `feature-hero` để kiểm quân số và luật chọn đội. Theo câu hỏi chung vòng 2 (`_general/r2-all-2`, góp ý `kiem-tra-doi-hinh`).

### team-compare | AI 7 | Màn chọn đội hiện vai trò phòng thủ của từng hero để người chơi so sánh trước khi chọn
Vai trò cho biết hero giúp giữ thành bằng cách nào, để chọn những hero bổ trợ cho nhau.

### team-remember | AI 6 | Màn chọn đội nhớ đội của lượt trước
Ai muốn giữ nguyên đội thì chỉ cần bấm vào trận. Đã loại: lần nào cũng chọn lại từ đầu.

### team-memory-prefill | AI 7 | Đội được nhớ do màn chọn đội giữ, chỉ là lựa chọn điền sẵn, không phải tiến trình
Người chơi vẫn xác nhận đội trước khi vào trận; đội nhớ không làm hero mạnh hơn. Theo câu hỏi chung vòng 2 (`_general/r2-all`, góp ý `chu-so-huu-doi-duoc-nho`).
