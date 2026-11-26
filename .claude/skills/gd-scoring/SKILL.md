---
name: gd-scoring
description: Chỉ phiên FEATURE=@GameDesigner. Chấm điểm - thang điểm ý (AI và người), điểm của người là tuyệt đối, độ quan trọng của node (priority.json ở node lá, điểm hiệu lực, giai đoạn tới hạn và để sau, tiến độ). Dùng khi chấm điểm AI cho ý, viết hay sửa priority.json, hoặc xử lý điểm và phản hồi người chấm.
---
# @GameDesigner: chấm điểm và độ quan trọng
Luật chung: `.claude/rules/gamedesigner.md`. Lúc một node đạt thì ở luật chung.

## Chấm điểm
| Điểm | Nghĩa |
|---|---|
| 8, 9, 10 | Ý tưởng tốt, gần như chốt |
| 5, 6, 7 | Chấp nhận ở hiện tại, sẽ cập nhật nếu có yêu cầu |
| 2, 3, 4 | Ý tưởng xấu, cần làm lại |
| 0, 1 | Ý tưởng lạc, nên tránh |

- GD tự chấm từng ý ở dòng `### <id> | AI <điểm>`. Chấm thật: điểm là đánh giá của bạn về ý đó so với ý tưởng vòng 1 và các node đã chốt. Không nâng điểm để qua vòng; ý bạn thấy dưới 5 thì viết lại ý, đừng sửa số.
- Người chấm trên tab Game design (0 đến 10, kèm phản hồi). **Điểm của người là tuyệt đối**; điểm AI chỉ dùng cho ý người chưa chấm.
- Điểm của người gắn với đúng nội dung ý lúc chấm. Viết lại một ý thì ý đó thành chưa chấm; điểm và phản hồi cũ vẫn nằm trong lịch sử.
- Người chấm được ở cả bảng đề xuất và bảng `live`; đó là một bộ điểm chung. Một ý đã chốt bị người chấm dưới 5 thì node đề xuất tương ứng cũng thành "cần làm lại" (khi nội dung ý còn giống).
## Độ quan trọng của node (tiền đề cho kế hoạch)
Mỗi node có một điểm 0 đến 10 nói node đó cần cho giai đoạn phát triển nào. **Mọi node lá phải có điểm, kể cả một feature mới dừng ở vòng 2 hay một node vòng 3 chưa có con** (nó là node lá cho tới khi có con). Project làm theo giai đoạn: người đặt "giai đoạn đang làm" (dòng `guard:` và `gd_status.py` nêu nó). Node của giai đoạn đang làm và trước đó là **tới hạn**: phải viết tới vòng 4 (`gd_status.py`, mục "Cần thiết kế tới vòng 4"). Node của giai đoạn sau (và node tuỳ chọn) là **để sau**: chỉ cần tóm tắt và các ý cốt lõi đủ để biết nó là gì và nối với feature nào; không đào sâu thêm khi chưa tới giai đoạn của nó (máy không chặn, nhưng đó là công bỏ ra sớm và sẽ phải làm lại). Người đổi giai đoạn thì `gd_status.py` liệt kê các node vừa tới hạn. Chưa đặt giai đoạn: mọi node coi như tới hạn. Ngoài điểm này, dữ liệu theo giai đoạn chỉ được ghi ở mục "Thông số theo giai đoạn" của node vòng 4 (hoặc trong bảng `.excel` của node đó).

| Điểm | Giai đoạn | Nghĩa |
|---|---|---|
| 8, 9, 10 | IDEA | Phải có để chạy đủ core game (art siêu cơ bản) |
| 5, 6, 7 | PROTOTYPE | Tính năng quan trọng (art đủ cho phần core game) |
| 2, 3, 4 | FULL | Cần để đủ tính năng, đủ art; giai đoạn điều chỉnh |
| 0, 1 | (tuỳ chọn) | Có cũng được, không có cũng được |

- GD chỉ chấm ở **node lá** (node chưa có node con), ghi vào `<node>/priority.json`: `{"ai": 8, "why": "Không có đợt quái thì không có gì để chơi."}`. `why` một câu.
- Node cha tự lấy điểm cao nhất của các node bên dưới; `priority.json` đặt ở node cha bị bỏ qua. Khi một lá có node con thì chuyển việc chấm xuống các node con.
- Chấm theo câu hỏi "thiếu node này thì giai đoạn đó có chạy được không", không theo độ khó hay độ thích.
- Người chấm được ở mọi node; điểm của người là tuyệt đối, và các node con người chưa chấm dời theo cùng khoảng cách. `gd_status.py` in điểm hiệu lực; đừng sửa điểm AI để "bù" lại điểm của người.
- Đổi độ quan trọng không đổi hash, không làm node phải làm lại, không ảnh hưởng đạt hay chốt, không ghi vào lịch sử.
- Tiến độ (`.progress`, GD chỉ đọc): node lá đã xong giai đoạn của nó thì không còn đẩy điểm lên node cha; node cha lấy điểm cao nhất của phần **chưa xong** bên dưới.
