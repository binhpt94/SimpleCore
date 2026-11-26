# Thủ thành anh hùng (tên tạm)

Dữ liệu cốt lõi: [data.md](data.md)

## Người chơi
- Người chơi phổ thông, chơi lượt ngắn trên điện thoại cầm ngang.
- Không cần nhanh tay. Trước trận chọn đội hero; trong trận canh lúc bấm kĩ năng thành và chọn nâng cấp nào mua lúc nào.
- Thích nhìn đội hero của mình mạnh dần và thử đội khác ở lượt sau.
- Ít nút nhưng phải đọc được trận: nhìn là biết lúc nào thành đang nguy và hero nào sắp gục, khi còn kịp chọn bấm kĩ năng hay nâng ai; mua xong thấy ngay món vừa mua giúp gì. Vị trí quái và hero trên dải đất đủ rõ để hiểu nguy ở phía nào.

## Cảm giác chính
- Bị kẹp từ hai phía, áp lực tăng dần qua mỗi đợt.
- Tự tay cứu nguy: bấm kĩ năng thành đúng lúc quái dồn đông.
- Thấy rõ mình mạnh lên ngay trong trận: vừa mua nâng cấp là hero, thành đánh mạnh hơn liền.
- Hồi hộp khi thành sắp bị phá, nhẹ nhõm khi dọn sạch một đợt.

## Vòng lặp chơi
1. **Làm gì:** chọn đội hero, vào trận; đội tự đánh quái từ hai bên, người chơi bấm kĩ năng thành và mua nâng cấp.
2. **Được gì:** hạ quái được tiền; tiền đổi ra nâng cấp; dọn sạch đợt thì điểm tăng.
3. **Lặp lại thế nào:** đợt sau khó hơn, phải mua thêm nâng cấp để trụ, tới khi thành bị phá. Thua thì xem điểm và kỉ lục, rồi giữ đội cũ để thử mua khác, bấm kĩ năng khác, hoặc đổi đội; chơi lại từ đầu.

Kể trong một phút: một tòa thành đứng giữa màn hình, quái kéo tới từ cả hai bên. Bạn chọn trước một đội hero; vào trận, hero tự lao ra đánh, thành tự bắn. Việc của bạn: canh lúc bấm kĩ năng của thành và tiêu tiền hạ quái vào nâng cấp, ngay giữa trận. Ví dụ: quái áp sát thành từ hai phía, một hero sắp gục. Bấm kĩ năng thành ngay, hay giữ lại vì bấm xong phải chờ hồi? Tiền vừa kiếm được nên nâng hero đó hay nâng thành? Đợt sau đông hơn, mạnh hơn. Thành bị phá là hết lượt; trụ được càng nhiều đợt càng cao điểm.

Khác game thủ thành thường gặp (xây tháp dọc đường, hoặc điều khiển hero): không xây, không điều khiển; bị kẹp hai phía; quyết định nằm ở chọn đội, canh kĩ năng và chi tiền.

## Đã tự quyết
- **Đối tượng: người chơi phổ thông.** Hero tự đánh, thao tác chỉ là bấm kĩ năng và mua nâng cấp. Đã loại: game nặng tính toán.
- **Nhìn ngang từ bên cạnh, thành ở giữa.** "Màn hình ngang" trong ý tưởng gốc hiểu là góc nhìn ngang trên điện thoại cầm ngang. Đã loại: nhìn từ trên xuống.
- **Quái vào từ hai bên trái và phải.** Thành ở giữa thì hai bên là hướng tự nhiên. Đã loại: bốn phía, một phía.
- **Đội hero cố định, chọn trước lượt chơi (theo phản hồi của người).** Hiểu "mỗi màn" là mỗi lượt chơi, vì game có một bản đồ và chơi vô tận. Nếu người muốn nhiều màn tách biệt thì cần chấm lại `single-map` và `endless-score`. Đã loại (người loại): thành tự sinh hero theo thời gian.
- **Hero gục thì hồi sinh ở thành.** Đội cố định mà chết hẳn thì chơi vô tận sẽ sụp nhanh và mất cảm giác "đội của mình". Đã loại: chết hẳn tới hết lượt.
- **Nhiều hero hơn số chỗ trong đội, chọn được hết ngay.** Thừa hero so với chỗ là đã có lựa chọn đáng nghĩ; không có tiến trình lâu dài nên không mở khoá. Mục tiêu (theo góp ý `hero-co-diem-yeu` của GPT): không có đội nào xử lí tốt mọi kiểu áp lực; đổi đội là đổi tình huống mình mạnh và tình huống phải bù bằng kĩ năng thành hoặc nâng cấp. Cân bằng từng hero ở `feature-balance`.
- **Thành tự đánh và có kĩ năng người chơi bấm (theo phản hồi của người).** Đã loại (người loại): thành không đánh.
- **Nâng cấp ngay trong trận, trận không dừng (theo phản hồi của người).** Đã loại (người loại): chỉ nâng cấp giữa hai đợt. Giữ `id` `upgrade-between-waves` theo quy trình dù tên không còn khớp.
- **Mỗi lượt bắt đầu lại từ đầu, chỉ lưu kỉ lục.** Ý tưởng gốc chưa nhắc tới tiến trình lâu dài. Đã loại: nâng cấp vĩnh viễn giữa các lượt.
- **Không có thắng, chơi vô tận.** Điều kiện kết thúc duy nhất trong ý tưởng là thành bị phá. Đã loại: thắng khi qua đủ một số đợt. Thước đo là số đợt đã vượt: mục tiêu "qua thêm một đợt" dễ hiểu, có mốc rõ để thử lại.
- **Một bản đồ duy nhất.** Giữ game gọn; mặt trận quen giúp người chơi đọc nhanh nguy ở phía nào. Nếu nhiều lượt cùng một cách mua và bấm thì xem lại nhịp dồn quái trước, không thêm bản đồ. Đã loại: nhiều màn.
- **Làm lại theo luật vòng 1 mới (2026-11-23):**
  - Thêm góc nhìn và điểm khác với game cùng thể loại (tóm tắt, mục "Kể trong một phút").
  - Bỏ mọi con số (thời lượng lượt, số hero trong đội, số hero trong danh sách); để vòng 4.
  - Bỏ chi tiết mức định nghĩa và danh sách: "0 máu" thành "thành bị phá"; "hero đánh quái gần nhất", "thành bắn quái trong tầm", danh sách nâng cấp của thành (máu, sức đánh, kĩ năng) chuyển xuống các vòng sau.
  - Không dùng chữ theo bản phát hành ("bản đầu"), theo yêu cầu của người.

- **Game 2D nhìn ngang (quyết khi vào vòng 4, 2026-11-23).** Vòng 4 cần biết 2D hay 3D để ghi kích thước unit. 2D hợp góc nhìn ngang một dải đất, sprite gen bằng AI làm nhanh ở mức IDEA. Đã loại: 3D low poly (thêm việc dựng model mà góc nhìn ngang không cần chiều sâu).

- **Màn ngang (`screen: landscape`, 1920x1080).** Game nhìn ngang, thành ở giữa, quái tới từ mép trái và mép phải: dải chiến trường cần chiều ngang; người chơi cầm điện thoại ngang. Đã loại: màn dọc (dải đất hai bên thành quá hẹp, quái ra gần thành).
- **Hai style hình ảnh (2026-11-23).** `ui-flat` cho giao diện (HUD, popup, nút, icon): phẳng, màu đặc, viền dày, đọc rõ trên điện thoại. `world-cartoon` cho thế giới trận (hero, quái, thành, nền, hiệu ứng): hoạt hình 2D nhìn ngang, viền tối, đổ bóng 2 bậc; mỗi nhân vật một hình đứng yên, chuyển động do game tạo bằng scale, move, rotate, đổi màu, hợp mức IDEA với sprite gen bằng AI. Tách hai style vì giao diện cần phẳng và tương phản cao, còn thế giới cần khối và phân biệt phe. Đã loại: một style chung cho tất cả (UI dễ chìm vào chiến trường); pixel art (khó gen đều tay bằng AI); 3D low poly (đã chọn game 2D). Chưa có ảnh tham chiếu; người bỏ ảnh `<id>-<tên>` vào `.art-style/` khi có.

- **Đổi style sang Colorful (2026-11-25, người yêu cầu qua @Manager, ảnh `Management/references/20261125-225339-lose.png`: "Tôi dùng phong cách này · Colorfull").** Viết lại cả hai style theo ảnh: màu bão hoà và tươi, viền vàng dày, ruy băng tím, chữ to tròn có viền và bóng, nút viên thuốc bóng như kẹo, icon emoji, trang trí sao, nơ, lá. Thay cho bản cũ "phẳng, màu đặc, không gradient, không glow". `world-cartoon` theo cùng tông (chibi, màu tươi, bóng mềm) để trận và UI là một game. Tự quyết:
  - **Giữ id `ui-flat`** dù tên không còn đúng nghĩa: hơn 50 order ở studio (feature, @UIDesigner, @Codex) khai `style: ui-flat`; đổi id làm hỏng các order đó. Tiêu đề file đã đổi thành "Giao diện Colorful kiểu casual mobile". Đã loại: id mới `ui-colorful` (phải sửa order của phiên khác).
  - **Gán màu theo vai trò nút** (xanh lá chính, xanh dương về) theo đúng ảnh, để mọi màn đọc giống nhau.
  - Ảnh tham chiếu nằm ở tab Tham chiếu; chưa chép vào `.art-style/` (người bỏ ảnh `ui-flat-<tên>` vào đó nếu muốn làm ảnh chính thức).
  - Hệ quả: hash gốc đổi; các node vòng 2 cập nhật `based-on`, không đổi ý nào. Ảnh concept cũ thành "CŨ" theo style mới.

- **Đưa góp ý GPT lần hỏi `style-colorful` (2026-11-25, `advice/_root/style-colorful.md`, 13 góp ý "thêm").** Mỗi góp ý thành một ý `art-*` ở node gốc (không con số, theo luật vòng 1); quy tắc cụ thể và con số (mã màu, tương phản 4,5:1 cho chữ HUD và 3:1 cho viền đơn vị, độ đục hiệu ứng tối đa 50%) ghi trong hai file `.art-style/`. Tự quyết:
  - **Bảng màu chung** đặt trong `ui-flat.md`, `world-cartoon.md` trỏ tới đó. Mã màu dự kiến, đo bằng mắt từ ảnh tham chiếu; người sửa được thẳng trong bảng.
  - **Nguồn sáng thống nhất "từ trên"** cho cả hai style (`art-light-from-top`); bỏ "trên bên trái" của `world-cartoon`. Đã loại: giữ lệch trái cho thế giới (hai công cụ lên khối khác nhau khi ghép).
  - **Ngôn ngữ chữ trong game là tiếng Việt** (`art-ui-text-font`): mọi node vòng 4 có giao diện đã chốt chữ tiếng Việt ("Chơi lại"); ảnh tham chiếu dùng tiếng Anh chỉ là ví dụ phong cách.
  - **Khung trận mẫu** (`art-battle-reference`, AI 6): chưa có. Dựng từ concept nền chiến trường và HUD sau khi người duyệt style mới; trước đó đối chiếu với frame đã chốt.
  - Điểm thấp hơn: `art-castle-edge-solid` (AI 5, chi tiết mức vẽ thành, gần vòng 4), `art-faction-silhouette`, `art-unit-outline-contrast`, `art-effect-overlay-limit` (AI 6, đúng nhưng là quy tắc kiểm asset hơn là quyết định định hình game).
  - Hash gốc đổi lần nữa; 24 node vòng 2 ghim lại `based-on`, không đổi ý.

- **Rà lại vòng 1 với GPT (2026-11-24), không đổi `data.md`.** Chín góp ý ở `advice/_root/root-gaps.md` và lượt phản biện `root-gaps-r2.md` đều không đổi quyết định nào ở gốc; chỉ thêm diễn giải ở đây. Giữ hash gốc để cả cây không phải làm lại.

- **Làm lại vòng 1 cùng GPT (2026-11-24, lần hỏi `advice/_root/root-r1.md`).** Ba góp ý loại "thêm", đều thành ý của node (luật mới: không bỏ góp ý nào):
  - `player-commander` (AI 6): đúng hướng và gom ba thao tác về một vai trò, nhưng phần lớn đã nằm sẵn trong `target-player` và `hero-auto-fight`; thêm ít quyết định mới.
  - `main-feel` (AI 7): chọn một cảm giác chủ đạo trong bốn cảm giác ở mục "Cảm giác chính" (gần "Tự tay cứu nguy"); giúp nhánh dưới biết ưu tiên nhịp trận.
  - `castle-damage-carries` (AI 7): khớp với thứ đã chốt ở `feature-castle` (thành không tự hồi máu giữa các đợt); đưa lên gốc để mọi nhánh bám theo.
  - Không góp ý nào đi ngược ba ý người đã chấm lại.

- **Câu hỏi chung vòng 2 với GPT (2026-11-24, `advice/_general/r2-all.md`, 7 góp ý "thêm").** Sáu góp ý đã thành ý của feature đích: `chu-so-huu-doi-duoc-nho` vào `feature-team-select` (`team-memory-prefill`), `chu-so-huu-thuong-tien` vào `feature-wallet` (`wallet-records-rewards`), `chu-so-huu-cap-nang` vào `feature-upgrade` (`upgrade-owns-levels`), `khoi-tao-luot-moi`, `quyen-chot-luot`, `yeu-cau-tam-dung` vào `feature-run` (`run-reset-owner`, `run-end-owner`, `run-pause-request`).
  - **Không dùng `chu-so-huu-ban-do` (thêm feature mới `feature-battlefield` giữ bố cục bản đồ trận).** Các mốc không gian chung (mép thành, mép màn, mặt đất) đã có một nơi giữ ở nhánh thân thành của `feature-castle`, các node khác dùng lại số đó. Thêm feature mới phải dựng cả nhánh vòng 3-4 và chuyển mọi frame đang dùng số của thành mà không đổi quyết định nào. Xét lại nếu bản đồ có thêm thứ ngoài thành và hai mép quái vào. **Cập nhật 2026-11-25:** người yêu cầu thêm `feature-battlefield` (nền chiến trường: trời, cảnh xa, dải đất, decor), chỉ để nhìn; các mốc không gian vẫn ở nhánh thân thành, nền dùng lại.
  - **Không sửa `run-reset` của gốc** dù GPT đề nghị hai lần (`chu-so-huu-doi-duoc-nho`, và `pham-vi-giu-ki-luc` ở `feature-run`). "Chỉ giữ kỉ lục" nói về tiến trình chơi; đội được nhớ chỉ là lựa chọn điền sẵn, mất khi tắt game, và đã ghi rõ ở `feature-team-select` (`team-memory-prefill`) và `feature-run` (`run-record`). Sửa gốc làm cả cây phải làm lại mà không đổi quyết định.

- **Câu hỏi chung vòng 2 lần hai, không rào (2026-11-24, `advice/_general/r2-all-2.md`, 9 góp ý "thêm").** Lần hỏi trước có rào do GameDesigner tự thêm; lần này bỏ hết.
  - Thành feature mới: `phan-xu-sat-thuong` → `feature-combat`; `tach-dieu-huong-man` → `feature-game-flow` (khai `node-by`).
  - Thành ý của feature đích: `kiem-tra-doi-hinh` → `feature-team-select` (`team-valid-before-start`); `sinh-doi-dau-tran` → `feature-hero` (`hero-spawn-team`); `mua-nang-cap-tron-ven` → `feature-upgrade` (`buy-one-level`); `tam-dung-khi-ra-nen` → `feature-run` (`run-pause-background`).
  - **Không dùng `gop-giao-dien-tran` (gộp `feature-hud-upgrade` vào `feature-hud`).** Trái luật vòng 2 của project ("HUD lớn thì tách cụm lớn"); cụm `feature-hud/r2-3` của chính GPT lại đề nghị giữ tách (`keep-upgrade-bar-apart`).
  - **Không dùng `tinh-so-tien-thuong` (ví tính số tiền thưởng).** Mức tiền hạ quái và thưởng dọn đợt đã chốt ở nhánh của `feature-enemy` và `feature-wave`; cụm `feature-wave/r2-3` của GPT giao việc tính cho quái và đợt, ví chỉ ghi (`enemy-computes-reward`, `wave-computes-reward`, và `wallet-records-rewards` ở `feature-wallet`).
  - **Không dùng `moc-chot-diem-luot` (tính điểm đợt dọn sạch cùng lúc thành bị phá).** Trái luật đã chốt "thua trước" ở nhánh luồng màn của `feature-run` và nhánh dọn đợt của `feature-wave`; giữ góp ý cùng chỗ `thu-tu-chot-thua` (`lose-before-clear` ở `feature-wave`).

- **Bỏ tạm dừng, quảng cáo và popup hồi sinh (2026-11-26, người quyết).** Bỏ ý `ad-revive` và hai feature `feature-ads`, `feature-popup-revive`; thành bị phá là thua; bỏ chữ "Tạm dừng" và màu nút quảng cáo khỏi các ý art.

## Chưa rõ
- Tên chính thức: chưa có, không chặn việc thiết kế.
- Ảnh tham chiếu chính thức trong `.art-style/`: chưa có; hướng style lấy từ ảnh popup thua ở tab Tham chiếu.
- **Đổi nhịp và đổi cánh** (từ góp ý `ap-luc-doi-canh`): `feature-wave/spawning` đang chia quái gần đều hai bên mỗi đợt. Có nên thỉnh thoảng dồn sức ép sang một cánh để người chơi phải đổi ưu tiên không: câu hỏi mở cho nhánh `feature-wave`, không đổi gốc.
- **Chi tiền có đánh đổi** (góp ý `chi-tien-co-danh-doi`): kiểm ở `feature-balance` và `feature-upgrade` rằng không món nào luôn đáng mua trước; thứ tự mua phải đổi theo thế trận.
- **Hồi chiêu và thời điểm** (góp ý `hoi-chieu-va-thoi-diem`): kiểm ở `feature-castle` và `feature-wave` rằng có lúc nên giữ kĩ năng cho đợt dồn sắp tới, không phải lúc nào cũng bấm ngay khi hồi xong.

## Góp ý của GPT
- ap-luc-doi-canh (advice/_root/root-gaps.md): bỏ ở gốc; đi ngược `spawn-both-sides` đã chốt của nhánh wave, `enemy-two-sides` đủ rộng; GPT đồng ý bỏ ở lượt phản biện; ghi thành câu hỏi mở ở "Chưa rõ".
- chi-tien-co-danh-doi (advice/_root/root-gaps.md): dùng, để vòng sau; ghi ở "Chưa rõ".
- hoi-chieu-va-thoi-diem (advice/_root/root-gaps.md): dùng, để vòng sau; ghi ở "Chưa rõ".
- diem-khac-biet (advice/_root/root-gaps.md): sửa; thêm ví dụ vào "Kể trong một phút" nhưng không hứa "một cánh sắp vỡ"; ví dụ theo bản GPT gợi ý ở `root-gaps-r2.md`.
- hero-co-diem-yeu (advice/_root/root-gaps.md): dùng; thêm vào mục "Nhiều hero hơn số chỗ" ở "Đã tự quyết".
- diem-theo-dot (advice/_root/root-gaps.md): dùng; đã khớp `feature-run`, thêm một câu ở "Không có thắng".
- choi-lai-tu-dau (advice/_root/root-gaps.md): dùng; sửa câu vòng lặp thành giữ đội hoặc đổi đội.
- mot-ban-do (advice/_root/root-gaps.md): dùng; thêm lí do ở "Một bản đồ duy nhất".
- pho-thong-de-doc (advice/_root/root-gaps.md): sửa theo điều kiện của GPT ở `root-gaps-r2.md`: "hero nào sắp gục" thay cho "cánh nào cần cứu"; thêm dòng ở "Người chơi".
- advice/_general/r2-all-4 (2026-11-26, sau khi tách `feature-save`): 8 góp ý đã thành ý của `feature-save` (danh sách ở `CLAUDE.md` của node đó). Hai góp ý không thành ý mới:
  - bo-luot-khong-xet-ki-luc: không dùng. Game không còn bỏ lượt (bỏ tạm dừng, 2026-11-26).
  - don-scene-khong-xoa-luu: đã có, đúng ý `feature-session-scene/session-clean#saved-data-only`; không thêm ý trùng.
