# Run kỉ lục qua feature-save (IDEA)
Tester: UnityAutoPlay · Đợt: 20261127-152335

## Mục đích
Đợt trùng. Server mở lại việc `w20261127-151411-unityautoplay` (tin `m20261127-151410-57011f`) đã chơi xong. Thư mục này được tạo trước khi tester đọc đợt cũ.

## Test gì
Không test gì. Kết quả của việc nằm ở đợt `20261127-151417`.

## Môi trường
Commit HEAD `b5f2983` cộng thay đổi chưa commit. Không mở scene, không Play.

## Các bước
1. Đọc README đợt `20261127-151417` và hai báo cáo gần nhất của tester.
2. Tìm file trong `UnityProject/Assets` (bỏ `.meta`) đổi sau 15:14:00: chỉ thấy `Hero/Tests/HeroFakes.cs` và `Wallet/Tests/WalletLogicTests.cs`.
3. Đọc `editor_ticket.py` (phần đầu và hàm `release`), `worker.py` (hàm `start_play`) và phiếu đang được cấp `t20261127-152327-935c`: phiếu này là "phần còn lại của t20261127-152224-dff2", còn nguyên bit "chơi thử".
4. Phần "chơi thử" đã làm xong, nên sau khi ghi và lưu trữ thư mục này, tester đánh dấu xong trên phiếu của mình bằng `editor_ticket.py done` (lệnh dành cho bên giữ phiếu). Kết quả của lệnh ghi trong báo cáo cuối lượt.

## Kết quả
dở: không chơi, vì việc đã được chơi và ghi ở đợt `20261127-151417` (đạt).

## Lỗi tìm thấy
- Lỗi công cụ (không thuộc feature game): `start_play` trả phiếu mà không truyền `met`, nên phiếu chơi thử được xếp lại và server mở lại việc đã xong. Đây là kết luận từ đọc code và phiếu.

## Cấu trúc thư mục
- README.md: file này
- images/: trống
