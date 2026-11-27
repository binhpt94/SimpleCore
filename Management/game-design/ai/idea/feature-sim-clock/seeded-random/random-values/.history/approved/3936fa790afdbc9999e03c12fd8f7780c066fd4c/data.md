---
based-on: 493ba40160e7
frame: none
---
# Thông số seed và dòng ngẫu nhiên

Bản phát hành dùng seed ngẫu nhiên mỗi lượt và ghi lại; bản dev dùng seed cố định trong config và in ra console. Mỗi feature một dòng ngẫu nhiên sinh từ seed của lượt và tên feature.

## Ý cốt lõi

### release-seed-random | AI 8 | Bản phát hành: mỗi lượt bốc một seed mới ngẫu nhiên lúc vào trận
Người quyết (2026-11-25).

### release-seed-logged | AI 8 | Bản phát hành ghi seed của lượt vào log của game lúc vào trận, để lặp lại đúng lượt khi báo lỗi
Người quyết (2026-11-25).

### dev-seed-fixed | AI 8 | Bản dev: seed lấy từ config, cố định, và được in ra console lúc vào trận
Người quyết (2026-11-25). Tester đặt seed khác trong config để chơi lại một lượt đã báo lỗi.

### dev-seed-per-run | AI 7 | Bản dev: mọi lượt trong một lần mở game dùng đúng seed đang ghi trong config
Chơi lại ngay trong cùng lần mở game ra đúng lượt vừa chơi; muốn thứ tự quái khác thì đổi seed trong config.

### seed-range | AI 7 | Seed là một số nguyên từ 0 tới 2147483647
Vừa một số nguyên 32 bit không âm, dễ chép từ log.

### stream-from-name | AI 7 | Dòng của một feature sinh từ seed của lượt và tên feature, không phụ thuộc feature nào xin dòng trước
Đổi thứ tự cài feature không làm lệch số.

### wave-stream | AI 7 | Đợt quái dùng một dòng tên Wave cho cả việc xáo thứ tự loại quái lẫn gán bên ra
Hiện là feature duy nhất cần ngẫu nhiên.
