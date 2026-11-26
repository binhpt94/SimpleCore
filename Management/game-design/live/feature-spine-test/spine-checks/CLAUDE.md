# Nhân vật thử và các điều kiểm về Spine

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- MỘT nhân vật Spine, BA animation, MỘT nút cho mỗi animation, MỘT nút quay mặt, MỘT nhãn trạng thái.
- Tại mỗi lúc nhân vật chạy đúng MỘT animation và quay MỘT mặt.
- Nhân vật không quan hệ với hero, quái, thành: chúng đi xuyên qua nhau, không ai đánh ai.
- Thứ tự vẽ theo `feature-battlefield/battlefield-layers`: nhân vật ở lớp quái và hero; HUD, popup trên nó.

## Định nghĩa
- Không dùng từ nào của glossary. Các từ riêng của Spine chỉ dùng trong node này và con của nó:
  - Animation lặp: chạy hết thì nối lại từ đầu, không dừng.
  - Animation một lần: chạy hết một lượt thì xong.
  - Chuyển mượt: animation mới bắt đầu từ tư thế đang có, trộn dần trong một khoảng ngắn (con số ở vòng 4).

## Danh sách
- Animation: đứng yên (lặp), chạy (lặp), hành động (một lần). Tên thật trong bộ xương mẫu chốt ở vòng 4.
- Nút: một nút cho mỗi animation, một nút quay mặt.
- Nhãn: một nhãn trạng thái.

## Luật cơ bản
- Màn trận mở: nhân vật đứng yên, quay mặt về một phía cố định.
- Bấm nút animation: đổi sang animation đó, chuyển mượt.
- Animation một lần chạy xong: tự về đứng yên.
- Bấm nút quay mặt: nhân vật lật sang phía kia, animation không đổi.
- Rời màn trận: nhân vật và nút ẩn; lần sau mở lại từ đầu.
- Trận dừng: nhân vật đứng nguyên tư thế, nút không nhận chạm; trận chạy lại: animation chạy tiếp từ đúng chỗ.
- Lần thử đạt khi mọi điều kiểm đạt, trừ `style-fit` ghi riêng. Kết quả, ảnh, clip, số đo ghi ở đợt chơi thử của tester (`Tester/`), đó là phiếu `result-sheet` của feature.

## Đã tự quyết
- **Ba animation**: đủ cả hai kiểu lặp và một lần, và đủ một cặp lặp-lặp để thử chuyển mượt. Đã loại: chỉ một animation (không thử được đổi), bày hết mọi animation của bộ mẫu (nhiều nút, không thêm điều kiểm mới).
- **Vẽ ở lớp unit**: đây là chỗ Spine sẽ đứng nếu được dùng cho hero, quái. Ba chỗ khác trong thứ tự vẽ đã có `feature-cube-test` thử. Đã loại: ba nhân vật ở ba lớp.
- **Chạy tại chỗ**: thử animation, không thử đường đi; tránh đè lên khối thử, decor.
- **Nút trên màn**, không phải phím: chơi thử trên điện thoại và tester bấm được.
- **Dừng theo trận** (`pause-freeze`, theo góp ý GPT `r2-core` ở node feature): Spine dùng cho unit thì phải đứng khi trận dừng. Đã loại: chạy theo thời gian thực bất kể trận dừng.
- **Các điều kiểm mới theo `r2-core`** (`overlap-order`, `flip-anchor`, `mid-mix-switch`, `size-vs-hero`, `narrow-camera`, `material-check`, `style-fit`, `perf-cost`, `touch-lock`, `dev-build-only`, `all-required`): hiện thực các ý cùng tên của node feature thành điều chấm được.
- **Phiếu kết quả là đợt chơi thử của tester**, không làm file riêng: tester đã ghi ảnh, kết quả từng điều, lỗi theo đợt.

## Chưa rõ
- Ngân sách FPS và máy yếu nhất được hỗ trợ (`perf-cost`): chưa có trong thiết kế.
- Bộ xương mẫu cụ thể: chốt ở vòng 4, theo bản runtime @CodeLead cài.
