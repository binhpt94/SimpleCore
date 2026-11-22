# Các bước và luật của một lần chuyển

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
Một lần chuyển, theo đúng thứ tự:
1. Nhận tên scene đích. Kiểm: tên có trong danh sách, không đang chuyển, không trùng scene đang chạy. Sai thì từ chối theo `reject-*`, không làm bước nào tiếp.
2. Hiện lớp che (tức thì). Từ lúc này mọi lần bấm bị lớp che chặn.
3. Nhờ `feature-session-scene` huỷ scene cũ (bước huỷ của mọi feature tham gia).
4. Nhờ `feature-session-scene` tải scene mới; thanh tiến độ trên lớp che tăng theo số feature tham gia đã tải xong.
5. Nhờ `feature-session-scene` cho scene mới chạy (các feature tạo view).
6. Bỏ lớp che (tức thì). Lần chuyển kết thúc; từ đây nhận được yêu cầu chuyển mới.

Lớp che là thứ duy nhất của feature này sống qua lúc chuyển: nó không thuộc scene cũ hay scene mới, chỉ tồn tại từ bước 2 tới bước 6.

## Thông số
| Thông số | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Số bước một lần chuyển | 6 | bước | Như mục "Làm gì" |
| Thời gian hiện, ẩn lớp che | 0 | giây | Tức thì; đơn giản, không có lúc che dở |
| Độ che của lớp che | 100 | % | Không thấy scene dở dang |
| Số lần chuyển cùng lúc | 1 | lần | Theo `one-switch-at-a-time` |
| Số yêu cầu chờ | 0 | yêu cầu | Không hàng chờ |
| Số lần chuyển trong một lần chạy game | 1 (Loading → Game) | lần | Theo `feature-scene-flow/scene-list` |

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Hiện, ẩn lớp che | Tức thì | Tức thì | Mờ dần 0,2 giây (dự kiến) |
| Nội dung lớp che | Tên game dạng chữ + thanh tiến độ | Như IDEA | Như màn Loading bản đủ art |
| Dòng lỗi khi chuyển hỏng | Nêu tên feature lỗi | Như IDEA | Chữ thân thiện, tên feature chỉ vào log |

## Trường hợp biên
- **Hai feature xin chuyển trong cùng một khung hình**: yêu cầu đến trước được nhận, yêu cầu sau bị bỏ qua theo `reject-busy`.
- **Xin chuyển trong lúc đang huỷ hoặc đang tải**: bỏ qua, ghi cảnh báo.
- **Một feature của scene cũ huỷ bị lỗi**: ghi lỗi, vẫn huỷ tiếp các feature còn lại và đi tiếp sang bước tải (`feature-session-scene/session-steps/participant-order`).
- **Một feature của scene mới tải lỗi hoặc quá 10 giây (dự kiến)**: các feature đã tải xong của scene mới được huỷ theo thứ tự ngược; lớp che giữ nguyên và hiện dòng lỗi; không bước nào tiếp theo được làm.
- **Người chơi rời game lúc đang chuyển**: quay lại thì chuyển tiếp từ bước đang dở.
- **Bấm lên lớp che**: không có tác dụng gì, không chuyển xuống dưới.

## Đã tự quyết
- **Lớp che tức thì.** Chỉ có một lần chuyển, ngay lúc mở game; hiệu ứng không đáng làm sớm. Đã loại: mờ dần ngay từ đầu.
- **Lớp che có cùng nội dung màn Loading.** Lần chuyển duy nhất là Loading → Game, người chơi thấy một màn liền. Đã loại: lớp che trơn không chữ (thấy hai màn khác nhau nháy qua lại).
- **Bỏ qua yêu cầu khi đang chuyển, không xếp hàng.** Đã loại: hàng chờ.
- **Không quay về scene cũ khi lỗi.** Scene cũ đã huỷ hết (`switch-order`); dựng lại dễ sinh lỗi chồng lỗi.

## Chưa rõ
- Không có điểm chặn.
