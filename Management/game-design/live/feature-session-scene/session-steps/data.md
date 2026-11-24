---
based-on: 2f0485d8eda5
refs: glossary#participant@418cacc16440
---
# Ba bước của một scene và feature tham gia

Mỗi scene có một danh sách feature tham gia. Dịch vụ này đưa cả danh sách đi qua ba bước tải, chạy, huỷ theo cùng một thứ tự; feature nào có view hoặc cần tải đều phải tham gia.

## Ý cốt lõi

### participant-list | AI 8 | Mỗi scene có một danh sách feature tham gia, cố định cho scene đó
Scene Loading và scene Game mỗi scene một danh sách riêng. Đã loại: feature tự đăng kí lúc chạy (khó biết scene gồm những gì).

### step-load | AI 8 | Bước tải: mọi feature tham gia tải xong thứ mình cần thì scene mới sang bước chạy
Tải gồm hình, prefab, cấu hình. Không feature nào chạy khi feature khác còn đang tải.

### step-run | AI 8 | Bước chạy: các feature tạo view của mình; scene không có view đặt sẵn
View chỉ sinh ra ở bước này. Đã loại: đặt sẵn view trong scene (khó dọn, khó biết ai sở hữu).

### step-dispose | AI 8 | Bước huỷ: mọi feature tham gia huỷ view và bỏ thứ mình đã tải
Chạy khi rời scene, do `feature-scene-flow` yêu cầu.

### fixed-order | AI 7 | Các feature đi qua mỗi bước theo thứ tự của danh sách, huỷ theo thứ tự ngược lại
Feature đứng sau dùng được thứ feature đứng trước đã chuẩn bị; huỷ ngược để không feature nào mất thứ nó đang dựa vào.
