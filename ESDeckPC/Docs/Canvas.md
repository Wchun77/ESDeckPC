# ESDeck Canvas 頁面製作指南

這份文件說明怎麼為 ESDeck 的 **Canvas 模式**自製頁面（HTML）和設定檔（config）。
可以直接交給其他 AI 參考：照這份文件做出來的頁面，放進資料夾就能在裝置上顯示。

---

## 1. 運作原理（先看這段）

- ESDeck 是一台 800×480 觸控螢幕裝置，左側 80 px 是側邊欄，**右側 720×480 是 Canvas 內容區**。
- 頁面不是在裝置上跑的。**ESDeckPC 在電腦上用 WebView2（Chromium / Edge 核心）開啟你的 HTML**，
  以 720×480、縮放 1.0 的視窗截圖，再透過 USB 把畫面傳給裝置。
- PC 每 **150 ms** 截一次圖，以 16×16 px 的格子比對，只傳**有變動的格子**。
  - 變動面積小：無損傳送，清晰。
  - 一次變動超過畫面 1/4：改用 JPEG，比較省流量，但文字邊緣會模糊、可能殘留色塊。
- 裝置螢幕是 **RGB565（16 位元色）**：大面積的細緻漸層會出現色階或顆粒感。
- 觸控：裝置把觸控座標回傳 PC，PC 在頁面上模擬滑鼠。
  - **點一下** → 手指放開時，在按下的位置送出一次 `click`。
  - **拖曳超過 10 px** → 變成滾輪捲動（內容跟著手指走）。
  - 沒有多點觸控、沒有長按、沒有鍵盤輸入。

結論：**Canvas 適合「顯示為主、搭配簡單按鈕」的頁面**——時鐘、月曆、資訊看板、捷徑按鈕。
不適合動畫、拖曳操作、需要即時回饋的介面。

---

## 2. 檔案放哪

所有東西都在 ESDeckPC 執行檔旁邊的 `Canvas` 資料夾（ESDeckPC 選單 **Modes → Open Canvas Folder** 可直接開啟）：

```
Canvas/
├─ active.txt              目前使用中的 config 檔名，例如 neon.json
├─ configs/                設定檔：每個 .json 定義一組頁面（最多 4 頁）
│   ├─ default.json
│   └─ mytheme.json
├─ pages/                  頁面庫：一個資料夾 = 一頁
│   └─ my_clock/
│       ├─ index.html      必要，頁面本體
│       ├─ icon.png        選用，側邊欄圖示
│       └─ (其他 css / js / 圖片)
└─ shared/                 所有頁面共用的檔案（CSS、字型、圖片）
    └─ themes/
```

- 一個頁面 = `pages/` 底下一個**含有 `index.html` 的資料夾**。
- 資料夾和檔名請用**英文、數字、底線**，避免空白和中文（config 檔名最長 58 bytes）。
- `CanvasFiles/`（原始碼專案內）是**預設內容**，編譯時會複製到輸出的 `Canvas/`；
  自製的主題直接放輸出的 `Canvas/` 即可，不需要放進專案。

### 網址對應

`Canvas` 資料夾在 WebView 裡對應到 `https://canvas.esdeck/`：

| 檔案 | 網址 |
|---|---|
| `Canvas/pages/my_clock/index.html` | `https://canvas.esdeck/pages/my_clock/index.html` |
| `Canvas/shared/themes/neon.css` | `https://canvas.esdeck/shared/themes/neon.css` |

頁面裡請用**相對路徑**引用共用檔案，例如 `<link rel="stylesheet" href="../../shared/themes/neon.css">`。

---

## 3. Config 格式（configs/*.json）

```json
{
  "pages": [
    { "dir": "pages/my_clock",  "name": "時鐘", "icon": "" },
    { "dir": "pages/my_cal",    "name": "月曆", "icon": "shared/themes/my_cal_icon.png" }
  ],
  "settings": {
    "bg_image":  "shared/themes/my_bg.png",
    "side_icon": "shared/themes/my_gear.png"
  }
}
```

| 欄位 | 說明 |
|---|---|
| `pages` | 側邊欄的頁面，依序排列，**最多 4 個**（多的忽略） |
| `pages[].dir` | 頁面資料夾，相對於 `Canvas/` |
| `pages[].name` | 頁面名稱；沒有圖示時會把名稱畫在側邊欄按鈕上 |
| `pages[].icon` | 側邊欄圖示路徑，相對於 `Canvas/`。**留空** → 使用 `<dir>/icon.png`；也沒有 → 顯示名稱文字 |
| `settings.bg_image` | 在 Canvas 模式下，裝置 Settings 畫面的背景圖。留空 = 純色 |
| `settings.side_icon` | 側邊欄齒輪（Settings）按鈕的圖示。留空 = 預設齒輪 |

所有路徑都相對於 `Canvas/`，所以整個 `Canvas` 資料夾可以直接複製到別台電腦使用。

### 圖片規格

| 用途 | 建議尺寸 | 說明 |
|---|---|---|
| 側邊欄圖示 `icon.png` | 64×64 PNG | 等比例縮放到 64×64，貼在深灰色（#2a2a2a）按鈕上；可用透明背景 |
| 齒輪 `side_icon` | 64×64 PNG | 同上 |
| Settings 背景 `bg_image` | 720×480 PNG / JPG | 會等比例放大並置中裁切以填滿 720×480 |

---

## 4. 撰寫頁面（index.html）

### 基本範本

```html
<!DOCTYPE html>
<html><head><meta charset="utf-8">
<style>
  * { box-sizing: border-box; }
  html, body {
    margin: 0; width: 720px; height: 480px; overflow: hidden;
    background: #101418; color: #e8e8e8;
    font-family: "Segoe UI", "Microsoft JhengHei UI", sans-serif;
    font-variant-numeric: tabular-nums;   /* 數字等寬，跳動時不會左右晃 */
  }
  .time { position: absolute; left: 0; right: 0; top: 150px; text-align: center; font-size: 140px; }
</style></head>
<body>
  <div class="time" id="t">--:--</div>
<script>
  const pad = n => String(n).padStart(2, '0');
  let last = '';
  function tick() {
    const d = new Date();
    const s = `${pad(d.getHours())}:${pad(d.getMinutes())}`;
    if (s !== last) { document.getElementById('t').textContent = s; last = s; }   // 沒變就不要動 DOM
  }
  tick(); setInterval(tick, 1000);
</script></body></html>
```

### 必須遵守

1. **固定 720×480**：`html, body` 設 `width: 720px; height: 480px; overflow: hidden`。不要依賴視窗大小變化。
2. **不要做動畫**：不要用 CSS `animation` / `transition`、不要每幀更新的 canvas、不要影片或 GIF。
   每個變動都會被截圖傳送，動畫會吃滿 USB 頻寬，畫面也會卡。
3. **更新頻率越低越好**：時鐘每秒更新一次就夠；資料類每分鐘或更久。只在值真的改變時才改 DOM。
4. **變動範圍越小越好**：只改需要變的那幾個字。不要每秒重畫整個畫面或整塊背景。
5. **不要 hover / active 效果**：觸控模擬的滑鼠會停在最後點的位置，`:hover` 會一直卡著；
   `:active` 按下的效果也來不及顯示。按鈕請做成靜態外觀，按下後用文字回饋結果即可。
6. **不要捲動區域、不要拖曳操作**：拖曳會被轉成滾輪事件，而且延遲明顯。內容請一頁放完。
7. **不要輸入框**：裝置沒有鍵盤。
8. **按鈕要大**：至少約 80×60 px，彼此間距 10 px 以上。
9. **裝飾用的覆蓋層要加 `pointer-events: none`**：疊在最上面的外框、光暈、紋理之類的元素，
   會擋住底下按鈕的點擊。

### 建議

- **顏色**：用純色或簡單、色差小的漸層；全畫面的細緻漸層在 RGB565 下會有顆粒。
- **字型**：用 Windows 內建字型——`Segoe UI`、`Microsoft JhengHei UI`（微軟正黑）、`Consolas`、
  `PMingLiU`（新細明體）等。可以在 `shared/` 放字型檔用 `@font-face` 載入。
- **字級**：螢幕約 7 吋，內文 16 px 以上、重要數字 40 px 以上比較好讀。
- **直書**：`writing-mode: vertical-rl` 受字型影響可能排版不穩，建議改用「一個字一個區塊」往下排。
- **漸層或圖片背景**：可以，只要它**不會變動**——靜態的部分只會在進入頁面時傳一次。
- 頁面載入完成後約 0.4 秒擷取第一張畫面；之後載入的內容會透過差異更新補上。

---

## 5. 和電腦互動：`window.esdeck`

每個頁面都會自動注入 `window.esdeck`：

```js
// 執行一個動作（格式和 Deck 按鈕相同），回傳 Promise<string>（執行結果文字）
const result = await esdeck.action({ action: "launch", target: "notepad.exe" });

// 寫一行到 ESDeckPC 的 log
esdeck.log("hello");
```

在一般瀏覽器裡測試時沒有 `window.esdeck`，請先判斷：`if (window.esdeck) ...`。

### 可用的動作

| action | 欄位 | 範例 |
|---|---|---|
| `launch` | `target`：程式、檔案、資料夾或網址 | `{ action: "launch", target: "calc.exe" }`、`{ action: "launch", target: "https://example.com" }` |
| `hotkey` | `keys`：按鍵陣列，同時按下 | `{ action: "hotkey", keys: ["ctrl", "shift", "esc"] }`、`{ action: "hotkey", keys: ["win", "shift", "s"] }` |
| `sequence` | `keys`：組合鍵字串陣列，依序送出（間隔 50 ms），每個字串用逗號分隔按鍵 | `{ action: "sequence", keys: ["ctrl,c", "alt,tab", "ctrl,v"] }` |
| `text` | `target`：要輸入的文字，逐字模擬鍵入（中文輸入法開著時，中文和全形標點可能錯位） | `{ action: "text", target: "Hello" }` |
| `paste` | `target`：要貼上的文字，透過剪貼簿貼上（Ctrl+V），之後還原剪貼簿；中文、標點不會錯位，但 Ctrl+V 不是貼上的程式（部分終端機）不適用 | `{ action: "paste", target: "收到，謝謝！" }` |
| `media` | `target`：`play_pause` `stop` `prev` `next` `vol_up` `vol_down` `mute` | `{ action: "media", target: "play_pause" }` |
| `scroll` | `target`：`up` `down` `left` `right`；`amount`：120 = 一格（選填） | `{ action: "scroll", target: "down", amount: 360 }` |
| `mouse_click` | `target`：`double` = 雙擊，其他 = 單擊 | `{ action: "mouse_click", target: "double" }` |
| `power` | `target`：`shutdown` `restart` `logoff` `lock` `sleep` | `{ action: "power", target: "lock" }` |
| `discord` | `target`：`mute` `unmute` `deafen` `undeafen` `join_channel` `leave_channel`；`channel_id` | 需要 ESDeckPC 已連上 Discord |

`label` 欄位可選填，只用來顯示在 log 中。

按鍵名稱：`win` `ctrl` `alt` `shift` `enter` `tab` `esc` `del` `up` `down` `left` `right` `f1`～`f12` `prtscr`，
以及單一字母或數字。

### 按鈕範例

```html
<button class="btn" data-act='{"action":"media","target":"play_pause"}'>播放/暫停</button>
<div id="msg"></div>
<script>
  document.querySelectorAll('.btn').forEach(b => b.addEventListener('click', async () => {
    if (!window.esdeck) return;
    try { document.getElementById('msg').textContent = await esdeck.action(JSON.parse(b.dataset.act)); }
    catch (e) { document.getElementById('msg').textContent = String(e); }
  }));
</script>
```

### 系統狀態：`esdeck.stats()`

和 Monitor 模式同一組感測器（LibreHardwareMonitor）。頁面每秒呼叫一次即可；
停止呼叫約 6 秒後感測器會自動停止。剛開始的第一次可能回傳 `{}`。

```js
const s = await esdeck.stats();
// { cpu: 12, cpuTemp: 48, cpuMHz: 3600, cpuW: 25, ram: 41,
//   gpu: 3, gpuTemp: 40, vram: 18, gpuPower: 12,
//   netUp: 0, netDown: 1, disk: 2, ssdLife: 98, t: 1761234567890 }
```

| 欄位 | 單位 |
|---|---|
| `cpu` `ram` `gpu` `vram` `disk` | % |
| `cpuTemp` `gpuTemp` | °C |
| `cpuMHz` | MHz（以 100 MHz 為單位） |
| `cpuW` | W；`gpuPower`：NVIDIA 為 %，AMD / Intel 為 W |
| `netUp` `netDown` | MB/s（整數） |
| `ssdLife` | 剩餘壽命 %（每 10 秒更新） |
| `t` | 取樣時間（Unix 毫秒），可用來判斷資料是否過舊 |

### 正在播放：`esdeck.media()`

讀取 Windows 的媒體工作階段（Spotify、瀏覽器播放的 YouTube 等）。

```js
const m = await esdeck.media();
// { active: true, title, artist, album, pos: 毫秒, dur: 毫秒, playing: true, cover: 3 }
// 沒有播放中的工作階段：{ active: false }

const url = await esdeck.mediaCover();   // 封面 data: URL，沒有則為 ""
// m.cover 這個數字改變時才需要重新取封面

esdeck.mediaCmd('toggle');   // 'toggle' | 'next' | 'prev'
```

`pos` 在播放中會自動推算，不用自己計時；進度條每秒更新一次就夠。

### 抓網路資料：`esdeck.fetch(url)`

由 ESDeckPC 代為送出 HTTP GET，不受瀏覽器 CORS 限制（RSS、沒開放跨網域的 API 都能抓）。

```js
const r = await esdeck.fetch('https://example.com/feed.xml');
// { ok: true, status: 200, text: '<?xml ...', error: '' }
// 失敗：{ ok: false, status: 404 或 0, text: '', error: '原因' }
if (r.ok) {
  const xml = new DOMParser().parseFromString(r.text, 'text/xml');
}
```

- 只支援 `http` / `https`，逾時 15 秒，回應上限 8 MB。
- 編碼依回應的 charset，或 XML / HTML 宣告裡的 encoding（Big5 也可以），都沒有就當 UTF-8。
- 編輯器預覽也能用（讀取資料不受唯讀限制）。

---

## 6. 資料保存與網路

- **localStorage** 可以用，會保存在 `%LOCALAPPDATA%\ESDeckPC\WebView2`，重開也在。
  所有頁面同一個網域（`https://canvas.esdeck`），**key 請加上自己頁面的前綴**，例如 `myclock.alarm`，避免互相覆蓋。
- 切換頁面時會**重新載入**該頁；離開 Canvas 模式時頁面會關閉。所以頁面裡的計時器只在顯示時運作，
  需要延續的狀態（例如番茄鐘的開始時間）請存進 localStorage，回來時再算。
- 頁面可以用瀏覽器的 `fetch` 抓有開放 CORS 的 API（例如天氣），其他的用 `esdeck.fetch()`（見上）。
  兩者都要自行處理逾時與離線狀況，失敗時顯示舊資料或提示。

---

## 7. 怎麼偵測與套用

- **自動重新載入**：ESDeckPC 會監看整個 `Canvas` 資料夾。存檔後約 0.4 秒內：
  - 改到**目前顯示的頁面**資料夾或 `shared/` → 該頁重新載入。
  - 改到 `configs/` 或 `active.txt` → 重新讀 config，側邊欄圖示和頁面整批重送。
  - 其他頁面的變動不會觸發（切換到該頁時就會載入新版）。
- **新增頁面**：在 `pages/` 建資料夾、放 `index.html`，再把它加進某個 config 的 `pages`。
- **切換 config**（兩種方式，會互相同步）：
  - PC：**Modes → Canvas Editor...**，選 config → **Use This Config**。
  - 裝置：Canvas 模式下開 **Settings → Select Config**，清單由 PC 提供。
- **除錯**：**Modes → Canvas DevTools** 可以打開目前頁面的開發者工具（Console、元素檢查）。
  ESDeckPC 主視窗的 log 會顯示載入、傳送大小、錯誤和 `esdeck.log()` 的內容。
- 在電腦上用一般瀏覽器開 `index.html` 也能預覽，請把視窗調成 720×480 檢查排版。

---

## 8. 做一套新主題的步驟（給 AI 的檢查清單）

1. 決定主題名稱（英文小寫，例如 `ocean`）。
2. 建立共用樣式 `shared/themes/ocean.css`（顏色、字型、共用元件）。
3. 建立 1～4 個頁面：`pages/ocean_clock/index.html`、`pages/ocean_xxx/index.html`……
   每頁都引用 `../../shared/themes/ocean.css`。
4. 每頁放一張 64×64 的 `icon.png`（或在 config 的 `icon` 指定路徑）。
5. 選擇性：`shared/themes/ocean_gear.png`（64×64）和 `shared/themes/ocean_bg.png`（720×480）。
6. 建立 `configs/ocean.json`，依第 3 節的格式列出頁面和 settings。
7. 檢查：
   - [ ] 每頁都是固定 720×480、沒有捲軸、沒有東西超出邊界
   - [ ] 沒有 `animation` / `transition` / `:hover` / `:active` 樣式
   - [ ] 定時更新只改變動的元素，且頻率 ≤ 1 次/秒
   - [ ] 按鈕夠大，`esdeck.action` 有用 `if (window.esdeck)` 保護
   - [ ] localStorage 的 key 有加頁面前綴
   - [ ] config 裡的路徑都相對於 `Canvas/`，且檔案確實存在
