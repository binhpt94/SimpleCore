# Config, nút và panel cheat
Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Feature `CheatPanel` có một asset config chứa ô `cheat_enabled`. Lúc game khởi động, đọc ô này một lần.
- Bật: tạo nút cheat nổi trên cùng ở mọi scene, mọi màn, trên cả popup. Tắt: không tạo gì.
- Chạm nút: mở panel (tiêu đề, danh sách dòng cuộn được, nền che). Chạm lại: đóng.
- Phần tử, tương tác và flow ở `ui.md`; bố cục do @UIDesigner vẽ.
- Nội dung và thứ tự các dòng, việc xảy ra khi chạm một dòng: `feature-cheat-panel/cheat-states/states-rules`.

## Thông số
- Bảng `.excel/AccessParams.csv` (`params`): `cheat_enabled` và các chữ hiện trên nút, panel.

## Thông số theo giai đoạn
- Cùng bảng `AccessParams`, cột `idea`. Phạm vi việc chỉ gồm IDEA (Manager giao 2026-11-27), nên cột `prototype`, `full` để trống.

## Trường hợp biên
- Config tắt: không có nút, không có nền che, không bắt chạm nào; state chọn trên asset trong Editor vẫn chạy như trước.
- Đổi scene khi panel đang mở: panel đóng; nút vẫn có ở scene mới.
- Đang chuyển scene (màn Loading đang chạy): nút mở panel được, nhưng mọi dòng khoá tới khi chuyển xong.
- Popup đang mở: nút vẫn chạm được; panel và nền che nằm trên popup.
- Danh sách dài hơn panel: cuộn dọc; không thu nhỏ chữ.
- Không có state nào trong bản build: panel chỉ có dòng "Chơi bình thường" và nhãn "Bản này không có state cheat".
- Thiếu asset config: lỗi ngay lúc khởi động, nêu tên config bị thiếu, như mọi config khác.

## Chưa rõ
- Không còn điểm nào chưa rõ. Người quyết 2026-11-27 (q20261127-b018): giữ luật code, bản phát hành không mang code dựng state cheat; test dùng Development Build. Ở bản phát hành có config bật, panel chỉ có dòng "Chơi bình thường" và nhãn không có state.

## Đã tự quyết
- `cheat_enabled` nằm trong asset config của feature và mặc định bật: bản build hiện đều là bản test. Đã loại: file ngoài trên máy, mặc định tắt (người test phải build lại mới có nút).
- Chỉ config quyết, không theo loại build: đúng yêu cầu của người "có config để bật tắt cả hai". Đã loại: tự ẩn ở bản không phải bản dev.
- Nút mang chữ "Cheat": không cần art.
- Chạm nền che không đóng panel: tránh mất chỗ đang cuộn khi chạm nhầm.
- Khoá dòng khi đang chuyển scene: tránh hai lần chuyển chồng nhau.
