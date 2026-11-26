# Nội dung lời mời
Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- Một lần mời là một lần popup mời hiện, ứng với đúng một lựa chọn của người chơi (Đánh giá hoặc Để sau).
- Popup mời là của feature này; `feature-popup` hiện, đóng và che phần bên dưới.
- Chạm Đánh giá thì người chơi rời game sang Google Play. Game vẫn chạy nền như khi người chơi tự rời game.

## Định nghĩa
- Không có từ mới. Từ dùng chung (popup, nền che, id popup) đã có ở `glossary`; node này không cần khai.

## Danh sách
- Phần tử: dòng mời, nút Đánh giá, nút Để sau.

## Luật cơ bản
- Dòng mời trung tính, theo `rating-neutral-text` của node cha: không hỏi người chơi có thích game không, không nhắc số sao.
- Không có thưởng khi chạm Đánh giá, theo `rating-no-reward`.
- Feature này không lưu gì xuống máy (người quyết 2026-11-27).

## Chưa rõ
- Không còn. Lúc mời là nhánh `rating-timing` (chồng lên popup thua, người quyết 2026-11-27).

## Đã tự quyết
- Mở trang Google Play của game, không dùng khung đánh giá trong game (In-App Review): Google không cho hỏi người chơi trước khi hiện khung đó, mà feature này là một popup hỏi trước. Đã loại: In-App Review.
- Popup đóng trước rồi mới mở Google Play: người chơi quay lại game thì không còn thấy lời mời. Đã loại: giữ popup mở sau khi chạm Đánh giá.
