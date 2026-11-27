# Các đợt test đã lưu trữ của UnityAutoPlay
Đợt cũ được dời vào đây bằng `python Management/tools/tester.py archive`. Chỉ nhóm Testers (và người) đọc thư mục này; các phiên khác không đọc để không loãng ngữ cảnh.

## Cấu trúc thư mục
- README.md: file này (lệnh tự ghi)
- <yyyymmdd-hhmmss>/: một đợt test, nguyên vẹn như lúc dời

## Danh mục
- 20261127-162312: CheatPanel IDEA · <đạt | không đạt | dở>: <một câu>
- 20261127-152335: Run kỉ lục qua feature-save (IDEA) · dở: không chơi, vì việc đã được chơi và ghi ở đợt `20261127-151417` (đạt).
- 20261127-152230: Run: kỉ lục qua feature-save, popup thua và chơi lại · dở: không chơi, vì việc đã được chơi và ghi ở đợt `20261127-151417` (đạt).
- 20261127-152146: Run: kỉ lục ở feature-save, popup thua, chơi lại (IDEA) · dở: không chơi, vì việc đã được chơi và ghi ở đợt `20261127-151417` (đạt).
- 20261127-152024: Run: ki luc qua feature-save, popup thua, choi lai · dở: không chơi vì là đợt trùng; kết quả của việc nằm ở đợt `20261127-151417` (đạt).
- 20261125-015850: cheat new-record · <đạt | không đạt | dở>: <một câu>
- 20261125-014348: chot IDEA · đạt: cả 9 bước của Manager và 11 node vòng 4 đều đúng thiết kế ở mức IDEA. Cheat Hud `mid-battle` và HudUpgrade `mixed-buttons` (phần hiển thị) đạt. Ba cheat PopupLose `new-record`, Popup `sample-popup`, SceneFlow `load-error` chưa kiểm vì chỉ bật được qua config asset.
