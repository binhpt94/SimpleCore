---
tool: claudeimage
kind: art
count: 1
deliver: concept
format: png 1920x1080
style: ui-flat
design: feature-popup-lose/lose-result/result-spec
---
# Claude concept: chữ kết quả của popup thua

## Mô tả
Hình tham chiếu mức IDEA cho phần chữ kết quả của popup thua. Bố cục lấy đúng frame `main`, đọc bằng `python Management/tools/gd_frame.py live/feature-popup-lose/lose-result/result-spec`.

Phần node này tự định đều là dòng chữ (`label`): tiêu đề, dòng điểm, dòng kỉ lục, và dòng "Kỉ lục mới!" khi node đã Sync. Tool tự vẽ chữ, nên bảng phần có thể rỗng (`{"parts": []}`). Chỉ khai một phần riêng khi một dòng cần hình nền, ví dụ dải nền nổi bật cho dòng "Kỉ lục mới!".

Khung popup và hai nút mượn số của node khác, nên giữ hình cơ bản. Hai nút đã có phần ở `feature-popup-lose/lose-actions/actions-spec`, dùng lại bằng `same-as` nếu cần. Nền che phủ màn thì bỏ qua.

## Danh sách
- main: các dòng chữ kết quả của popup thua, đúng bố cục frame `main`

## Tiêu chí nhận
- Ảnh đúng 1920x1080, các dòng chữ đúng vị trí và cỡ của frame.
- Nếu có phần được vẽ: theo style `ui-flat`, viền tối thiểu 3 px, không gradient, không glow.
