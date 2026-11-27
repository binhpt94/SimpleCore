## Phần tử
- cheat-button | nút | chính | mở và đóng panel cheat, chữ "Cheat" | trạng thái: panel đóng, panel đang mở
- cheat-shade | khung | phụ | che màn hình dưới panel, chặn chạm xuống game và popup
- cheat-panel | khung | chính | chứa tiêu đề và danh sách dòng
- panel-title | nhãn | phụ | chữ "Cheat"
- state-list | danh sách | chính | các dòng của panel, cuộn dọc khi dài hơn panel
- normal-row | nút | chính | dòng "Chơi bình thường", luôn là dòng đầu | trạng thái: thường, đang chọn, khoá
- state-row | nút | chính | một dòng state, chữ "<tên feature>: <tên state>"; mẫu cho mọi dòng state | trạng thái: thường, đang chọn, khoá
- empty-label | nhãn | phụ | chữ "Bản này không có state cheat", chỉ hiện khi không có dòng state nào
## Tương tác
- cheat-button chạm khi panel đóng: mở panel, hiện cheat-shade
- cheat-button chạm khi panel đang mở: đóng panel, game nhận chạm lại như cũ
- cheat-shade chạm: không làm gì
- state-list kéo dọc: cuộn danh sách
- normal-row chạm: bỏ state cheat, đóng panel, game khởi động lại không có state
- state-row chạm: đóng panel, game chuyển tới state của dòng đó
- normal-row, state-row chạm khi khoá: không làm gì
## Flow
- Mọi màn, mọi popup → panel cheat (chồng lên trên cùng, nền che chặn chạm phía dưới)
- Panel cheat → đóng bằng cheat-button, bằng chọn một dòng, hoặc khi game đổi scene
- Chọn state-row → scene chứa state được tải lại với state đó
