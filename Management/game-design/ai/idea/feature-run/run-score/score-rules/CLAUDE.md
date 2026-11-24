# Cách đếm điểm và lưu kỉ lục

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Đặt điểm về 0 khi lượt mới bắt đầu.
- Nghe báo dọn sạch đợt từ `feature-wave`; cộng 1 điểm mỗi lần, chỉ khi lượt chưa kết thúc.
- Khi lượt kết thúc: chốt điểm, so với kỉ lục, cập nhật kỉ lục và cờ kỉ lục mới, ghi kỉ lục xuống máy nếu đổi.
- Lúc mở game: đọc kỉ lục đã lưu (không có hoặc hỏng thì 0).

## Thông số
| Thông số | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Điểm đầu lượt | 0 | đợt | Chưa dọn đợt nào |
| Điểm cộng mỗi lần dọn sạch | 1 | đợt | Điểm = số đợt đã dọn |
| Kỉ lục mặc định | 0 | đợt | Chưa chơi lượt nào |
| Kiểu số | số nguyên không âm | | Không có điểm lẻ |
| Số kỉ lục lưu | 1 cho cả game | | Không theo đội |
| Lúc ghi kỉ lục | ngay khi đổi | | Người chơi hay tắt ngang |

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Đếm điểm | Có | Có | Có |
| Kỉ lục nhớ trong lúc game mở | Có | Có | Có |
| Kỉ lục lưu khi tắt game | Không bắt buộc | Có | Có |
| Cờ kỉ lục mới | Có | Có | Có |

## Trường hợp biên
- **Thua ở đợt 1:** điểm 0, cờ tắt, kỉ lục giữ.
- **Điểm bằng kỉ lục:** không phải kỉ lục mới.
- **Dữ liệu lưu là số âm hoặc không phải số:** coi kỉ lục là 0, ghi đè ở lần phá kỉ lục kế tiếp.
- **Ghi xuống máy thất bại:** kỉ lục trong lúc game mở vẫn đúng; không báo lỗi cho người chơi.
- **Báo dọn sạch tới hai lần cho cùng một đợt:** chỉ cộng một lần (đợt sau phải có số lớn hơn đợt đã tính).
- **Bỏ lượt ngay khi vào trận:** điểm 0, như thua ở đợt 1.
- **Thành bị phá lần đầu rồi hồi sinh:** điểm không chốt, không đặt lại; cộng tiếp từ số đang có. Kỉ lục chỉ so khi lượt kết thúc thật.
- **Đợt dọn sạch cùng khung hình thành bị phá lần đầu:** giữ chờ: hồi sinh thành công thì cộng; chốt thua thì không cộng, theo luật "thua trước" (ý `same-tick-clear` của `feature-popup-revive`; sửa 2026-11-25).

## Đã tự quyết
- **Kỉ lục hỏng coi là 0.** Không chặn người chơi vì lỗi lưu. Đã loại: hiện thông báo lỗi.
- **Ghi trước khi popup thua hiện.** Tránh mất kỉ lục khi tắt game giữa popup.
- **Chống cộng trùng theo số đợt.** Phòng khi `feature-wave` báo lặp.

## Chưa rõ
- Không có điểm chặn.
