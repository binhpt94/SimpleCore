# Dòng state và cách áp state
Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Mỗi feature có node cheat khai các state của mình cho panel; panel không giữ danh sách cố định.
- Panel dựng danh sách: dòng "Chơi bình thường", rồi các state theo tên feature A-Z, trong feature theo thứ tự ý.
- Chạm dòng state: nhớ state được chọn, tải lại scene chứa state (chuyển scene nếu cần), feature sở hữu dựng state khi scene chạy.
- Chạm "Chơi bình thường": quên state, chạy lại game từ scene đầu tiên.
- Chỗ dựng mỗi state vẫn là code cheat sẵn có của feature sở hữu; panel chỉ chọn state nào và lúc nào.

## Thông số
- Không có số. Chữ của dòng lấy từ tên feature và tiêu đề ý ở node cheat, không có bảng riêng.

## Thông số theo giai đoạn
- Không có thông số theo giai đoạn: luật của node như nhau ở mọi giai đoạn.

## Trường hợp biên
- Không có state nào: chỉ có dòng "Chơi bình thường".
- State thuộc scene trận cần đội: state tự ép đủ giá trị nó cần như khi chọn trên asset; panel không ép thêm.
- Chọn state khi đang chuyển scene: dòng đang khoá (xem `ui.md` của node config, nút, panel), không có gì xảy ra.
- Chạm dòng của state đang chạy: dựng lại từ đầu.
- State có ở thiết kế nhưng code của feature chưa làm: không có dòng.
- State cheat không ghi xuống máy: kỉ lục thật trên máy giữ nguyên sau khi chơi state.

## Chưa rõ
- Không còn điểm nào chưa rõ.

## Đã tự quyết
- Luôn tải lại scene khi áp state: một đường duy nhất, giống hệt cách chọn state trên asset. Đã loại: ép state lên scene đang chạy (mỗi feature phải biết cách quay lui trạng thái hiện tại).
- "Chơi bình thường" chạy lại từ scene đầu: sạch nhất. Đã loại: tải lại scene đang mở không có state (một số scene không tự đứng được khi không có scene trước).
- Không ghi xuống máy trong state cheat: tránh hỏng kỉ lục thật của máy test.
