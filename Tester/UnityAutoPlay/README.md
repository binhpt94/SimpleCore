# UnityAutoPlay
Phiên `FEATURE=@UnityAutoPlay` (tên chat `UnityAutoPlay`): chơi game trong Unity Editor qua unity-mcp như một người chơi, đối chiếu với thiết kế đã chốt (`Management/game-design/live`), và ghi lại điều đã thấy. Không sửa code, asset hay scene.

## Test gì
- Từng feature ở mức được giao (IDEA, PROTOTYPE, FULL): các state của node `<feature>/cheat`, luồng người chơi thấy được, lỗi và cảnh báo trong console.
- Luồng nối nhiều feature (ví dụ Home → chọn đội → trận → thua): chuyển màn, dữ liệu mang theo.

## Cấu trúc thư mục
- `README.md`: file này.
- `<yyyymmdd-hhmmss>/`: một đợt test. `README.md` của đợt có 7 mục: Mục đích, Test gì, Môi trường, Các bước, Kết quả, Lỗi tìm thấy, Cấu trúc thư mục. Ảnh và dữ liệu phụ nằm trong `images/`.
- `Archive/`: đợt cũ đã dời đi; chỉ nhóm Testers đọc. `Archive/README.md` là danh mục.

## Đọc một đợt
Xem dòng đầu mục "Kết quả" (`đạt`, `không đạt`, `dở`), rồi mục "Lỗi tìm thấy": mỗi lỗi ghi bước tái hiện, ảnh và feature sở hữu. `python Management/tools/tester.py status UnityAutoPlay` in tóm tắt mọi đợt đang mở.
