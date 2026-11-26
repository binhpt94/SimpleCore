# Nhờ hiện và đóng popup

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- Một dịch vụ popup phục vụ nhiều feature gọi.
- Một feature gọi có thể có một hoặc nhiều popup, mỗi popup một id.
- Feature gọi hiện tại: `feature-popup-lose`, `feature-popup-pause`, `feature-popup-revive`.
- Dịch vụ là một bên tham gia của scene Game (`feature-session-scene/session-steps`).

## Định nghĩa
- `glossary#caller`
- `glossary#popup-id`

## Danh sách
- Việc của dịch vụ: nhận id, dựng popup, đặt lên trên, đóng khi được yêu cầu, huỷ khi rời scene.
- Việc của feature gọi: chọn lúc mở, điền nội dung, xử lí nút bấm, yêu cầu đóng.

## Luật cơ bản
- Không feature nào tự dựng popup; mọi popup đi qua dịch vụ.
- Dịch vụ không biết nội dung hay ý nghĩa nút của popup.
- Popup chỉ đóng khi feature gọi yêu cầu, hoặc khi scene bị huỷ.
- Mở popup không tự dừng trận; dừng trận là việc của feature gọi (`feature-popup-pause`, `feature-run`).

## Đã tự quyết
- **Đi qua id, không qua object.** Feature gọi không phải đưa đồ của mình cho dịch vụ. Đã loại: đưa thẳng object popup.
- **Feature gọi điền nội dung.** Dịch vụ giữ chung cách hiện, không chứa luật game.
- **Không tự đóng khi bấm ra ngoài.** Popup thua và tạm dừng đều cần người chơi chọn rõ. Đã loại: bấm ra ngoài là đóng.
- **Popup không tự dừng trận.** Không phải popup nào cũng dừng trận; để feature gọi quyết.

## Chưa rõ
- Hiệu ứng mở và đóng popup: vòng 4.
