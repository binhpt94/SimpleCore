# Cheat: dịch vụ popup

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Ở bản dev, chọn state `sample-popup` thì dịch vụ mở popup mẫu `_Services/Popup/DevSample` lên scene đang chạy, qua đúng đường mở, đóng của dịch vụ (`feature-popup/popup-request/request-rules`), để xem nền che, khung và việc chặn bấm (`feature-popup/popup-layer/stack-rules`).
- Id `_Services/Popup/DevSample` do chính dịch vụ khai trong Internal của nó; asset popup mẫu (khung trống, một nút "Đóng") nằm trong thư mục của dịch vụ. Cả id lẫn asset chỉ có ở bản dev, nên danh sách id của scene Game ở bản phát hành vẫn đúng 2 id như `request-rules`.
- Bấm "Đóng": dịch vụ đóng popup mẫu như mọi lần đóng do feature gọi yêu cầu; ở đây dịch vụ tự là bên gọi.
- Code cheat nằm trong thư mục `_Services/Popup`, sau cờ dev; không lọt vào bản phát hành.

## Đã tự quyết
- **Viết lại state, không xoá node (2026-11-24, theo đề nghị của @CodeLead và phiên Popup qua @Manager).** Luật node cheat bắt buộc với mọi feature có frame `kind: ui`, kể cả feature hạ tầng; `stack-rules` có frame `ui`, xoá node này thì nhánh dịch vụ mang lỗi cấu trúc. Thay vào đó sửa cả bốn điểm vướng của state cũ `popup-over-battle`:
  1. Không mở `PopupPause/Main` (không thuộc mức IDEA): dùng popup mẫu của chính dịch vụ.
  2. Không đòi trận đứng yên: dịch vụ không tham chiếu `feature-run`, và mở popup không tự dừng trận (`feature-popup/popup-request`).
  3. Không tạo id của feature khác: id mẫu là của chính dịch vụ, khai trong Internal của nó.
  4. Hình dáng popup thật vẫn của feature gọi; popup mẫu chỉ là khung trống cỡ khung chuẩn của `stack-rules`, là bên gọi của chính dịch vụ.
  Đã loại: xoá node (trái luật cheat); giữ `PopupPause/Main` (vướng cả bốn điểm).
- **Id mẫu mang tên thư mục của dịch vụ: `_Services/Popup/DevSample` (2026-11-24, theo @CodeLead qua @Manager).** Validator SRC-05 bắt phần chủ của id trùng thư mục code của chủ; thư mục của dịch vụ là `_Services/Popup` (`code.json`). Đã loại: `Popup/DevSample` (bản trước, validator báo lỗi).
- **Cảnh popup đè lên trận thuộc cheat của feature sở hữu popup.** `feature-popup-lose/cheat` (state `new-record`) đã có nền che trên trận đứng yên phía sau và đủ thành phần popup thua; về sau `feature-popup-pause` có cheat riêng. Không cần thêm state nào cho popup thua.
- **Một state.** Game chỉ mở một popup mỗi lúc; state chồng nhiều popup không có thật. Đã loại: state ba popup chồng nhau.
- **Frame `use` khung từ `stack-rules`, cao nút từ `feature-hud/hud-layout/layout-spec`.** Tự ghi: nền che và lớp dưới phủ toàn màn 1920x1080 (màn của vòng 1); nút "Đóng" rộng 260 px đặt giữa khung (x = (800 − 260) / 2 = 270), y = 540 cùng hàng nút với popup thua.
- **Nút "Đóng" giữ hình cơ bản trong claude-concept (2026-11-25).** Không node vòng 4 nào của dịch vụ có phần nút: dịch vụ không vẽ nút, nút là của feature gọi. Popup mẫu chỉ có ở bản dev, nên hình cơ bản là đủ. Đã loại: `use` số nút "Về màn bắt đầu" của popup thua để mượn phần đã vẽ (dịch vụ sẽ dựa vào feature gọi, trái luật "dịch vụ không biết nội dung popup"); đặt order vẽ nút riêng (tốn lượt cho thứ không ra bản phát hành).
- **Ảnh art-concept của dịch vụ popup (khung, nền che) chỉ đặt ở node này**, style `ui-flat`; chưa đặt order, cảnh popup đã có ảnh ở `feature-popup-lose/cheat`.

## Chưa rõ
- Không có điểm chặn.
