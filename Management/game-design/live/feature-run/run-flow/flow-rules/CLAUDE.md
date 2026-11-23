# Luật chuyển màn của lượt

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Giữ một trạng thái màn hiện tại trong scene Game: màn bắt đầu, màn chọn đội, trận (đang đánh / tạm dừng / đã kết thúc).
- Nhận báo bấm nút từ `feature-home`, `feature-team-select`, `feature-popup-lose`, `feature-popup-pause` và báo thành bị phá từ `feature-castle`; quyết định màn tiếp theo theo bảng ở node cha.
- Khi vào trận: dựng trận mới, báo lượt mới (`feature-run/run-start`).
- Khi thành bị phá: dừng trận ngay, chốt điểm (`feature-run/run-score`), chờ 1 giây, nhờ `feature-popup` hiện popup thua.
- Khi rời trận (chơi lại, về màn bắt đầu, bỏ lượt): đóng popup đang mở, huỷ mọi thứ của trận, hiện màn đích.

## Thông số
| Thông số | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Số scene | 2 (Loading, Game) | scene | Theo `feature-scene-flow/scene-list` |
| Màn đầu tiên trong scene Game | Màn bắt đầu | | `flow-order` |
| Độ trễ trước popup thua | 1 (dự kiến) | giây | Đủ thấy thành bị phá, không làm chậm chơi lại |
| Số lần bấm có tác dụng trong lúc đổi màn | 1 | lần | Tránh mở trận hai lần |
| Thời gian đổi màn | 0 (đổi ngay) | giây | Mức đơn giản nhất; hiệu ứng ở bảng giai đoạn |

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Màn sau Loading | Vào thẳng trận với đội mặc định (`feature-team-select/team-memory/memory-rules`) | Màn bắt đầu | Màn bắt đầu |
| Chơi lại ở popup thua | Vào thẳng trận mới với đội mặc định | Sang màn chọn đội | Sang màn chọn đội |
| Về màn bắt đầu ở popup thua | Không có (chưa có màn bắt đầu) | Có | Có |
| Bỏ lượt | Không có (chưa có tạm dừng) | Không bắt buộc | Có |
| Độ trễ trước popup thua | 1 giây | 1 giây | 1 giây |
| Hiệu ứng đổi màn | Không | Không | Mờ dần ra rồi vào, 0.3 giây mỗi chiều (dự kiến) |

## Trường hợp biên
- **Thành bị phá cùng khung hình với lúc dọn sạch đợt:** xử lí thành bị phá trước; đợt đó không được tính vào điểm.
- **Thành bị phá khi popup tạm dừng đang mở:** không xảy ra, vì lúc tạm dừng quái không đánh (`feature-run/run-pause`).
- **Rời game đúng lúc thành vừa bị phá (trong 1 giây chờ):** không mở popup tạm dừng; quay lại thì popup thua hiện (lượt đã kết thúc, tạm dừng không áp dụng).
- **Bấm "Chơi lại" và "Về màn bắt đầu" gần như cùng lúc:** chỉ lần bấm đầu tính (`transition-lock`).
- **Đang ở màn bắt đầu hoặc màn chọn đội mà rời game:** không có gì dừng; quay lại vẫn ở màn đó.
- **Nút quay lại của máy (Android):** ở màn bắt đầu, màn chọn đội, popup thua và trong 1 giây chờ: không làm gì. Trong trận đang đánh: như bấm nút tạm dừng. Trên popup tạm dừng: theo `feature-popup-pause/pause-popup/pause-popup-spec`.

## Đã tự quyết
- **Mọi màn trong scene Game, không tách scene trận.** Theo `feature-scene-flow/scene-list` và ý `flow-scenes` của node cha (đã sửa cho khớp, 2026-11-23). Lượt mới luôn sạch vì trận được dựng mới và huỷ hết mỗi lần (`fresh-battle`). Đã loại: tải lại scene Game mỗi lượt (chậm, trái `no-return-loading`).
- **Chờ 1 giây trước popup thua.** Popup hiện ngay thì người chơi không hiểu vì sao thua. Đã loại: 3 giây (chậm chơi lại).
- **Bỏ qua báo dọn sạch tới sau khi chốt điểm.** Điểm chỉ chốt một lần.
- **Nút quay lại của máy trong trận = tạm dừng.** Thói quen điện thoại; không thoát nhầm khỏi lượt.

- **Rà lại theo `flow-scenes` mới của node cha (2026-11-23).** Các ý đã khớp sẵn; chỉ ghi `based-on` mới và xoá ghi chú mâu thuẫn.

## Chưa rõ
- Không có điểm chặn.
