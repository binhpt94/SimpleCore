# Popup hồi sinh

Dữ liệu cốt lõi: [data.md](data.md)

## Người chơi được gì
- Cơ hội thứ hai đúng lúc thua: xem một quảng cáo là thành đứng lại, đội và nâng cấp giữ nguyên.
- Quyền chọn: không muốn xem thì thua và xem kết quả như thường.

## Liên quan
- `feature-run`: chốt thành bị phá, dừng trận, mở popup này lần đầu trong lượt, nhận lựa chọn.
- `feature-ads`: hiện quảng cáo và báo kết quả.
- `feature-castle`: thành hồi sinh với một phần máu.
- `feature-popup-lose`: tới sau khi bỏ qua hoặc quảng cáo không xem được.
- `feature-popup`: dịch vụ hiện popup.

## Đã tự quyết
- **Tạo feature theo yêu cầu của người (2026-11-24): "xem quảng cáo nhận hồi sinh thành với 30% hp".** Luật vòng 2: mỗi popup là một feature; tách khỏi popup thua để popup thua chỉ hiện khi lượt đã thật sự thua. Đã loại: thêm nút "xem quảng cáo" vào popup thua (popup thua lúc đó báo kết quả của một lượt còn có thể chơi tiếp).
- **Mức máu khi hồi sinh (30% máu tối đa theo yêu cầu) để ở vòng 4 của nhánh thành.** Vòng 2 không ghi con số.
- **Không có đồng hồ tự đóng popup.** Trận đã dừng; người chơi chủ động chọn. Xét lại ở vòng 4 nếu cần.
- `code.json`: `PopupRevive`, theo luật đặt tên vòng 2.

- **Đưa góp ý GPT lần hỏi `r2-1` (2026-11-24, 10 góp ý).** Thứ tự dừng trận, khoá nút, đóng popup rồi mới báo, và cách tính điểm, kỉ lục khi có hồi sinh (AI 6-7). Hai góp ý "làm rõ" viết lại chính ý cũ (`revive-offer`: nút từ chối ghi "Kết thúc lượt"; `revive-ad-result`: mốc chốt thua). Góp ý `moc-chot-thua` còn đề nghị sửa ý `lose-condition` ở node gốc; không sửa gốc vì ý `ad-revive` ở gốc đã nêu ngoại lệ hồi sinh. `revive-before-pause` đi ngược ý `lose-before-pause` ở `feature-popup` (thua trước tạm dừng): hai ý không trái nhau vì thua chỉ chốt khi không còn hồi sinh, nhưng cần ghi lại ở vòng 3.

## Chưa rõ
- Thứ tự giữa hồi sinh, tạm dừng và chốt thua cùng một nhịp: vòng 3, cùng `feature-run`.
