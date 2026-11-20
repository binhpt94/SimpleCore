# Shop

Bán vật phẩm bằng xu. Làm chủ danh mục vật phẩm và giá; không giữ số xu.

- Dùng `IWalletService.TrySpend` (Wallet) để trừ xu, `IPopupService.Show` (Popup) để báo mua xong.
- Chưa có Contracts: chưa feature nào cần gọi Shop. Ai cần thì đề xuất.
- Danh mục và prefab popup: `ShopConfig.asset` trong `FeatureAssets/Private`, lúc chạy chỉ đọc.
- Popup "đã mua" là của Shop (`PurchasedPopup.prefab`), Shop đưa prefab cho dịch vụ Popup lúc chạy.
- Logic nằm trong `ShopLogic` (C# thuần). View chỉ gửi yêu cầu mua, lấy logic qua `ShopRuntime`.
- Phải được cài sau Wallet và Popup trong `Boot.prefab`.

Test: assembly `Game.Shop.Tests` (EditMode), dùng wallet và popup giả.
