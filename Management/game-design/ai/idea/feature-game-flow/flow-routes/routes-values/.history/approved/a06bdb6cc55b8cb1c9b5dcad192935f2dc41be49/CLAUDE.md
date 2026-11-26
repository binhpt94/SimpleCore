# Thông số đổi màn

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Giữ màn chính đang hiện (bắt đầu, chọn đội, trận) và cờ khoá.
- Nhận yêu cầu theo bảng của node cha; đang khoá thì bỏ.
- "Vào trận": giao đội cho `feature-run`, chờ báo dựng xong (tối đa 3 giây), rồi ẩn màn chọn đội, hiện màn trận.
- Lựa chọn rời trận tới qua `feature-run` sau khi dọn trận: hiện màn đích ngay.

## Thông số
| Thông số | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Thời gian đổi màn | 0 (cùng khung hình) | giây | Mức đơn giản nhất, khớp `feature-run/run-flow/flow-rules` |
| Hạn dựng trận | 3 (dự kiến) | giây | Mọi thứ đã tải ở bước tải của scene; dựng trận chỉ là tạo object |
| Số lần thử lại khi dựng lỗi | không giới hạn, mỗi lần một chạm | lần | Người chơi tự bấm lại |
| Nút "Quay lại" | góc trên trái màn chọn đội, cạnh tối thiểu 120 px | px | Cỡ chạm tối thiểu của HUD và popup |
| Dòng lỗi dựng trận | "Không vào được trận, thử lại" | | Người chơi biết bấm lại |

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Ai đổi màn | `feature-run` (chỉ có màn trận; mở game và chơi lại vào thẳng trận) | Điều hướng màn, theo bảng của node cha | Như PROTOTYPE |
| Nút "Quay lại" ở màn chọn đội | Không (chưa có màn chọn đội) | Có | Có |
| Hiệu ứng đổi màn | Không | Không | Mờ dần ra rồi vào, 0.3 giây mỗi chiều (dự kiến), khoá trong lúc chạy |

## Trường hợp biên
- **Bấm "Vào trận" hai lần nhanh:** lần hai rơi vào lúc khoá, bị bỏ; chỉ một trận được dựng.
- **Bấm "Quay lại" đúng lúc đang dựng trận:** bị bỏ vì đang khoá.
- **Dựng trận lỗi rồi bấm "Quay lại":** về màn bắt đầu; đội được nhớ vẫn là đội của lần vào trận trước.
- **Rời game khi đang dựng trận:** thời gian ở ngoài không tính vào hạn 3 giây.
- **Báo dọn trận xong tới khi không đang ở màn trận (lỗi):** bỏ, ghi lỗi.

## Đã tự quyết
- **Ở IDEA, `feature-run` giữ việc đổi màn.** IDEA chỉ có màn trận (quyết định 2026-11-25 với @Manager). Từ PROTOTYPE bảng đường đi chạy ở đây; nhánh luồng màn của `feature-run` chỉ còn phần trong trận. Đã ghi ở `CLAUDE.md` của nhánh đó; khi tới PROTOTYPE, ý của nhánh đó sẽ được sửa cho khớp (đổi hash, phiên Run sửa code).
- **Nút "Quay lại" ở màn chọn đội.** Theo ý `back-from-team` (vòng 2). Trước đây nhánh nhớ đội của `feature-team-select` ghi không có nút này; đã ghi lệch ở `CLAUDE.md` của nhánh chỗ trong đội và nhánh nhớ đội.
- **Dựng lỗi thì ở lại, không về màn bắt đầu.** Theo ý `retry-on-build-fail` (vòng 2): bấm lại ngay với cùng đội.

## Chưa rõ
- Không có điểm chặn.
