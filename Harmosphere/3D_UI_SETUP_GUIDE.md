# 3D LLM 選項系統 Unity 設定指南

本指南說明如何在 Unity 中使用 3D 物件和 TextMeshPro 設定 LLM 選項選擇系統。

## 系統概述

新的系統使用：
- **3D TextMeshPro** - 世界空間中的文字（不是 Canvas UI）
- **3D 按鈕物件** - 立方體或其他幾何體作為可點擊按鈕
- **Collider 碰撞器** - 用於射線檢測點擊
- **射線投射 (Raycast)** - 檢測滑鼠點擊

## Unity 設定步驟

### 第 1 步：創建選項顯示容器 GameObject

1. 在 Hierarchy 中右鍵 → `Create Empty`
2. 命名為 `OptionSelectionContainer`
3. 設定位置：
   - Position: X=0, Y=0, Z=5 (放在相機前方)
   - Rotation: X=0, Y=0, Z=0
   - Scale: X=1, Y=1, Z=1

4. 在 Inspector 中 Add Component → `OptionSelectionUI3D`

---

### 第 2 步：創建歡迎文字

1. 在 `OptionSelectionContainer` 下右鍵 → `3D Object` → `Quad`
2. 命名為 `WelcomeText`
3. 設定 Transform：
   - Position: X=0, Y=2.5, Z=0
   - Rotation: X=0, Y=0, Z=0
   - Scale: X=6, Y=1, Z=1

4. 添加 TextMeshPro 組件：
   - Add Component → 搜尋 `TextMeshPro`（如果沒有會要求 import）
   - 在彈出視窗中匯入 TMP Essentials

5. 設定 TextMeshPro：
   - **Text**: 留空（會由程式碼填充）
   - **Font Size**: 36
   - **Alignment**: 置中對齊（Center）
   - **Vertex Color**: 白色 (R=255, G=255, B=255)
   - **Overflow**: Overflow
   - **Page to Screen Size**: ✓ 勾選

6. 移除不需要的組件：
   - 選擇 `WelcomeText`，移除 `Mesh Renderer` 的材質（或保留黑色背景）
   - 移除 `BoxCollider`（如果有的話）

7. 添加 Mesh Collider（用於射線檢測）：
   - Add Component → `Mesh Collider`
   - Convex: 勾選

---

### 第 3 步：創建第一個選項按鈕

#### 3.1 創建按鈕物件
1. 在 `OptionSelectionContainer` 下右鍵 → `3D Object` → `Cube`
2. 命名為 `OptionButton_0`
3. 設定 Transform：
   - Position: X=-2, Y=0.5, Z=0
   - Rotation: X=0, Y=0, Z=0
   - Scale: X=1.5, Y=0.5, Z=0.1

4. 移除 `BoxCollider`（我們稍後會重新添加）

5. 添加組件：
   - Add Component → `Box Collider`（用於射線檢測）
   - 確保 Collider 的大小與按鈕相符

#### 3.2 為按鈕添加文字
1. 在 `OptionButton_0` 下右鍵 → `Create Empty`
2. 命名為 `OptionText_0`
3. 設定 Transform：
   - Position: X=0, Y=0, Z=-0.1（在按鈕前方）
   - Rotation: X=0, Y=0, Z=0
   - Scale: X=1, Y=1, Z=1

4. 添加 TextMeshPro：
   - Add Component → `TextMeshPro - Text`
   - **Text**: 留空（會由程式碼填充）
   - **Font Size**: 20
   - **Alignment**: 置中對齊
   - **Vertex Color**: 黑色 (R=0, G=0, B=0)

#### 3.3 設定按鈕材質
1. 選擇 `OptionButton_0`
2. 在 Project 窗口中創建材質：
   - 右鍵 → `Create` → `Material`
   - 命名為 `ButtonDefault`
   - 設定顏色為淺藍色 (RGB: 100, 150, 255)

3. 拖曳此材質到 `OptionButton_0` 的 Renderer

4. 複製材質並創建其他兩個：
   - `ButtonHovered` - 亮藍色 (RGB: 150, 200, 255)
   - `ButtonSelected` - 深藍色 (RGB: 50, 100, 200)

---

### 第 4 步：複製按鈕

1. 選擇 `OptionButton_0`
2. Ctrl+D（或 Cmd+D）複製
3. 命名為 `OptionButton_1`
4. 設定 Position：X=0, Y=0.5, Z=0（中間）

5. 再複製一次
6. 命名為 `OptionButton_2`
7. 設定 Position：X=2, Y=0.5, Z=0（右邊）

**最終佈局**：
```
OptionSelectionContainer
├─ WelcomeText (Y=2.5)
├─ OptionButton_0 (Position X=-2, Y=0.5)
│  └─ OptionText_0
├─ OptionButton_1 (Position X=0, Y=0.5)
│  └─ OptionText_1
└─ OptionButton_2 (Position X=2, Y=0.5)
   └─ OptionText_2
```

---

### 第 5 步：設定 LLMOptionGenerator GameObject

1. 在 Hierarchy 中右鍵 → `Create Empty`
2. 命名為 `LLMOptionGenerator`
3. Add Component → `LLMOptionGenerator`
4. （其他設定已預設）

---

### 第 6 步：連接到 RhythmGameManager

1. 在場景中找到有 `RhythmGameManager` 腳本的物件
2. 在 Inspector 中找到 "LLM Option System" 部分
3. 設定：
   - **Llm Option Generator**: 拖曳 `LLMOptionGenerator` GameObject 過來
   - **Option Selection UI 3D**: 拖曳 `OptionSelectionContainer` GameObject 過來

4. 返回 `OptionSelectionContainer`，在 Inspector 中找到 `OptionSelectionUI3D` 組件
5. 設定：
   - **Welcome Text**: 拖曳 `WelcomeText` 過來
   - **Option Texts**:
     - Element 0: 拖曳 `OptionText_0` 過來
     - Element 1: 拖曳 `OptionText_1` 過來
     - Element 2: 拖曳 `OptionText_2` 過來
   - **Option Button Objects**:
     - Element 0: 拖曳 `OptionButton_0` 過來
     - Element 1: 拖曳 `OptionButton_1` 過來
     - Element 2: 拖曳 `OptionButton_2` 過來
   - **Option Colliders**:
     - Element 0: 拖曳 `OptionButton_0` 過來（它的 Collider）
     - Element 1: 拖曳 `OptionButton_1` 過來（它的 Collider）
     - Element 2: 拖曳 `OptionButton_2` 過來（它的 Collider）
   - **Default Button Material**: 拖曳 `ButtonDefault` 過來
   - **Hovered Button Material**: 拖曳 `ButtonHovered` 過來
   - **Selected Button Material**: 拖曳 `ButtonSelected` 過來

6. **重要**：在 Inspector 頂部取消勾選 `OptionSelectionContainer` 的 active 狀態（預設隱藏）

---

## 最終檢查清單

- [ ] `OptionSelectionContainer` 已建立且非 active
- [ ] `WelcomeText` 已建立並添加 TextMeshPro 和 Mesh Collider
- [ ] `OptionButton_0/1/2` 已建立，位置正確
- [ ] 每個按鈕都有對應的 `OptionText_X` 子物件
- [ ] 三個按鈕材質已建立（Default、Hovered、Selected）
- [ ] `LLMOptionGenerator` GameObject 已建立並添加腳本
- [ ] `OptionSelectionUI3D` 的所有欄位已填充
- [ ] `RhythmGameManager` 的 LLM Option System 欄位已填充
- [ ] `OptionSelectionContainer` 已設為非 active

---

## 測試

1. 按下 Play
2. 遊戲啟動後應該會：
   - 調用 Gemini API 生成介紹選項
   - 顯示 3D 歡迎文字和三個按鈕
   - 當滑鼠懸停在按鈕上時，按鈕顏色改變（亮藍色）
   - 點擊按鈕後，按鈕變為深藍色並隱藏 UI
   - 繼續遊戲流程

3. 檢查 Console 日誌：
   ```
   [IntroSequence] Starting to generate intro options...
   [IntroSequence] LLM options generated successfully
   [OptionSelectionUI3D] Options displayed. Waiting for user selection...
   [IntroSequence] User selected option X: [selected option text]
   ```

---

## 常見問題

### 按鈕看不到文字
- 檢查 `OptionText_X` 的 Position 是否正確（應在按鈕 Z 前方，如 Z=-0.1）
- 檢查文字的 Vertex Color 是否為黑色
- 確認 TextMeshPro 已正確導入（如未導入會有警告）

### 滑鼠點擊無反應
- 檢查每個按鈕是否都有 `Box Collider`
- 確認 Collider 的 Convex 選項已勾選（如果按鈕有 Mesh Collider）
- 檢查 Camera.main 是否存在（某些場景可能沒有主相機）
- 確認鼠標位置在遊戲視窗內

### 文字顏色看不清
- 調整 `WelcomeText` 和 `OptionText_X` 的 Vertex Color
- 考慮添加背景（如創建另一個 Quad 在文字後方）
- 檢查場景的光照設定

### 按鈕懸停效果沒有
- 檢查三個材質是否都已正確賦予
- 確認 `DetectHover()` 中的 Raycast 距離足夠（預設 100f）
- 檢查 Console 是否有錯誤

---

## 進階調整

### 改變按鈕位置
編輯 `OptionButton_X` 的 Position：
- Y 軸：上下移動（預設 Y=0.5）
- X 軸：左右間距（預設 -2, 0, 2）
- Z 軸：前後深度

### 改變按鈕大小
編輯 `OptionButton_X` 的 Scale：
- 預設為 X=1.5, Y=0.5, Z=0.1（寬、高、深）

### 改變文字大小
編輯 `OptionText_X` 的 Font Size：
- 目前設為 20
- 調整 Scale 也可以改變顯示大小

### 改變文字位置
編輯 `OptionText_X` 的 Position：
- Z 軸必須小於 0（在按鈕前方），否則會被按鈕遮擋
- X、Y 軸可以調整文字在按鈕上的位置

---

## 材質參考

如果要自定義按鈕顏色，可以編輯三個材質：

**ButtonDefault** (默認、未懸停)
- Albedo Color: RGB(100, 150, 255) - 淺藍色

**ButtonHovered** (懸停時)
- Albedo Color: RGB(150, 200, 255) - 亮藍色

**ButtonSelected** (選擇後)
- Albedo Color: RGB(50, 100, 200) - 深藍色

---

## 遊戲流程集成

新的選項系統在遊戲流程中的位置：

```
遊戲啟動
    ↓
Start() → IntroSequence()
    ↓
GenerateAndDisplayIntroOptions()
    ├─ LLMOptionGenerator 生成選項
    ├─ OptionSelectionUI3D 顯示 3D UI
    └─ 等待用戶選擇
    ↓
停止 BGM
    ↓
選擇第一首歌 (60 BPM)
    ↓
載入 Beatmap
    ↓
遊戲開始
```

現在用戶的選擇儲存在 `RhythmGameManager.userSelectedOptionIndex` 中，後續可以用來影響遊戲內容。
