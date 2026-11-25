---
name: manager-dispatch
description: Chỉ phiên FEATURE=@Manager. Điều phối một đợt việc - lập bảng theo feature, lấy một việc kế tiếp mỗi feature theo sổ yêu cầu, bỏ việc phải nối tiếp, nhắn cùng lúc; giao đợt chơi thử cho @UnityAutoPlay; điều phối lại khi một Leader nhắn BÁO CHẶN hay HẾT CHẶN. Dùng sau kiểm tra đầu phiên, khi có báo cáo mới, khi cần chơi thử, hoặc khi nhận tin BÁO CHẶN.
---
# @Manager: giao việc theo đợt
Quyền, chuỗi của một feature và danh sách "phải nối tiếp" vẫn ở `.claude/rules/manager.md`. Kiểm tra đầu phiên: skill `manager-startup-check`.

## Giao việc theo đợt
1. Lập bảng theo feature: `live` tới vòng mấy, ĐƯỢC CODE hay ĐANG KHOÁ, đang ở bước nào của chuỗi, phiên nào đang làm, đang chờ ai.
2. Với mỗi feature, lấy đúng một việc kế tiếp theo chuỗi. Ở bước Code, giao theo sổ yêu cầu (`gd_req.py <Tên>`): nêu id yêu cầu cần làm, không giao "làm tiếp feature".
3. Bỏ các việc vướng danh sách "phải nối tiếp". **Nhắn cùng lúc** mọi phiên còn lại: nhiều `SendMessage` trong một lượt. Không chờ phiên này trả lời rồi mới nhắn phiên kia.
4. Báo người bảng đó và các việc đang chờ người (chấm, chọn xung đột, chốt): đây thường là thứ đang chặn nhiều feature nhất.
5. Có báo cáo mới thì lập lại bảng và giao đợt tiếp.

## Chơi thử (nhóm Testers)
- **Bạn (cùng người) là bên duy nhất giao đợt chơi thử và xếp lịch cho tester.** Nó giữ khoá Unity Editor suốt lúc chơi, nên mỗi lần gọi là chặn Play, test và scene của mọi phiên. Guard chặn `SendMessage` tới `UnityAutoPlay` của phiên feature, @Core, Services và nhóm Tools; các phiên đó nhắn bạn khi cần chơi thử.
- @CodeLead và @GameDesigner được gọi nó trực tiếp chỉ để giả lập một thao tác người chơi xác nhận một tính năng (tin `XÁC NHẬN · …`); việc rộng hơn họ phải nhờ bạn.
- Gom các thứ cần thử thành một đợt thay vì gọi nhiều lần. Không giao khi @CodeLead đang Play hay chạy test; hỏi `CodeLead` trước nếu chưa rõ.
- Giao `UnityAutoPlay` chơi thử khi một feature ĐƯỢC CODE đã được @CodeLead kiểm và nối dây ở mức đang làm (IDEA, PROTOTYPE, FULL): nhắn feature nào, mức nào, muốn biết điều gì. Nó không sửa gì; kết quả nằm ở `Tester/UnityAutoPlay/<đợt>/README.md` và trong báo cáo cuối lượt của nó.
- Đợt `không đạt`: chuyển danh sách lỗi cho @CodeLead (nó nhắn đúng phiên sở hữu). Sửa xong thì giao chơi lại đúng phần đó.
- Lưu trữ: đợt đã có đợt mới thay thế hoặc lỗi đã sửa và chơi lại đạt thì dời đi, `python Management/tools/tester.py archive UnityAutoPlay <đợt>` (hoặc `--keep <N>`). Sau khi dời, phiên ngoài nhóm Testers (kể cả bạn) không đọc được đợt đó nữa.

## Khi một Leader báo chặn
@CodeLead và @GameDesigner nhắn tin `BÁO CHẶN · <lỗi | chờ người> · …` khi gặp lỗi không tự gỡ được hoặc phải dừng chờ người, và `HẾT CHẶN · <việc>` khi đi tiếp được. Nhận tin thì điều phối lại ngay, không đợi báo cáo cuối lượt:
1. Ghi vào bảng theo feature: feature nào, đang đứng ở bước nào của chuỗi, chờ ai.
2. **Dừng giao các việc phụ thuộc** vào thứ đang bị chặn. Ví dụ @CodeLead kẹt ở khung của A: không giục phiên A và các feature dùng contract của A. @GameDesigner chờ người chốt node cha: không nhờ hỏi GPT hay đặt order cho node con của nó.
3. **Giao ngay đợt mới cho các việc không phụ thuộc**: feature khác đã mở khoá, order ảnh, thiết kế của feature còn khoá. Một Leader đứng không phải là cả project đứng.
4. **Chờ người**: gom mọi việc đang chờ người thành một danh sách ngắn trong câu trả lời cho người: việc gì, làm ở đâu (tab, node), đang chặn những feature nào; việc chặn nhiều feature nhất xếp trước. Không để từng Leader hỏi người lẻ tẻ.
5. **Lỗi mà phiên khác gỡ được**: nhắn đúng phiên đó, kèm file và câu lỗi. Lỗi chỉ người gỡ được (guard, quyền, công cụ, hạn mức): báo người, nêu rõ lệnh và câu lỗi.
6. Nhận `HẾT CHẶN`: lập lại bảng và giao đợt tiếp theo "Giao việc theo đợt".
Leader im lặng lâu mà bảng cho thấy nó đang giữ việc: hỏi thẳng nó trước khi coi là bị chặn.
