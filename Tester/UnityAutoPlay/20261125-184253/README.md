# IDEA scene Game cac yeu cau play cho CodeLead kiem
Tester: UnityAutoPlay · Đợt: 20261125-184253

## Mục đích
- @Manager giao ngày 2026-11-25, mức IDEA.
- Chơi thử scene Game cho các yêu cầu kiểu `play` đang chờ @CodeLead kiểm. Mỗi yêu cầu ghi đạt hay không đạt, kèm ảnh.
- Danh sách đạt gửi @CodeLead để `verify`.
- Bước 1 là chuẩn bị (tạo đợt, đọc gd_pack và sổ yêu cầu). Bước 2 chỉ Play khi @Manager nhắn BẮT ĐẦU, vì Hud, Run, Combat đang sửa và biên dịch.
- Bỏ qua theo lệnh của @Manager:
  - 4 yêu cầu tạm dừng của Run ở `run-pause/pause-rules`;
  - mọi yêu cầu của Home, PopupPause, Ads, PopupRevive, TeamSelect, GameFlow (các phần này tắt ở IDEA, để tới PROTOTYPE).

## Test gì
Scene: `Assets/_Composition/Loading.unity`, Play, game tự sang `Game.unity`. Từ sổ `gd_req.py` có 20 yêu cầu (đều là `live/...`); cách thấy khi Play chép từ sổ.

| # | Feature | Yêu cầu | Cách thấy khi Play (theo sổ) |
|---|---|---|---|
| R1 | Run | `feature-run/run-start/replay-checks#edge-luot-ket-thuc-luc-dang-co-hieu-ung` | Thua lúc kĩ năng thành đang chạy hoặc quái chết dở, chơi lại: hiệu ứng cũ không chạy sang lượt mới |
| R2 | Run | `feature-run/run-start/replay-checks#edge-tat-game-giua-tran` | Dừng Play giữa trận rồi Play lại: chỉ kỉ lục đã lưu trước đó còn, lượt dở không lưu |
| R3 | Run | `feature-run/run-start/start-values#edge-hieu-ung-ki-nang-con-do-tu-luot` | Bấm Băng giá rồi để thành bị phá, chơi lại: quái lượt mới không bị làm chậm |
| R4 | Run | `feature-run/run-start/start-values#edge-tien-thuong-dot-hay-tien-ha-quai-toi` | Thua đúng lúc đợt vừa dọn, chơi lại: tiền đầu lượt đúng 50 |
| B1 | Balance | `feature-balance/balance-difficulty-curve/break-forecast#skill-gap` | Chơi ít nhất 5 lượt đội mẫu, ghi đợt gãy (vụng khoảng đợt 10, khéo khoảng đợt 20-25) |
| L1 | PopupLose | `feature-popup-lose/lose-actions/actions-spec#edge-tat-game-khi-popup-dang-mo` | Thua với điểm vượt kỉ lục cũ, đang mở popup thì dừng Play, Play lại: kỉ lục mới vẫn còn |
| S1 | _Services/SessionScene | `feature-session-scene/session-steps/participant-order#edge-nguoi-choi-roi-game-luc-dang-tai` | Play từ Loading với một participant tải chậm, giữa lúc tải rời app khoảng 30 giây: tải tiếp, không Failed vì quá 10 giây |
| C1 | Castle | `feature-castle/castle-attack/auto-shot-values#edge-phat-ban-lam-quai-ve-0-mau` | Thành bắn hạ một Lính, ví tăng tiền như khi hero hạ |
| C2 | Castle | `feature-castle/castle-skills/skill-values#edge-quai-bi-cham-roi-bi-ha` | Ở PROTOTYPE có Băng giá: quái đang chậm bị hạ thì dấu chậm mất cùng quái |
| H1 | Hero | `feature-hero/hero-list/hero-roster-values#hero-feedback` | Hero đi thì nhún 0.1u chu kì 0.4s, ra đòn nhích 0.2u trong 0.15s, bị đánh nháy đỏ 0.1s, Đấu sĩ rìu có vệt chém 0.2s, Cung thủ có đạn bay 0.15s |
| H2 | Hero | `feature-hero/hero-targeting/targeting-values#edge-muc-tieu-bi-bang-gia-lam-cham` | Bấm Băng giá khi hero đang đánh: hero giữ mục tiêu và nhịp đòn, chỉ quái chậm lại |
| E1 | Enemy | `feature-enemy/enemy-stats/enemy-stat-values#edge-tam-dung` | Hạ một quái rồi tạm dừng ngay: hình đang thu nhỏ đứng yên tới khi tiếp tục |
| F1 | SceneFlow | `feature-scene-flow/scene-list/boot-loading#edge-nguoi-choi-roi-game-ve-man-hinh-chinh` | Lúc thanh Loading đang chạy thì bỏ focus cửa sổ Game rồi quay lại: thanh tải tiếp từ chỗ cũ |
| F2 | SceneFlow | `feature-scene-flow/scene-request/switch-rules#edge-nguoi-choi-roi-game-luc-dang-chuyen` | Như F1: lần chuyển đi tiếp từ chỗ dở, không tải lại từ đầu |
| W1 | Wave | `feature-wave/spawning/spawn-order#edge-hai-con-ra-cung-cho` | Đợt 1: hai con liền một bên vẫn ra, chồng lên nhau rồi tách khi đi |
| W2 | Wave | `feature-wave/wave-gap/gap-timer#gap-same-rules` | Trong 3 giây thở vẫn mua nâng cấp, dùng kĩ năng thành, hero gục vẫn hồi sinh, máu thành không hồi |
| P1 | _Services/Popup | `feature-popup/popup-layer/stack-rules#edge-nguoi-choi-roi-game-luc-co-popup` | Bật cheat sample-popup rồi Play, rời app rồi quay lại: popup mẫu và nền che y như cũ, nút Đóng vẫn đóng được |

- Không đưa vào: `feature-popup/popup-request/request-rules#edge-dang-mo-popup-tam-dung-ma-luot-ket`. Nó đang chờ kiểm bằng test EditMode, không phải `play`, nên @CodeLead kiểm bằng test.
- Mục 5 của @Manager (Castle, Hero, Enemy, SceneFlow, Wave, _Services/Popup) ra 10 yêu cầu: C1, C2, H1, H2, E1, F1, F2, W1, W2, P1.

### Chỗ chưa chắc làm đúng cách sổ ghi (đã báo @Manager trước khi Play)
- **C2, H2, R3 cần Băng giá.** Theo `CastleConfigTests.Skill_numbers_match_the_design_with_only_thunder_at_the_idea_stage`, ở IDEA thành chỉ có Sấm sét. Nếu nút Băng giá không có khi Play thì không làm được như sổ ghi; ghi `không kiểm được`, không tự gọi `SlowAll` bằng code.
- **E1 cần tạm dừng.** Pause tắt ở IDEA (`pauseEnabled` của Run, @Manager bỏ qua `pause-rules`). Không có nút tạm dừng thì ghi `không kiểm được`.
- **P1 cần đổi `PopupInstaller.asset` (`cheat = SamplePopup`).** Tester không được sửa asset, nên phải nhờ @CodeLead bật, hoặc @Manager đồng ý thay bằng popup thua thật (cùng lớp popup, cùng nền che).
- **S1 cần một participant tải chậm,** mà scene thật không có. Chỉ thử được với scene thật (tải nhanh), nên chỉ thấy phần "không Failed" chứ không thấy phần "không tính 10 giây". Ghi rõ giới hạn đó.
- **"Rời app" trong Editor** là cửa sổ Unity mất focus (`OnApplicationFocus(false)`, `RunAppSignalsView`). Tester không tự bỏ focus được. Cách dự kiến: `execute_code` gửi `OnApplicationFocus(false)`, rồi `OnApplicationPause(true)`, rồi chiều ngược lại, tới các MonoBehaviour đang chạy (chỉ thao tác lúc Play). Cách này không dừng đồng hồ thật như khi app ra nền.
- **B1 cần 5 lượt dài tới đợt 10-25,** mỗi lượt khoảng 7-10 phút. Ở IDEA bảng giai đoạn ghi "không bắt buộc" kiểm bằng chơi thử. Đợt trước Editor chạy khoảng 1 khung hình/giây khi cửa sổ không được focus, nên 5 lượt có thể rất lâu.

### @Manager đã quyết (2026-11-25, trước khi Play)
1. C2, H2, R3: không có nút Băng giá thì ghi "không áp dụng ở IDEA, không kiểm được"; không gọi `SlowAll` bằng code.
2. E1: tạm dừng tắt ở IDEA thì ghi như mục 1.
3. P1: thử bằng popup thua thật (phương án b), ghi rõ không dùng SamplePopup.
4. S1: chỉ kiểm phần không Failed, ghi rõ giới hạn.
5. Rời app: `execute_code` gửi `OnApplicationFocus` và `OnApplicationPause`; ghi rõ cách này không dừng đồng hồ thật.
6. B1 để cuối:
   - ghi tới đâu được tới đó: số lượt, đợt bị phá, thời gian thật mỗi lượt;
   - nếu 2 lượt đầu mỗi lượt quá 30 phút thật thì dừng, ghi "dở" và báo @Manager.

## Môi trường
- Commit: guard chặn lệnh git trong phiên tester; đợt trước ghi HEAD 2bd8f00 cộng thay đổi chưa commit.
- Unity: dùng `mcp__UnityMCP__`, instance `UnityProject@638c2683` theo @Manager (không dùng `Rebuilt@5e230bc6`). Play trong Editor (bản dev), Game view 1920x1080.
- Ảnh: `manage_camera screenshot`, `output_folder: "Captures"`, rồi `tester.py shot UnityAutoPlay 20261125-184253` với tên file ảnh.
- BẮT ĐẦU lúc khoảng 18:44.
- Các lần Play:
  - lần 1: Play có Pause ở khung 1 để thử F1, F2, S1;
  - lần 2: hỏng vì Unity nạp lại code giữa lúc Play, do @CodeLead sửa `.cs` trong `_Composition`; @Manager đã cho dừng;
  - lần 3 (18:53:35): trận sạch, chơi tới đợt 10;
  - lần 4 (18:59:14) và lần 5 (18:59:43): các phép thử dừng Play rồi Play lại.
- Khung hình:
  - cửa sổ Unity không được focus thì chạy khoảng 1 khung/giây (game tự kẹp `deltaTime` 0.333, trận chậm 3 lần);
  - người bấm vào Game view thì lên 60 khung/giây;
  - H1 đo lúc đủ 60 khung/giây;
  - từ khoảng 18:57 Editor tụt lại 1 khung/giây nhiều lần.
- Cách đo: `execute_code` gắn hàm vào `Application.onBeforeRender` (chỉ trong bộ nhớ) và cộng vào các event của `EnemyField`, `CastleAutoShot`, `CastleSkills`.
  - Đọc trạng thái qua `SessionSceneLogic.participants`: `RunFlow.Step`, `RunScore`, `WalletLogic.Coins`, `CastleBody.Health`, `WaveDirector`, `EnemyField._alive`.
  - Bấm nút như người chơi bằng `ExecuteEvents.pointerClickHandler`.
  - Ép thua bằng `CastleBody.ForceHealth(1)`, cùng đường với `ICastleCheat.SetHealth`, rồi để quái tự phá thành.
- "Rời app" trong Editor: gửi `OnApplicationFocus(false)` và `OnApplicationPause(true)` tới mọi GameObject, rồi chiều ngược lại. Đồng hồ thật không dừng.

## Các bước
Viết tắt: MĐ là mong đợi theo sổ hoặc thiết kế, TT là thực tế thấy.

1. **F1, F2, S1: rời app giữa lúc Loading** (ảnh 01, dữ liệu 02).
   - Play từ `Loading.unity` với Pause, đứng ở khung 1 (thanh 0.00, scene Game đang tải). Gửi "rời app", Step từng khung, giữ khoảng 13 giây, gửi "quay lại", rồi bỏ Pause.
   - MĐ:
     - F1: thanh tải tiếp từ chỗ cũ.
     - F2: lần chuyển đi tiếp, không tải lại.
     - S1: phiên không Failed vì quá 10 giây.
   - TT:
     - Thanh lên 0.00 rồi 1.00, không về 0.
     - Dòng Error trống. Phiên không Failed dù 27 giây thật nằm giữa lúc bắt đầu tải và lúc trận chạy (đồng hồ tải là `FrameLoadTimer`, tính theo khung).
     - Trận chạy đủ view.
     - Console chỉ có cảnh báo đúng thiết kế "[SceneFlow] Switch to 'Game' still going after 5 s… It keeps going."
   - Giới hạn:
     - scene thật tải xong trong 1 khung, không có participant tải chậm (S1 chỉ kiểm phần không Failed);
     - không dừng được đồng hồ thật.
   - Kết quả: **đạt** (F1, F2, S1, với giới hạn trên).
2. **C1: thành bắn hạ quái** (dữ liệu 03).
   - Lượt 1 đợt 3, ghi event `CastleAutoShot.Fired` và `EnemyField.Rewarded`/`Defeated`.
   - TT: khung 1883, phát bắn của thành vào Lính handle 25 (x 2.98u) đưa nó về 0 máu. Cùng khung có Rewarded 5 xu, ví 237 lên 242, rồi Defeated. Giống khi hero hạ quái.
   - Kết quả: **đạt**.
3. **H1: hiệu ứng hero** (ảnh 04, dữ liệu 05). Đo 60 khung/giây, 3 hero.
   - Nhún 0.100u, chu kì 0.400s.
   - Nhích 0.20u trong 0.15s (0.136-0.168s, lệch 1 khung).
   - Nháy đỏ 0.100-0.117s.
   - Vệt chém Đấu sĩ rìu 0.184-0.200s; đạn Cung thủ 0.133-0.149s.
   - Kết quả: **đạt**.
4. **W1: hai con ra cùng chỗ** (dữ liệu 11).
   - TT về thả quái: con liền nhau cùng bên vẫn ra (tối đa 3 con liền), cùng x=±17.
   - TT về chồng hình: quái cùng bên chồng nhau (cách dưới 0.3u) rồi tách sau 0.2-1.1 giây khi khác tốc độ (Lính vượt Khổng lồ, Sói vượt Lính). Không đẩy nhau.
   - Giới hạn: không thấy hai con ra đúng cùng một khung.
   - Kết quả: **đạt**.
5. **W2: trong 3 giây thở** (dữ liệu 11).
   - Ở lần thở 1 và 2, bấm nút nâng cấp Hiệp sĩ và nút Sấm sét như người chơi.
   - TT:
     - mua được (393 xuống 363, rồi 504 xuống 463, cấp 2 rồi cấp 3);
     - Sấm sét dùng được, hồi chiêu chạy (19.55 giây còn lại 1.1 giây sau khi bấm);
     - máu thành giữ nguyên suốt lúc thở (974.3, 863.7);
     - lúc thở dài 2.98-3.0 giây.
   - Chưa thấy hero gục trong lúc thở, nên phần "hồi sinh 8 giây vẫn chạy" chưa kiểm.
   - Kết quả: **đạt một phần** (mua, kĩ năng, hồi chiêu, máu không hồi); phần hồi sinh hero chưa thấy.
6. **L1: tắt game khi popup thua đang mở** (ảnh 06, dữ liệu 09).
   - Kỉ lục cũ 8. Chơi tới điểm 9, ép thành 1 máu để quái phá thành ở đợt 10.
   - TT:
     - `Run/Best` thành 9 ngay ở bước Ending, trước khi popup hiện;
     - popup "Trụ được 9 đợt", "Kỉ lục: 9 đợt", "Kỉ lục mới!";
     - dừng Play lúc popup mở, PlayerPrefs `Run/Best` = 9;
     - Play lại: `RunScore.Best` = 9.
   - Kết quả: **đạt**.
7. **P1: rời app lúc có popup** (bằng popup thua thật, không dùng SamplePopup; ảnh 07, dữ liệu 09 và 10).
   - TT:
     - trước, trong và sau "rời app": nền che alpha 0.60, chặn chạm, popup thua nằm trên nền che, cả hai nút interactable;
     - bước Run vẫn Lost, không mở thêm gì;
     - lượt sau rời và quay lại lần nữa rồi bấm "Chơi lại": popup đóng, vào lượt mới.
   - Kết quả: **đạt**.
8. **R2: tắt game giữa trận** (ảnh 08, dữ liệu 09).
   - Dừng Play ở đợt 2 (điểm 1, tiền 115).
   - TT: PlayerPrefs `Run/Best` = 9. Play lại: tiền 50, máu 1000/1000, đợt 1, điểm 0, kỉ lục 9.
   - Giới hạn: điểm lượt dở thấp hơn kỉ lục.
   - Kết quả: **đạt**.
9. **R4: tiền tới sau khi lượt cũ kết thúc** (dữ liệu 10).
   - Ép thành 1 máu từ quái cuối của đợt 1. Thành vỡ ở đợt 2 (tiền 140 lúc Ending).
   - TT:
     - từ Ending tới lúc bấm Chơi lại không có Rewarded nào, tiền giữ 140;
     - bấm Chơi lại thì tiền về 50 ngay khung đó, máu 1000, đợt 1.
   - Giới hạn: không dựng được đúng khoảnh khắc thua cùng lúc với dọn đợt (cần quái cuối vừa bị hạ vừa phá thành cùng khung). Phần Run đã có `RunFlowTests.WaveClearedAfterTheCastleFellDoesNotCount`.
   - Kết quả: **đạt** (với giới hạn trên).
10. **R1: lượt kết thúc lúc đang có hiệu ứng** (dữ liệu 10).
    - Ép thành 1 máu, bấm Sấm sét khi quái gần thành, chờ thành vỡ lúc hiệu ứng còn chạy.
    - Tới đợt 3 thành vẫn chưa vỡ (19:03) thì @Manager báo dừng đợt vì người sắp tắt máy.
    - Ghi thêm:
      - lúc thua ở L1 có 1 hình quái đang chết dở (11 hình, 10 quái sống), nhưng lần đó dừng Play chứ không bấm Chơi lại;
      - ở R4, sau Chơi lại, số hình quái bằng số quái sống, popup và quái cũ đều đã dọn, nhưng lúc thua lần đó không có hiệu ứng đang chạy.
    - Kết quả: **dở**.
11. **C2, H2, R3: cần Băng giá.** Ở IDEA thanh kĩ năng chỉ có Sấm sét (ảnh 01, 04, 08). Kết quả: **không áp dụng ở IDEA, không kiểm được**.
12. **E1: cần tạm dừng.** Ở IDEA không có nút tạm dừng (`RunFlow.PauseEnabled=False`). Kết quả: **không áp dụng ở IDEA, không kiểm được**.
13. **B1: Balance 5 lượt.**
    - Đã chơi một lượt tới đợt 10 (thua vì bị ép, không phải gãy thật), lượt dài khoảng 4.7 phút game.
    - Editor mất focus nhiều lần (1 khung/giây, trận chậm 3 lần), nên 5 lượt tới đợt 10-25 sẽ quá 30 phút thật mỗi lượt.
    - Kết quả: **dở, cần focus** (theo quyết định 6 của @Manager).

## Kết quả
dở: 10 yêu cầu đạt, W2 đạt một phần, 4 yêu cầu không áp dụng ở IDEA, R1 và Balance B1 dở; đợt dừng lúc 19:03 vì @Manager báo người sắp tắt máy.
- Đạt (gửi @CodeLead verify):
  - F1 `feature-scene-flow/scene-list/boot-loading#edge-nguoi-choi-roi-game-ve-man-hinh-chinh`
  - F2 `feature-scene-flow/scene-request/switch-rules#edge-nguoi-choi-roi-game-luc-dang-chuyen`
  - S1 `feature-session-scene/session-steps/participant-order#edge-nguoi-choi-roi-game-luc-dang-tai` (chỉ phần không Failed)
  - C1 `feature-castle/castle-attack/auto-shot-values#edge-phat-ban-lam-quai-ve-0-mau`
  - H1 `feature-hero/hero-list/hero-roster-values#hero-feedback`
  - W1 `feature-wave/spawning/spawn-order#edge-hai-con-ra-cung-cho`
  - L1 `feature-popup-lose/lose-actions/actions-spec#edge-tat-game-khi-popup-dang-mo`
  - P1 `feature-popup/popup-layer/stack-rules#edge-nguoi-choi-roi-game-luc-co-popup` (bằng popup thua thật)
  - R2 `feature-run/run-start/replay-checks#edge-tat-game-giua-tran`
  - R4 `feature-run/run-start/start-values#edge-tien-thuong-dot-hay-tien-ha-quai-toi`
- Đạt một phần: W2 `feature-wave/wave-gap/gap-timer#gap-same-rules` (chưa thấy hero hồi sinh trong lúc thở).
- Không áp dụng ở IDEA, không kiểm được: C2, H2, R3, E1.
- Dở: R1 `feature-run/run-start/replay-checks#edge-luot-ket-thuc-luc-dang-co-hieu-ung`, B1 `feature-balance/balance-difficulty-curve/break-forecast#skill-gap`.

### Làm tiếp từ đâu
1. Cần cửa sổ Unity được focus (60 khung/giây) và không phiên nào ghi vào UnityProject trong lúc Play.
2. R1:
   - Play, ép thành 1 máu.
   - Thua lúc có quái đang chết dở hoặc Sấm sét đang chạy: so số con của EnemyStage với số quái sống ở bước Ending.
   - Bấm Chơi lại. Ở khung +2 kiểm số hình quái bằng số quái sống, không còn chữ tiền hay hiệu ứng cũ.
3. W2, phần hồi sinh: chờ một lần thở có hero đang gục, xem hero hồi sinh sau 8 giây trong lúc thở.
4. B1: 5 lượt đội mặc định, không ép thua.
   - Ghi đợt gãy và thời gian thật mỗi lượt.
   - Dừng nếu 2 lượt đầu mỗi lượt quá 30 phút thật.
5. Kỉ lục trong PlayerPrefs `Run/Best` hiện là 9, do L1 phá kỉ lục cũ 8.

## Lỗi tìm thấy
1. **Nút "Về màn bắt đầu" có trên popup thua ở IDEA** (bước 6, ảnh 06 và 07; feature PopupLose).
   - Thiết kế `feature-popup-lose/lose-actions/actions-spec`, bảng giai đoạn: "Nút Về màn bắt đầu | IDEA: Không có (chưa có màn bắt đầu)". Node cheat cũng ghi "Nút Về màn bắt đầu không có ở IDEA".
   - Thực tế popup có nút `Home` ("Về màn bắt đầu") interactable. Không bấm thử, nên chưa biết bấm thì ra sao.
   - Đợt 20261125-115759 không ghi nút này. Có thể do thay đổi `Boot` hoặc `_Composition` hôm nay (`Boot` giờ có `fromPrototype`).
2. **Nhãn và nút "Cheat: mid-battle" hiện trên HUD khi chơi thường** (ảnh 01, 04, 06, 08; feature Hud).
   - `HudView/Safe/Cheat` là một Button tím ở mép trên, hiện ở mọi lượt.
   - Console lúc tải có dòng "Hud cheat 'mid-battle': not applied: Frost: không có ở mức này, Rally: không có ở mức này".
   - Có vẻ `HudInstaller` đang để state cheat `mid-battle`. Cheat không được áp dụng nhưng vẫn hiện nhãn trên màn chơi.
   - Hud đang được sửa trong ngày, có thể là trạng thái tạm.
3. Không phải lỗi game, ghi để biết:
   - Unity nạp lại code giữa lúc Play (khoảng 18:50-18:53) làm `SessionSceneDriver.session` thành null và trận đứng yên. Nguyên nhân là @CodeLead ghi `.cs` lúc đợt đang chạy, @Manager đã xử lí.
   - Lúc Stop Play, console có 1 `NullReferenceException` từ chính hàm đo của tester (`MCPDynamicCode`, `BeforeRenderHelper`), không phải code game.

## Cấu trúc thư mục
- README.md: file này
- images/01-loading-left-then-back-game-runs.png: trận chạy đủ view sau khi "rời app" giữa lúc Loading rồi quay lại (F1, F2, S1)
- images/02-f1-f2-s1-loading-leave-log.txt: nhật kí từng khung của thanh tải, lúc gửi rời app và quay lại (F1, F2, S1)
- images/03-c1-castle-kill-events.txt: nhật kí event lúc thành bắn hạ Lính và ví tăng 5 xu (C1)
- images/04-h1-heroes-fighting.png: màn trận lúc thở đợt 4, thanh kĩ năng chỉ có Sấm sét, nhãn Cheat mid-battle trên HUD
- images/05-h1-hero-feedback-measure.txt: số đo nhún, nhích, nháy, vệt chém, đạn của 3 hero (H1)
- images/06-l1-lose-popup-new-record-9.png: popup thua phá kỉ lục 9, có nút Về màn bắt đầu (L1, lỗi 1)
- images/07-p1-popup-after-leave-and-back.png: popup thua sau khi rời app rồi quay lại, y như trước (P1)
- images/08-r2-before-stop-mid-battle.png: trận đợt 2, tiền 115, ngay trước khi dừng Play giữa trận (R2)
- images/09-l1-p1-r2-log.txt: nhật kí L1, P1, R2: bước Run, PlayerPrefs Run/Best trước và sau khi dừng Play
- images/10-r1-r4-p1-replay-log.txt: nhật kí R4, R1 và lần bấm Chơi lại sau khi rời app (P1)
- images/11-w1-w2-wave-log.txt: nhật kí quái ra cùng bên, chồng rồi tách (W1) và 3 giây thở (W2)
