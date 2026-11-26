# Nhịp và ngẫu nhiên của trận

Dữ liệu cốt lõi: [data.md](data.md)

## Người chơi được gì
- Không thấy trực tiếp. Trận chạy như nhau trên máy nhanh và máy chậm.
- Gián tiếp: tester chạy nhanh nhiều lượt để bắt lỗi luật và chỉnh cân bằng, nên game đến tay người chơi ít lỗi hơn, độ khó đều hơn.

## Liên quan
- `feature-combat`: một nhịp giao chiến ứng với một bước của nhịp chung; bước đó lấy từ feature này.
- `feature-wave`: thả quái dùng số ngẫu nhiên (xáo thứ tự, bốc bên).
- `feature-hero`, `feature-enemy`, `feature-castle`, `feature-run`: logic theo thời gian (đi, đánh, hồi chiêu, hồi sinh, đồng hồ lúc thở, chờ trước popup thua) chạy theo nhịp chung.
- `feature-run`: dừng trận. Nhịp vẫn chạy lúc trận dừng; từng feature tự đứng yên theo trạng thái trận của lượt, như đã chốt.
- `feature-balance`: dùng chạy nhanh của tester để kiểm đường độ khó.
- `feature-session-scene`: nhịp được cài ngay sau vòng đời scene, trước mọi feature gameplay (việc của @CodeLead).

## Đã tự quyết
- **Feature hạ tầng mới, không gộp vào `feature-session-scene` hay `feature-combat`** (người cho chọn chỗ). Vòng đời scene lo tải, chạy, huỷ chứ không lo thời gian trong trận; giao chiến chỉ là một phần dùng nhịp. Nhịp và ngẫu nhiên cắt ngang mọi feature gameplay, nên tách riêng theo luật vòng 2. Đã loại: thêm ý vào `feature-combat` (hero đi, hồi chiêu, đồng hồ lúc thở không qua giao chiến).
- **Là feature, không đặt ở Core** (@CodeLead, 2026-11-25). Nhịp và ngẫu nhiên là dịch vụ để feature khác dùng, không phải thứ mọi feature phải kế thừa; thành feature thì có vòng 4 giữ con số, sổ yêu cầu và cheat bản dev.
- **Gộp nhịp và ngẫu nhiên trong một feature.** Cả hai cùng phục vụ một mục đích là trận lặp lại được; tách hai thì tester phải ghép hai nơi. Đã loại: hai feature riêng.
- **Key `feature-sim-clock`, `code.json`: `SimClock`**, theo luật đặt tên vòng 2, @CodeLead đã đồng ý.
- **Dòng ngẫu nhiên riêng cho từng feature** (@CodeLead đề nghị). Giữ kết quả của feature này ổn định khi feature khác đổi số lần bốc.
- **Dừng trận không thuộc nhịp chung** (@CodeLead). Giữ nguyên luật dừng trận đã chốt ở `feature-run`; nhịp không dùng cách làm chậm thời gian của engine.
- Con số (bước tick, trần số bước, seed, tốc độ chạy nhanh) để vòng 4, theo luật.

## Đã tự quyết (người quyết, 2026-11-25, qua @Manager)
- **Cách chạy mặc định là bước cố định 40 lần mỗi giây**, cho cả chơi thường lẫn test; theo khung hình vẫn chọn được trong config. Ghi ở `feature-sim-clock/clock-step/clock-values`.
- **Seed bản phát hành ngẫu nhiên mỗi lượt và được ghi lại; bản dev dùng seed cố định, in ra console.** Ghi ở `feature-sim-clock/seeded-random/random-values`.
- **Bỏ tạm dừng, quảng cáo và popup hồi sinh (2026-11-26, người quyết).** Bỏ `feature-popup-revive` và tạm dừng khỏi các bên dùng nhịp (chỉ còn trận dừng sau khi thua); bỏ hai ý `continue-after-revive`, `revive-result-queued`.

## Góp ý của GPT
Lần hỏi `r2-1` (2026-11-25, `advice/feature-sim-clock/r2-1.md`, also: `feature-combat`, `feature-wave`, `feature-run`). 12 góp ý, đều thành ý của node (không bỏ góp ý nào); 2026-11-26 bỏ hai ý về hồi sinh thành:
- Ba góp ý làm rõ viết lại chính ý cũ:
  - `clock-drives-gameplay` theo `dong-ho-khi-tran-chay` (AI 7). Tách đồng hồ gameplay khỏi số bước chung: lúc trận dừng bước vẫn chạy để áp hàng chờ, chỉ đồng hồ gameplay đứng. Không trái quyết định "dừng trận không thuộc nhịp" của @CodeLead, chỉ nói rõ thêm. Node con `clock-step` và `clock-values` (`steps-counted`) sửa theo.
  - `tester-fast-run` theo `dieu-kien-chay-lai` (AI 7): đủ điều kiện chạy lại, kể cả độ dài từng bước ở cách theo khung hình.
  - `seeded-rng` theo `seed-dev-dung-config` (AI 7): bản dev dùng đúng seed config mỗi lượt; `random-values#dev-seed-per-run` sửa theo, bỏ cách cộng số lượt.
- Chín góp ý thêm (còn bảy sau khi bỏ hồi sinh thành):
  - Ở mức 7: `step-after-combat`, `spawn-after-results`, `keep-drawn-spawn`, `init-on-run`, `first-step-on-start`, `seed-record-owner`, `clear-on-dispose`. Phần lớn khớp thứ đã viết ở vòng 3, 4 hoặc ở `feature-wave` (danh sách thả lập sẵn đầu đợt nên con chờ trần giữ nguyên loại và bên).
- **Mốc đầu lượt** (`init-on-run`, `first-step-on-start`, `seed-record-owner`): `feature-run` quyết khi nào, nhịp làm việc khởi tạo; không có hai nơi cùng giữ việc khởi tạo lượt.

## Chưa rõ
- Không có điểm chặn. Thứ tự feature trong một bước ở `feature-sim-clock/clock-step`; đặt lại dòng mỗi lượt ở `feature-sim-clock/seeded-random`.
