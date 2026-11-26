# Lúc mời
Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- `feature-run/run-score` cấp cờ "kỉ lục mới" của lượt vừa chốt; feature này chỉ đọc.
- `feature-popup-lose` hiện popup thua; lời mời chồng lên nó, theo luật xếp chồng của `feature-popup`.
- Một lần mở game có tối đa một lần mời; một lần mời ứng với đúng một lựa chọn (node `rating-invite`).

## Định nghĩa
- Không có từ mới.

## Danh sách
- Điều kiện để mời: cờ "kỉ lục mới" bật, popup thua đang hiện, lần mở game này chưa mời.

## Luật cơ bản
- Đủ ba điều kiện thì mời; thiếu một thì không mời.
- Lời mời luôn nằm trên popup thua, không bao giờ nằm dưới.
- Không lưu gì xuống máy.

## Chưa rõ
- Không còn.

## Đã tự quyết
- Lần mời tính khi popup mời đã hiện, không tính khi chỉ xin mở mà bị từ chối: người chơi chưa thấy lời mời thì chưa mất lượt mời.
