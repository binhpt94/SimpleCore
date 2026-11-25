# Thông số nhịp chung

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
1. Đọc config chung: cách chạy (bước cố định hoặc theo khung hình), số bước mỗi giây, trần số bước mỗi khung, cờ dev.
2. Mỗi khung hình:
   - Bước cố định: cộng thời gian khung (kẹp ở 0.125 giây) vào phần chờ; còn đủ 0.025 giây thì chạy một bước, tối đa 5 bước.
   - Theo khung hình: chạy đúng một bước dài bằng thời gian khung (kẹp ở 0.125 giây).
3. Mỗi bước: áp các thao tác trong hàng chờ chung theo thứ tự đã đưa vào (mỗi thao tác ghi kèm số bước nó được áp), rồi gọi các feature theo thứ tự ở `feature-sim-clock/clock-step` (mục Danh sách), tăng số bước đã chạy thêm 1.
4. Phần nhìn đọc trạng thái sau bước cuối của khung.
5. Bản dev: nhận tốc độ tua, lệnh chạy N bước, đóng băng, chạy một bước.

## Thông số
Bảng `ClockParams` trong `.excel/` (loại `params`): `tick_rate`, `step_seconds`, `default_mode`, `modes`, `max_steps_per_frame`, `max_frame_seconds`, các thông số `dev_*` và `view_interpolation`. Cột `reason` ghi lí do từng số.

## Thông số theo giai đoạn
Ba cột `idea`, `prototype`, `full` của bảng `ClockParams`. Chỉ `view_interpolation` đổi theo giai đoạn: FULL làm mượt hình giữa hai bước.

## Trường hợp biên
- **Khung đầu tiên sau khi tải scene dài bất thường** (vài giây): bị kẹp ở 0.125 giây, chạy tối đa 5 bước; trận không nhảy.
- **Game rời khỏi màn hình rồi quay lại:** lượt tự tạm dừng theo `feature-run`; nhịp vẫn chạy nhưng gameplay đứng yên; thời gian ngoài game bị kẹp như trên.
- **Khung không đủ 0.025 giây:** không chạy bước nào; phần thời gian giữ lại cho khung sau.
- **Thao tác bấm đúng lúc lượt vừa kết thúc trong bước trước:** thao tác bị bỏ theo luật của feature nhận nó (ví dụ nút kĩ năng đã khoá).
- **Đổi cách chạy trong lúc đang chơi:** không hỗ trợ; config đọc một lần khi vào trận.
- **Tua 16 lần trên máy chậm:** vẫn kẹp ở 0.125 giây thời gian khung; chạy tối đa 80 bước. Kết quả gameplay không đổi vì mỗi bước vẫn dài 0.025 giây.
- **Chạy N bước khi đang tạm dừng:** bước vẫn chạy, gameplay vẫn đứng yên theo luật tạm dừng.
- **Ở cách theo khung hình:** kết quả không lặp lại giữa các máy; dùng khi cần thử cảm giác theo khung hình, không dùng để kiểm cân bằng.

## Đã tự quyết
- **Trần 5 bước, kẹp 0.125 giây.** Giữ đúng trần cũ của `feature-combat/combat-tick/tick-values`; nay giữ ở đây cho mọi logic gameplay (@CodeLead đồng ý).
- **Đếm bước bằng số nguyên.** Đã loại: cộng dồn giây số thực (lệch dần sau hàng nghìn bước).
- **Chờ giờ hết ở bước đầu tiên đạt hoặc vượt.** Một luật cho mọi feature, đổi giây sang số bước không cần làm tròn riêng.
- **Tốc độ tua dự kiến 1 tới 16 lần.** Đã loại: tua không giới hạn (máy dev treo khung hình lâu, khó theo dõi).
- **Cách chạy mặc định là bước cố định.** Người quyết (2026-11-25), không phải tự quyết.

## Chưa rõ
- Không có điểm chặn.
