# Thông số seed và dòng ngẫu nhiên

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
1. Khi lượt chơi yêu cầu dựng lượt mới, chọn seed của lượt:
   - Bản phát hành: bốc một seed ngẫu nhiên trong khoảng cho phép.
   - Bản dev: lấy đúng seed trong config.
2. Đặt lại mọi dòng: dòng của mỗi feature sinh từ seed của lượt và tên dòng (bảng `Streams`).
3. Khi lượt chơi xác nhận lượt đã bắt đầu: ghi seed đúng một lần (bản phát hành vào log, bản dev ra console).
4. Feature cần số ngẫu nhiên xin dòng theo tên của mình; mỗi lần bốc lấy số kế tiếp trong dòng đó.

## Thông số
- Bảng `SeedParams` (loại `params`): nguồn seed ở bản phát hành và bản dev, seed mặc định dev, seed mỗi lượt ở dev, khoảng giá trị.
- Bảng `Streams` (loại `list`): mỗi dòng ngẫu nhiên một hàng; `owner` là feature xin dòng, `uses` là việc dùng nó. Feature mới cần ngẫu nhiên thì thêm một hàng ở đây.

## Thông số theo giai đoạn
Ba cột `idea`, `prototype`, `full` của bảng `SeedParams` (mọi thông số giữ như IDEA); cột `stage` của bảng `Streams`.

## Trường hợp biên
- **Chơi lại sau khi thua (cùng lần mở game):** lượt mới. Bản phát hành bốc seed mới; bản dev dùng lại đúng seed config nên ra đúng lượt vừa chơi nếu thao tác như cũ.
- **Dựng trận hỏng trước khi lượt bắt đầu:** không ghi seed; lần dựng sau chọn seed lại theo luật.
- **Tester muốn chơi lại một lượt bất kì:** đặt seed của config bằng seed đã in (hoặc đã ghi trong log), vào trận.
- **Feature xin dòng có tên không có trong `Streams`:** bản dev báo lỗi rõ để thêm vào bảng; không tự tạo dòng ngầm.
- **Hiệu ứng chỉ để nhìn cần ngẫu nhiên:** dùng nguồn riêng của phần nhìn, không xin dòng ở đây.
- **Huỷ trận:** các dòng của lượt bị dọn; lượt sau khởi tạo lại.

## Đã tự quyết
- **Seed phát hành ngẫu nhiên mỗi lượt, ghi log; seed dev cố định, in console.** Người quyết (2026-11-25), không phải tự quyết.
- **Mọi lượt dev dùng đúng seed config (2026-11-25, theo góp ý GPT `seed-dev-dung-config`, ý `seeded-rng` của node feature).** Thay cho bản trước "lượt thứ k dùng seed config cộng (k − 1)" (đề nghị của @CodeLead). Lí do: khớp đúng chữ "bản dev dùng seed cố định" của người, và tester lặp lại được một lượt ngay trong cùng lần mở game. Đã loại: cộng số lượt (seed đổi giữa các lượt dù config giữ nguyên).
- **Ghi seed lúc lượt bắt đầu, không lúc dựng trận** (góp ý `chu-so-huu-ban-ghi-seed`).
- **Seed mặc định dev 12345 (dự kiến).**
- **Dòng sinh từ tên, không từ thứ tự xin.** Đã loại: chia dòng theo thứ tự cài feature (đổi thứ tự cài là lệch số).
- **Bỏ tạm dừng, quảng cáo và popup hồi sinh (2026-11-26, người quyết).** Bỏ trường hợp biên hồi sinh thành bằng quảng cáo.

## Chưa rõ
- Không có điểm chặn.
