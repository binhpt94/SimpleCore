# Các đợt test đã lưu trữ của UnityAutoPlay
Đợt cũ được dời vào đây bằng `python Management/tools/tester.py archive`. Chỉ nhóm Testers (và người) đọc thư mục này; các phiên khác không đọc để không loãng ngữ cảnh.

## Cấu trúc thư mục
- README.md: file này (lệnh tự ghi)
- <yyyymmdd-hhmmss>/: một đợt test, nguyên vẹn như lúc dời

## Danh mục
- 20261127-185421: Chụp art Castle Simple · <đạt | không đạt | dở>: <một câu>
- 20261127-162312: CheatPanel IDEA · <đạt | không đạt | dở>: <một câu>
- 20261127-152335: Run kỉ lục qua feature-save (IDEA) · dở: không chơi, vì việc đã được chơi và ghi ở đợt `20261127-151417` (đạt).
- 20261127-152230: Run: kỉ lục qua feature-save, popup thua và chơi lại · dở: không chơi, vì việc đã được chơi và ghi ở đợt `20261127-151417` (đạt).
- 20261127-152146: Run: kỉ lục ở feature-save, popup thua, chơi lại (IDEA) · dở: không chơi, vì việc đã được chơi và ghi ở đợt `20261127-151417` (đạt).
- 20261127-152024: Run: ki luc qua feature-save, popup thua, choi lai · dở: không chơi vì là đợt trùng; kết quả của việc nằm ở đợt `20261127-151417` (đạt).
- 20261125-205040: IDEA lam tiep R1 W2 popup thua B1 · đạt: R1, W2 (phần hồi sinh) và popup thua ở IDEA không còn nút "Về màn bắt đầu" đều đúng thiết kế; B1 để sau theo lệnh của @Manager (chờ tick cố định).
- 20261125-184253: IDEA scene Game cac yeu cau play cho CodeLead kiem · dở: 10 yêu cầu đạt, W2 đạt một phần, 4 yêu cầu không áp dụng ở IDEA, R1 và Balance B1 dở; đợt dừng lúc 19:03 vì @Manager báo người sắp tắt máy.
- 20261125-131408: IDEA quai bi ha bien mat sau Choi lai · đạt: sau mỗi lần Chơi lại, quái bị hạ đều thu nhỏ rồi bị huỷ thật, không còn hình kẹt hay object ẩn ở lượt 2 và 3; nhún khi đi đúng 0.1u; console không lỗi. Hình thu nhỏ chỉ đo được ở các đoạn Editor chạy đủ khung hình.
- 20261125-115759: IDEA mot luot day du 12 feature va choi lai 3 lan · đạt: cả 9 bước chạy đúng thiết kế IDEA, chơi 4 lượt liền không dồn object và console không lỗi; có 1 lỗi bố cục nhẹ ở 5:3 và vài mục chỉ kiểm được một phần vì giới hạn của unity-mcp.
- 20261125-015850: cheat new-record · <đạt | không đạt | dở>: <một câu>
- 20261125-014348: chot IDEA · đạt: cả 9 bước của Manager và 11 node vòng 4 đều đúng thiết kế ở mức IDEA. Cheat Hud `mid-battle` và HudUpgrade `mixed-buttons` (phần hiển thị) đạt. Ba cheat PopupLose `new-record`, Popup `sample-popup`, SceneFlow `load-error` chưa kiểm vì chỉ bật được qua config asset.
