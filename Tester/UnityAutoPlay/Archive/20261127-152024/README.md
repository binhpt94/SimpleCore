# Run: ki luc qua feature-save, popup thua, choi lai
Tester: UnityAutoPlay · Đợt: 20261127-152024

## Mục đích
Đợt tạo trùng: lượt này được server mở lại cho việc `w20261127-151411-unityautoplay` (tin `m20261127-151410-57011f`). Lượt trước của việc đó đã chơi xong ở đợt `20261127-151417`. Kết quả của việc này đọc ở đợt đó.

## Test gì
Không chơi lại. Chỉ kiểm đợt `20261127-151417` còn dùng được không.

## Môi trường
Commit HEAD `3a62c28` cộng thay đổi chưa commit. Không mở scene, không Play.

## Các bước
1. Đọc README và báo cáo (lưu lúc 15:19:56) của đợt `20261127-151417`.
2. Mở lại ảnh `04-lose1-popup.png` và `05-lose2-popup.png` của đợt đó: khớp với kết quả đã ghi, lỗi bố cục dải "Kỉ lục mới!" thấy rõ ở ảnh 04.
3. Tìm file trong `UnityProject/Assets` đổi sau 15:14:11: chỉ có `Features/Hero/Tests/HeroFakes.cs` (15:16:38) và `Features/Wallet/Tests/WalletLogicTests.cs` (15:20:36). Cả hai là file test.
4. Đọc bản lưu `record.save`: `1` / `3`, giờ file vẫn 15:16:35.
5. Đọc console Editor: 0 lỗi, 0 cảnh báo.

## Kết quả
dở: không chơi vì là đợt trùng; kết quả của việc nằm ở đợt `20261127-151417` (đạt).

## Lỗi tìm thấy
không

## Cấu trúc thư mục
- README.md: file này
