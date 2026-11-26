## Phần tử
- spine-idle | nút | chính | chuyển nhân vật sang idle, ghi "Đứng" | trạng thái: bật, đang chọn (khi idle đang chạy)
- spine-run | nút | chính | chuyển nhân vật sang run, ghi "Chạy" | trạng thái: bật, đang chọn (khi run đang chạy)
- spine-jump | nút | chính | cho nhân vật nhảy một lần, ghi "Nhảy" | trạng thái: bật, đang chọn (khi jump đang chạy)
- spine-flip | nút | chính | quay mặt nhân vật sang phía kia, ghi "Lật" | trạng thái: bật
- spine-state | nhãn | phụ | ghi animation đang chạy, lặp hay một lần, mặt đang quay | trạng thái: một trạng thái, chữ đổi theo nhân vật

## Tương tác
- spine-idle chạm rồi nhả trên nút: đổi sang idle, chuyển mượt; đang idle thì không làm gì.
- spine-run chạm rồi nhả trên nút: đổi sang run, chuyển mượt; đang run thì không làm gì.
- spine-jump chạm rồi nhả trên nút: nhảy một lần rồi tự về idle; đang nhảy thì nhảy lại từ đầu.
- spine-flip chạm rồi nhả trên nút: lật mặt ngay, animation đang chạy không đổi.
- Mọi nút: chạm rồi kéo ra ngoài mới nhả thì không làm gì; giữ không có thao tác riêng.
- spine-state: không tương tác.

## Flow
- Màn trận mở → nhân vật idle, quay mặt trái; bốn nút và nhãn hiện, nhãn "idle · lặp · mặt trái", spine-idle đang chọn.
- Chạm Chạy → "run · lặp · mặt trái". Chạm Lật → "run · lặp · mặt phải".
- Chạm Nhảy → "jump · một lần · …"; nhảy xong → "idle · lặp · …", spine-idle đang chọn.
- Popup bất kì mở → popup che lên, không chạm tới nút; nhân vật vẫn chạy dưới nền che; đóng popup → bấm được lại, trạng thái giữ nguyên.
- Rời màn trận → nhân vật, nút và nhãn ẩn; lần sau mở lại từ idle, mặt trái.
