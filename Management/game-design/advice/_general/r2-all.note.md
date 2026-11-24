# Nhận xét của @DesignAdvisor về _general/r2-all

Nhận xét cho người chấm. Không phải lời của GPT, và không phải lí do để bỏ góp ý.
Góp ý ở `_general` không gắn node nào: @GameDesigner đưa từng góp ý vào feature đích, hoặc ghi ở `CLAUDE.md` gốc vì sao không dùng.

- **`chu-so-huu-ban-do`** (thêm feature mới `feature-battlefield`): luật chia vòng 2 cho phép tách việc cắt ngang nhiều feature. Hiện các mốc không gian chung của chiến trường đã có chỗ giữ: `feature-castle/castle-core/castle-body-values` có ghi "node này giữ các hằng chung của chiến trường" (mép thành, mép màn), các node khác `use` lại. GPT tự ghi là chưa đối chiếu nhánh dưới. Nếu nhận, đây là feature mới, phải làm cả nhánh vòng 3-4 và kéo các frame đang `use` số của castle.
- **`chu-so-huu-doi-duoc-nho`**: GPT lại đọc `run-reset` ở gốc là "chỉ giữ kỉ lục", giống `chon-san-doi-mac-dinh` của `feature-team-select/r2-1` (ý đó đã bị loại, AI 4). Lần này GPT không bỏ nhớ đội; GPT đề nghị sửa `run-reset` và `run-record` cho rõ đội nhớ chỉ là lựa chọn điền sẵn, không phải tiến trình. Lời đề nghị đó chạm node gốc.
- **`yeu-cau-tam-dung`** và **`quyen-chot-luot`**: đi cùng hướng đã nhận ở `feature-run/r2-1` (`trang-thai-tam-dung`, `bo-luot-ve-man-bat-dau`). Đây là phần mới: ai nhận yêu cầu dừng, ai chốt lượt khi thành bị phá sát lúc dọn đợt.
- **`chu-so-huu-thuong-tien`**: gần với `enemy-wave-clear` ở `feature-enemy/r2-1` (quái báo bị hạ cho đợt). Phần mới: chỉ ví ghi khoản thưởng.
