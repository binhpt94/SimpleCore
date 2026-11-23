# Xin chuyển scene

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- Nhiều feature có thể xin chuyển; chỉ feature này thực hiện việc chuyển.
- Một lần chuyển gồm một lần huỷ scene cũ và một lần tải scene mới, cả hai do `feature-session-scene` thực hiện.
- Lần chuyển duy nhất trong luồng hiện tại là Loading sang Game (`feature-scene-flow/scene-list`); người xin là chính scene Loading khi tải xong.

## Danh sách
- Các bước một lần chuyển: hiện lớp che → huỷ scene cũ → tải scene mới → scene mới chạy → bỏ lớp che.

## Luật cơ bản
- Chỉ feature này tải và bỏ scene.
- Huỷ xong scene cũ rồi mới tải scene mới.
- Mỗi lúc một lần chuyển.
- Lớp che chặn bấm trong lúc chuyển.

## Đã tự quyết
- **Xin bằng tên scene.** Feature xin không phụ thuộc cách tải. Đã loại: tự tải scene.
- **Huỷ trước, tải sau.** Không có lúc hai scene cùng chạy, dễ giữ sạch. Đã loại: tải scene mới song song rồi mới huỷ scene cũ.
- **Có lớp che chung.** Người chơi không thấy màn đen hay scene dở dang. Đã loại: không che.

## Chưa rõ
- Lớp che trông thế nào (màu trơn, tên game, hiệu ứng mờ dần): vòng 4.
