# Popup mời đánh giá
Dữ liệu cốt lõi: [data.md](data.md)

## Người chơi được gì
- Một lối tắt để nói mình thích game, đúng lúc vừa chơi tốt (vừa phá kỉ lục).
- Lời mời không chặn lượt chơi: chọn Để sau là đi tiếp ngay, không bị hỏi lại trong lần mở game đó.

## Liên quan
- `feature-popup`: hiện, đóng popup và nền che.
- `feature-run`: biết lượt vừa chốt có phá kỉ lục hay không.
- `feature-popup-lose`: lời mời chỉ tới sau khi popup thua đã đóng.
- `feature-game-flow`: người chơi đang đi sang màn đã chọn (chọn đội hoặc màn bắt đầu) khi lời mời hiện.
- `feature-save`: hiện chỉ lưu kỉ lục (`save-record-only`), nên feature này không nhớ được gì qua lần tắt game.

## Chưa rõ
- Nhớ "đã đánh giá" hay "đã từ chối" qua lần tắt game cần `feature-save` lưu thêm dữ liệu, trái với ý đã chốt `feature-save#save-record-only`. Ngoài phạm vi việc này; đã nêu với @Manager.
- Lời mời hiện trên màn nào sau khi popup thua đóng (màn chọn đội hay màn bắt đầu): để vòng 3.
- Dùng trang Google Play của game hay khung đánh giá ngay trong game của Google Play (In-App Review): để vòng 4. Hướng dẫn của Google cho khung trong game không cho hỏi người chơi trước khi hiện khung, nên hai cách này khác hẳn nhau về luồng.

## Đã tự quyết
- Không làm kiểu "chọn sao trong game, chỉ 5 sao mới sang Google Play": đó là lọc đánh giá, trái chính sách Google Play. Lời mời nói chung là "đánh giá game", người chơi tự chọn số sao trên Google Play.
- Lúc mời là sau lượt phá kỉ lục: vừa đúng lúc vui, vừa tự thưa dần (càng về sau càng khó phá kỉ lục), không cần lưu thêm dữ liệu. Đã loại: đếm số lượt rồi mời (cần lưu số đếm qua lần tắt game).
- Mỗi lần mở game mời nhiều nhất một lần: giữ trong bộ nhớ, không cần lưu xuống máy.
