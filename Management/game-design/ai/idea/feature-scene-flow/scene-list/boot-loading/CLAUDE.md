# Màn Loading lúc mở game

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
1. Mở game: engine mở scene Loading. Scene Loading tải và chạy như mọi scene (`feature-session-scene/session-steps`); view màn Loading là của feature này.
2. Màn Loading hiện tên game (chữ) và thanh tiến độ ở 0%. Ngay sau khi màn Loading hiện, feature này xin chuyển sang scene Game (`feature-scene-flow/scene-request`).
3. Lớp che hiện lên với đúng nội dung màn Loading (tên game, thanh tiến độ), rồi scene Loading bị huỷ. Người chơi không thấy khác gì: vẫn là màn Loading.
4. Trong lúc scene Game tải, thanh tiến độ trên lớp che tăng theo số feature tham gia của scene Game đã tải xong.
5. Tải xong và màn Loading (tính từ lúc nó hiện ở bước 2) đã hiện đủ thời gian ít nhất: scene Game chạy, màn bắt đầu (`feature-home`) được tạo, rồi lớp che biến mất.
6. Sau đó không quay lại Loading trong lần chạy game này.

## Thông số
| Thông số | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Thời gian hiện ít nhất của màn Loading | 1 | giây | Tránh màn nháy; ngắn đủ để không thấy chờ |
| Thời gian tối đa từ mở game tới màn bắt đầu | 5 (dự kiến) | giây | Lượt chơi ngắn, người chơi phổ thông dễ bỏ nếu chờ lâu |
| Thời gian chờ tối đa một feature tải | 10 (dự kiến) | giây | Quá mức này coi là tải lỗi (`loading-fail`) |
| Bước tiến độ | 1 feature | feature | Tiến độ thật, không giả |
| Số nút trên màn Loading | 0 | nút | Theo `boot-path` |

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Nội dung màn Loading | Tên game dạng chữ + thanh tiến độ | Như IDEA | Có hình nền và logo game |
| Thời gian hiện ít nhất | 1 giây | 1 giây | 1 giây |
| Thời gian tối đa tới màn đầu tiên của scene Game | 5 giây (dự kiến); ở IDEA màn đầu là trận, chưa có màn bắt đầu (`feature-run/run-flow/flow-rules`) | 5 giây (màn bắt đầu) | 3 giây (dự kiến, sau khi tối ưu) |
| Thanh tiến độ | Nhảy theo từng feature | Chạy mượt giữa các mốc | Như PROTOTYPE |
| Màn lỗi tải | Dòng chữ nêu tên feature lỗi | Như IDEA | Chữ thân thiện cho người chơi, tên feature chỉ ghi vào log |

## Trường hợp biên
- **Tải xong trước 1 giây**: chờ cho đủ 1 giây rồi mới sang màn bắt đầu.
- **Quá 5 giây nhưng vẫn đang tải**: tiếp tục tải, ghi cảnh báo hiệu năng vào log; chỉ coi là lỗi khi một feature quá 10 giây.
- **Một feature tải lỗi hoặc quá 10 giây**: dừng ở màn Loading, thanh tiến độ đứng yên, hiện dòng lỗi nêu tên feature. Các feature đã tải xong của scene Game được huỷ theo thứ tự ngược (`feature-session-scene/session-steps/participant-order`).
- **Người chơi rời game (về màn hình chính điện thoại) lúc đang Loading**: quay lại thì tải tiếp, không bắt đầu lại; thời gian rời game không tính vào giới hạn 10 giây.
- **Chạm màn hình lúc Loading**: không có tác dụng.

## Đã tự quyết
- **Tiến độ theo số feature đã tải xong.** Đúng và kiểm được. Đã loại: tiến độ giả theo thời gian (đánh lừa khi tải lỗi).
- **Dừng hẳn khi tải lỗi, không vào Game.** Theo luật "thiếu phụ thuộc thì lỗi ngay, không âm thầm bỏ qua". Đã loại: vào Game thiếu feature; tự thử lại (lỗi thiếu asset thử lại vẫn lỗi).
- **Màn Loading là của feature này.** Scene Loading chỉ có việc tải và chuyển, đều thuộc feature này. Đã loại: một feature Loading riêng (quá nhỏ).
- **Loading xin chuyển ngay khi hiện.** Scene Loading không có gì để tải ngoài chính nó; việc tải thật là tải scene Game.

## Chưa rõ
- Tên chính thức của game (đang dùng tên tạm từ node gốc).
