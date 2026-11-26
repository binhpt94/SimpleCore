# Luật tạm dừng

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Giữ trạng thái trận: đang đánh, tạm dừng, đã kết thúc.
- Vào tạm dừng: dừng thời gian trận, báo `feature-wave`, `feature-enemy`, `feature-hero`, `feature-castle` đứng yên; nhờ `feature-popup` hiện popup tạm dừng (`feature-popup-pause`).
- Nhận lựa chọn từ popup: tiếp tục (chạy lại ngay) hoặc bỏ lượt (kết thúc lượt qua `feature-run/run-flow/flow-rules`).
- Theo dõi game rời khỏi màn hình (mất tiêu điểm hoặc bị ẩn) để tự vào tạm dừng.

## Thông số
| Thông số | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Hồi chiêu đứng yên | Sấm sét 20, Băng giá 35, Cổ vũ 25 | giây | Bộ số chung; đều đứng khi dừng |
| Nghỉ giữa hai đợt đứng yên | 3 | giây | Bộ số chung |
| Đếm ngược khi tiếp tục | 0 | giây | Chạy lại ngay |
| Số popup tạm dừng mở cùng lúc | tối đa 1 | popup | Không mở chồng |
| Điểm khi bỏ lượt | số đợt đã dọn sạch | đợt | Giống thua |

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Nút tạm dừng và popup tạm dừng | Không có | Không bắt buộc | Có |
| Tự tạm dừng khi rời game | Không có (trận cứ chạy) | Không bắt buộc | Có |
| Nút quay lại của máy mở tạm dừng | Không có | Không bắt buộc | Có |
| Bỏ lượt | Không có | Không bắt buộc | Có, có hỏi xác nhận |

## Trường hợp biên
- **Đang tạm dừng mà game lại rời khỏi màn hình:** không mở thêm popup; vẫn một popup tạm dừng.
- **Rời game khi lượt đã kết thúc (đang chờ hoặc đang có popup thua):** không tạm dừng; popup thua vẫn như cũ.
- **Rời game khi đang ở màn bắt đầu hoặc chọn đội:** không có gì xảy ra.
- **Bấm tạm dừng hai lần liền:** lần hai rơi vào nền che của popup, không có tác dụng.
- **Tạm dừng và kĩ năng bấm trong cùng khung hình:** cả hai vào hàng chờ thao tác của `feature-sim-clock` và áp ở bước nhịp kế tiếp theo thứ tự bấm. Kĩ năng bấm trước thì dùng được và giữ hiệu quả, hiệu ứng (ví dụ làm chậm của Băng giá) đứng yên cùng trận và chạy tiếp khi tiếp tục. Tạm dừng bấm trước thì kĩ năng bị từ chối theo `pause-block-input`. (Câu chữ sửa 2026-11-25 theo hàng chờ thao tác; luật không đổi.)
- **Bỏ lượt khi chưa dọn đợt nào:** điểm 0, kỉ lục không đổi.
- **Rời game lúc popup hồi sinh đang mở hoặc quảng cáo đang chạy:** không tạm dừng, không mở popup tạm dừng; trận đã đứng yên từ lúc thành bị phá (`pause-not-during-revive` ở node cha).
- **Hồi sinh xong:** trận về trạng thái đang đánh; nút tạm dừng có tác dụng lại.

## Đã tự quyết
- **Không đếm ngược khi tiếp tục.** Trận đứng đúng chỗ, người chơi đã thấy tình hình trên nền che. Đã loại: đếm 3-2-1.
- **Bỏ trường hợp biên "tạm dừng giữa lúc nghỉ 3 giây" (2026-11-26, người quyết qua @Manager: việc chéo feature tách về feature làm việc đó).** Đồng hồ lúc thở là của đợt quái; luật đã có ở `feature-wave/wave-gap/gap-timer` ("Tạm dừng trong lúc thở: đồng hồ đứng yên; tiếp tục thì đếm phần còn lại"). Lượt chơi chỉ giữ trạng thái tạm dừng.
- **Nút quay lại của máy mở tạm dừng.** Khớp `feature-run/run-flow/flow-rules`.
- **Thao tác lọt qua popup vẫn bị chặn ở feature chủ.** Không chỉ dựa vào nền che; tránh mua được trong khung hình popup đang mở.

## Chưa rõ
- Không có điểm chặn.
