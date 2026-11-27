# Run: kỉ lục qua feature-save, popup thua và chơi lại
Tester: UnityAutoPlay · Đợt: 20261127-152230

## Mục đích
Đợt trùng. Server mở lại việc `w20261127-151411-unityautoplay` (tin `m20261127-151410-57011f`) đã chơi xong. Thư mục này được tạo trước khi tester thấy đợt cũ.

## Test gì
Không test gì. Kết quả của việc nằm ở đợt `20261127-151417`.

## Môi trường
Commit HEAD `b5f2983` cộng thay đổi chưa commit. Không mở scene, không Play.

## Các bước
1. Đọc README đợt `20261127-151417`, các báo cáo đã lưu và `job.json`.
2. Tìm file trong `UnityProject/Assets` (bỏ `.meta`) đổi sau 15:14:00: chỉ thấy `Hero/Tests/HeroFakes.cs` và `Wallet/Tests/WalletLogicTests.cs`.
3. Đọc các phiếu Editor, `editor_ticket.py` (hàm `release`) và `worker.py` (hàm `start_play`) để tìm vì sao việc bị mở lại:
   - `start_play` trả phiếu bằng `editor_ticket.release(root, PLAY_ROLE)`, không truyền `met`.
   - Bit việc `chơi thử` (code 1024) vì vậy còn nguyên, và `release` tạo phiếu mới ghi "phần còn lại của <phiếu cũ>".
   - Chuỗi phiếu trên máy khớp: `t20261127-151411-3919` → `151955-9895` → `152108-94f6` → `152224-dff2`.
   - Đây là kết luận từ đọc code và phiếu; tester không chạy thử hàm.

## Kết quả
dở: không chơi, vì việc đã được chơi và ghi ở đợt `20261127-151417` (đạt).

## Lỗi tìm thấy
- Lỗi công cụ (không thuộc feature game): phiếu chơi thử không được đánh dấu xong khi lượt kết thúc, nên server mở lại việc nhiều lần. Chi tiết ở bước 3.

## Cấu trúc thư mục
- README.md: file này
- images/: trống
