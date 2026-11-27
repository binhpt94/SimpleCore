---
based-on: 75d09f183702
---
# State trong panel cheat

Panel cheat là danh sách các state cheat của các feature; chạm một state thì game chuyển tới state đó.

## Ý cốt lõi

### states-from-features | AI 8 | Mỗi state ở node cheat của một feature là một dòng trong panel
Feature thêm hay bỏ state ở node cheat của mình thì panel đổi theo, panel không giữ danh sách riêng.

### states-built-in-build | AI 8 | Panel chỉ liệt kê state mà bản build đang chạy dựng được
Không có dòng nào chạm vào mà không có gì xảy ra.

### states-feature-builds | AI 8 | Chạm một dòng thì feature sở hữu state đó dựng state, panel không tự ép giá trị
Giữ đúng một chỗ dựng mỗi state, giống khi chọn trên asset trong Editor.

### states-one-at-a-time | AI 7 | Mỗi lần chỉ có một state cheat; chọn state mới thì bỏ state đang có
Không chồng hai state lên nhau.

### states-normal-play | AI 7 | Panel có một dòng chơi bình thường để bỏ state cheat và quay lại game không cheat

### states-close-after-pick | AI 7 | Chọn một dòng xong thì panel tự đóng
Người test thấy ngay state vừa chọn.
