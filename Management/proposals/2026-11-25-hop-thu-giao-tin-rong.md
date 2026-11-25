# Đề xuất: sửa lỗi hộp thư giao tin rỗng

Người đề xuất: @Manager · 2026-11-25 · Chờ người xem

## Vấn đề
Từ khoảng 20:39 ngày 2026-11-25, `setup_server.py` vẫn bật thread feature, service và tool lên khi có tin, nhưng thread chỉ nhận lời dẫn chung của trang Setup. Tin thiếu dòng `[tin <id> · từ <vai>]` và thiếu nội dung việc. `dispatch.py list` ghi cho các tin đó: `lỗi: không thấy tin trong transcript của thread sau khi giao`.

## Bằng chứng
| Tin | Đích | Phiên báo lại |
|---|---|---|
| m20261125-211347-977af4 | @DesignAdvisor | GameDesigner: tin tới nơi rỗng, phải nhắn lại bằng SendMessage |
| m20261125-211129-bb4ddf, m20261125-213932-2be744 | _Services/SessionScene | SessionScene: hai lần nhận tin rỗng, không làm gì |
| m20261125-214050-e4e1cb | SceneFlow | SceneFlow: ba lần nhận tin rỗng, BÁO CHẶN |
| m20261125-222815-9a9958 | Hero | Hero: ba lượt liền nhận tin rỗng, chưa sửa file |
| m20261125-214052-49bfba | _Services/Popup | Popup tự đọc được việc qua `dispatch.py list`, làm xong |
| m20261125-211127-2eea4d | Run | Run làm đúng việc dù tin ghi lỗi |

Lỗi không xảy ra mọi lần: Run và Popup vẫn làm được việc, có lẽ nhờ tự đọc `dispatch.py list`.

## Ảnh hưởng
Đang chặn giai đoạn IDEA: Hero, Castle và SimClock là các việc còn lại, đều phải giao qua hộp thư. @Manager không nhắn được feature bằng `SendMessage`, vì guard chặn.

## Đề xuất
- **A (sửa gốc):** người xem chỗ `setup_server.py` ghép nội dung tin vào lượt đầu của thread, và chỗ kiểm tin có trong transcript sau khi giao.
- **B (tạm thời, không đổi code):** thêm vào lời dẫn chung của trang Setup một câu: "không thấy dòng [tin …] thì chạy `python Management/tools/dispatch.py list <tên mình>` để đọc việc". Popup đã làm được như vậy.
- **C (tạm thời):** người vào chat của phiên bị ảnh hưởng và bảo nó chạy `dispatch.py list <tên>`.
