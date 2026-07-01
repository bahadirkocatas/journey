# Refactor Planı — Futbol Kariyer Kart Oyunu

Bu doküman, Claude (Sonnet) ile yapılan kapsamlı bir planlama oturumunun çıktısıdır.
40 dosyalık mevcut proje tek tek incelendi, sistemik sorunlar tespit edildi, kullanıcı
ile birlikte mimari kararlar netleştirildi. Bu dosya **canlı bir dokümandır** — her faz
tamamlandıkça "İlerleme Durumu" bölümü güncellenir. Yeni bir spec yazmadan önce bu
dosyanın tamamı okunmalı.

---

## 1. Proje Özeti

Reigns tarzı, kart kaydırmalı bir futbol kariyer simülasyonu. Oyuncu bir futbolcu
kariyerini yönetir: 5 kaynak (Form, Money, Fame, Relations, Focus), can sistemi, kriz
mekaniği, sezon/hafta döngüsü, faz ilerlemesi (Çaylak → Yükseliş → Prime → Efsane),
transfer sistemi, Şampiyonlar Ligi finali akışı. Veri JSON dosyalarından (cards, teams,
countries, crisis, endings) okunuyor.

**Neden refactor:** Proje "bitmeden çok özellik eklenmeye çalışılmış", üst üste yama
yapılmış. Sonuç: yarım kalmış refactor'lar, derlenmeyen kod, ölü özellikler, tutarsız
pattern'ler. Bu bir **büyük yeniden yazımdır**, yama değil.

**Geliştirici profili:** Solo, Türkçe konuşan, iteratif çalışan, mimari kararları
Claude'a devretmeye istekli ama profesyonel/endüstri standardı kod istiyor, kör
onaylama istemiyor.

---

## 2. Kesinleşmiş Mimari Kararlar

| Konu | Karar |
|---|---|
| Namespace | `Game.Data → Game.Core → Game.Services → Game.Gameplay → Game.UI` (tek yönlü bağımlılık) |
| Sahne yapısı | Tek sahne, panel aç/kapa ile menü geçişi |
| JSON kütüphanesi | Newtonsoft (`com.unity.nuget.newtonsoft-json`) — JsonUtility'nin dictionary/polimorfizm kısıtları yüzünden |
| Lokalizasyon | **Unity Localization Package** (resmi paket) — çoklu dil kullanıcı için en kritik özellik, eski regex parser tamamen kaldırılıyor |
| Singleton | `Singleton<T>` (sahne içi) / `PersistentSingleton<T>` (DontDestroyOnLoad) — tüm manager'lar buradan türer |
| Durum yönetimi | `GameStateMachine` + `GameState` enum — dağınık boolean bayrakların yerini alır |
| Event sistemi | Tek merkezi `GameEvents` — tüm dağınık static event'ler buraya toplandı |
| Sabitler | Tek merkezi `GameConstants` — eski `CardConstants` + `GameKeys` birleşti |
| Kart veri yapısı | `CardData` içinde sağ/sol tekrarı `ChoiceData` alt sınıfına çekildi |
| Encoding | UTF-8, BOM'suz, tüm dosyalarda |
| Asmdef | Rewrite bitince en sonda eklenecek (şimdi eklenirse Localization paketi kurulmadan önce derleme hatası verir) |

---

## 3. MVP Kapsamından Çıkarılanlar (SİLİNECEK)

- **Origin/Story seçim sistemi** (`StoryData.cs`, `stories.json`, origin tag'leri:
  "sokak çocuğu", "akademi dehası" vb.). Kullanıcı kararı: en eski yazılan
  sistemlerden biri, üzerine en çok yama yapılmış, düzelemeyecek seviyede. **Geri
  eklenmeyecek.**
- **Ödül (award) sistemi.** Zaten kısmen sökülmüştü (`StatsManager.earnedAwards`
  yorum satırına alınmış), kalıntılar (`SaveManager`'daki referanslar) temizlenecek.

## 4. MVP Sonrası Planlanan (ŞİMDİ YAZILMAYACAK — mimari buna kapanmasın)

Kullanıcının belirttiği, ileride eklenecek ama **bu refactor'da kod yazılmayacak**
özellikler:

1. **Hafızalı dolgu/zincir kartları** — örn. "geçen sezon UCL kazandın/kaybettin"
   şartına bağlı olarak gelen dolgu veya zincir kartları.
2. **Trait sistemi** — oyuncuya atanan özellikler, bunlara bağlı hafızalı kartlar.
3. Origin sistemi (bkz. madde 3) ileride farklı bir tasarımla geri gelebilir.

**Bu yüzden mimaride bilinçli olarak açık bırakılanlar:**
- Kart kategorileri (`GameConstants.Categories`) enum değil **string sabit** — JSON'dan
  yeni kategori eklenebilsin diye.
- `StoryManager`'ın flag/variable/string depoları **generic** tutuluyor — trait'ler
  ileride buraya oturabilsin diye.
- `RequirementService` yeni requirement tipi eklemeye kapalı değil.

---

## 5. JSON Kontratlarıyla Bulunan Uyumsuzluklar

`crisis.json`, `stories.json`, `endings.json`, `countries.json` incelendiğinde kodla
uyuşmayan noktalar:

- **`CrisisData`:** JSON'da `"condition": "Min"/"Max"` ve `"costsLife": true` alanları
  var. Mevcut C# modelinde `condition` okunmuyor, `costsLife` **hiç yok**. Sonuç:
  `CrisisManager` bir statın Min mi Max mı olduğuna bakmadan kriz seçiyor (örn. Form=0
  iken "doping" gibi bir Max krizi gelebiliyor — mantık hatası). **Düzeltilecek:**
  `condition` enum (`Min`/`Max`) + `costsLife` bool eklenecek, seçim mantığına
  uygulanacak.
- **`CrisisData`:** JSON'da `minPhaseIndex`/`maxPhaseIndex` yok, kod bunları
  okumaya çalışıp default (0–10) kullanıyor. Zararsız ama gereksiz alan — JSON'a
  eklenecekse eklensin, eklenmeyecekse koddan temizlensin (kullanıcıyla teyit
  edilecek, şimdilik default davranış korunuyor).
- **UCL risk/zar sistemi:** `cards_ucl.json`'da risk/zar mekaniğine dair alanlar var
  (`isRightRisk/isLeftRisk`, `rightRiskStat/leftRiskStat`, `rightRiskMode/leftRiskMode`
  — alan adları doğrulandı, `ChoiceData.isRisk/riskStat/riskMode` ile birebir eşleşiyor).
  `DiceManager` bu akışa **hiç bağlı değil** — ölü değil, sadece bağlantısı kopuk. UCL
  finali akışında gerçek zar atışına dönüştürülecek (bkz. madde 9, Katman 3).
- **`cards.json` veri hatası (`leftf` typo):** Bir kartta `leftForm`/`leftFocus` yerine
  yanlışlıkla `"leftf": 15` yazılmış. Kod tarafında karşılığı yok, sessizce düşüyor.
  CardData rewrite'ının kapsamı dışında; JSON düzenlenirken düzeltilecek.

---

## 6. Derlenmeyi Engelleyen Kritik Buglar (ÖNCELİK #1)

- `SaveManager`, `GameManager.currentLives` alanını okuyup yazıyor — ama `GameManager`
  bu alanı **dışarı açmıyor** (can `private LifeSystem` içinde gömülü).
- `SaveManager`, `StatsManager.earnedAwards` alanını okuyup yazıyor — bu alan
  **sökülmüş**, mevcut değil.
- Sonuç: proje muhtemelen şu anki haliyle **derlenmiyor**. Rewrite'ta bu iki
  uyumsuzluk kökten çözülecek (can sistemi düzgün expose edilecek, award referansları
  tamamen silinecek).

---

## 7. Sistemik Sorunlar (Tüm Projeyi İlgilendiren)

| # | Sorun | Çözüm |
|---|---|---|
| A | Derlenmeyen kod (SaveManager uyumsuzlukları) | Madde 6 |
| B | Singleton kalıbı her dosyada farklı (bazısı DontDestroyOnLoad, bazısı duplicate-check'siz) | `Singleton<T>` / `PersistentSingleton<T>` |
| C | Event bus ile direkt-çağrı karışık, event tanımları dağınık | Tek `GameEvents`, tutarlı kullanım kuralı |
| D | Yarım/ölü özellikler: cooldown hiç okunmuyor, `ResourceDisplay` bar'ları güncellenmiyor, DiceManager bağlı değil | Hepsi bağlanacak (D bulguları MVP kusursuzluğu için zorunlu) |
| E | Stat okumanın 3-4 farklı yolu var (`GameManager` alanları, `SetStat(string)`, `ApplyStatChange`, `DiceManager`'ın kendi switch'i, `RequirementService`'in kendi switch'i) | Tek `StatService` |
| F | Encoding kaosu (bozuk Türkçe karakterler eski dosyalarda) | UTF-8 standardizasyonu |
| G | Magic string'ler `GameConstants` dışında hâlâ kullanılıyor | Temizlik, tüm literal'ler sabitlere taşınacak |
| H | Synthetic kart üretimi (kriz/gameover/UCL node) 3 manager'a dağılmış, kriz çıkışı çift işleniyor | `SystemCardFactory` + tek sorumlu |
| I | Namespace/asmdef yok | Madde 2 |
| J | JsonUtility'nin dictionary kısıtı, Regex tabanlı localization parser kırılgan | Newtonsoft + Unity Localization Package |

---

## 8. Dosya Bazlı Karar Listesi

### YENİ dosyalar (rewrite'da eklenecek)
- `Singleton.cs`, `PersistentSingleton.cs` ✅ **YAZILDI**
- `GameState.cs`, `GameStateMachine.cs` ✅ **YAZILDI**
- `GameConstants.cs` ✅ **YAZILDI**
- `GameEvents.cs` ✅ **YAZILDI**
- `ChoiceData.cs` ✅ **YAZILDI**
- `StatService.cs` — bekliyor
- `DiceService.cs` (saf mantık; `DiceManager` sadece UI kalacak) — bekliyor
- `SystemCardFactory.cs` — bekliyor
- `UclFlowController.cs` (SeasonManager + UCLManager'dan ayrılan UCL akışı) — bekliyor
- `SeasonEndEvaluator.cs` (`SeasonScoring`'in side-effect'siz hali) — bekliyor
- `NationSelector.cs`, `ContractSelector.cs` (MainMenuManager'dan ayrılan) — bekliyor
- `TeamButtonView.cs` (MainMenu + Transfer'deki kopya buton üretim kodu için ortak bileşen) — bekliyor

### SİLİNECEK dosyalar
- `GameKeys.cs` (→ `GameConstants`'a birleşti) ✅ birleşim yapıldı, eski dosya rewrite bitince silinecek
- `CardConstants.cs` (→ `GameConstants`'a birleşti) ✅ birleşim yapıldı, eski dosya rewrite bitince silinecek
- `SeasonScoring.cs` (→ `SeasonEndEvaluator`'a dönüşecek)
- `UCLManager.cs` (→ `UclFlowController`'a birleşecek)
- `StoryData.cs` (→ origin sistemi silindi, bkz. madde 3)

### KORUNAN dosyalar (cilalanarak yeniden yazılacak, fikir olarak doğru)
CardData, RequirementData, HiddenEffect, StatEffect, CountryData, TeamData, CrisisData,
EndingData, SeasonScheduleModels, RequirementService, CardRegistry, CardDeckBuilder,
MatchSimulator, CardSwipeConfig, MatchSimulationConfig, SeasonCalendar, LifeSystem,
GameManager, CardManager, CrisisManager, CountryManager, TeamManager, StatsManager,
StoryManager, EndingManager, DiceManager (UI katmanına indirgenmiş hali), SaveManager,
LocalizationManager (Unity Localization Package'a geçişle beraber), CardUIHandler,
CardController, MainMenuManager (→ ince bir Controller'a indirgenecek), DashboardManager,
CareerSummaryUI, TransferUIManager, ResourceDisplay, SnapToItem.

---

## 9. Katman Planı ve İlerleme Durumu

> Her dosya bittiğinde burada ✅ işaretlenecek. Yeni bir Claude Code oturumu önce bu
> tabloya bakıp nereden devam edeceğini anlar.

### Katman 0 — `Game.Core` (altyapı) — **TAMAMLANDI**
- [x] `Singleton.cs`
- [x] `PersistentSingleton.cs`
- [x] `GameState.cs`
- [x] `GameStateMachine.cs`
- [x] `GameConstants.cs`
- [x] `GameEvents.cs`

### Katman 1 — `Game.Data` (saf veri modelleri) — **DEVAM EDİYOR**
- [X] `RequirementData.cs`
- [X] `ChoiceData.cs` — risk alan adları (`isRisk`/`riskStat`/`riskMode`) `cards.json` ve
      `cards_ucl.json` ile karşılaştırıldı: JSON'daki `isRightRisk/isLeftRisk`,
      `rightRiskStat/leftRiskStat`, `rightRiskMode/leftRiskMode` önekleri CardData'nın
      ayrı right/left tutmasıyla zaten kalkıyor — **birebir eşleşiyor, değişiklik
      yapılmadı**.
- [x] `HiddenEffect.cs`
- [x] `StatEffect.cs`
- [x] `CardData.cs` (ChoiceData kullanacak şekilde; `CardPool`, `CardCollection` dahil)
      — Tamamlandı: `Assets/_Scripts/Data/CardData.cs`'e taşındı, nested `right`/`left`
      (ChoiceData) kullanıyor, global namespace'teki eski `HiddenEffect`/`StatEffect`
      tanımları silindi (artık `Game.Data.HiddenEffect`/`Game.Data.StatEffect`
      kullanılıyor — duplike tip yok). JSON'da bulunan `isStoryCard` alanı eklendi
      (286× kullanılıyor, UCL/zincir kartları için). `addTraitRight/addTraitLeft`
      (trait sistemi, MVP-sonrası) bilinçli olarak eklenmedi.
      Bu değişiklik CardData'yı adıyla anan/flat alanlarına erişen 8 dosyayı mekanik
      olarak nested şekle uyarladı (derin/işlevsel rewrite değil, sadece derleme
      uyumu): `CardManager`, `CardUIHandler`, `GameManager`, `StatsManager`,
      `StoryManager`, `SeasonManager`, `UCLManager`, `CrisisManager`/`CardController`/
      `CardDeckBuilder`/`CardRegistry` (sadece `using Game.Data;` eklendi).
      **Bilinen geçici durum:** `CardRegistry` hâlâ `JsonUtility` ile yüklüyor; flat
      JSON alanlarını nested `right`/`left`'e eşleyemediği için JSON'dan gelen kartlar
      şu an etkisiz (stat etkisi 0) yükleniyor — NRE atmıyor çünkü `CardData.right/left`
      default olarak boş `ChoiceData()` ile başlatılıyor. Gerçek eşleme Katman 2'de
      Newtonsoft + custom `JsonConverter` ile gelecek. Synthetic kartlar (kriz/gameover/
      UCL) kodda kurulduğu için zaten doğru çalışıyor.
      Ayrıca `leftf` typo'su bulundu (`cards.json` içinde bir kartta `leftForm`/
      `leftFocus` yerine yanlışlıkla `"leftf"` yazılmış) — veri hatası, bu adımın
      kapsamı dışında, ileride JSON'da düzeltilecek.
      **NOT (isStoryCard — Katman 3'te karara bağlanacak):** `isStoryCard` origin/story
      sistemiyle **ilgisiz** (madde 3'te silinen sistem farklı bir şey; `StoryManager`
      da ayrı, o saf flag/variable hafızası). `cards_ucl.json` içinde incelendiğinde
      `isStoryCard` değeri kartın `category`'siyle **%100 örtüşüyor** (481 kartın
      tamamında istisnasız): `pre_match`/`locker_room`/`climax_chain` → hep false,
      `match_action`/`victory`/`defeat`/`city_aftermath`/kategori yok (zincir dalı
      kartları) → hep true. `chain_starter` kategorisi (ana `cards.json` destesinde
      kullanılan, farklı bir kavram) ile de çakışmıyor — `cards_ucl.json` o kategori
      adını hiç kullanmıyor. Şu an kodda `isStoryCard`'ı okuyan hiçbir yer yok; sadece
      veri kaybolmasın diye modele eklendi. `UclFlowController` yazılırken (Katman 3)
      karar verilecek: `isStoryCard` category'den bağımsız gerçek bir anlam taşıyacak
      mı (örn. deste kurma mantığının zincir-dışı kartları filtrelemesi için), yoksa
      redundant olduğu için silinip yerine category bazlı kontrol mü kullanılacak.
- [x] `CountryData.cs` (+ `CountryCollection`, `List<>` tutarlılığı sağlanacak — bazı
      koleksiyonlar array bazıları List, hepsi List'e sabitlenecek) — Tamamlandı:
      `Assets/_Scripts/CountryData.cs` → `Assets/_Scripts/Data/CountryData.cs` (GUID
      korunarak `git mv`), `Game.Data` namespace'ine taşındı, XML doc yorumları
      eklendi, gereksiz `using UnityEngine` kaldırıldı. `CountryCollection.countries`
      `CountryData[]` → `List<CountryData>` oldu (`CardCollection`'daki `List<CardData>`
      önceliğiyle tutarlı; `JsonUtility` List alanını array gibi deserialize ediyor,
      `CountryManager` değişmeden çalışıyor). `isPlayableOrigin` alanı kullanıcı
      kararıyla **aynı isimle korundu** (JSON/scene kontratı bozulmasın diye); silinen
      Origin/Story sistemiyle ilgisi olmadığını belirten bir XML doc notu eklendi.
      `CountryManager.cs`'e `using Game.Data;` eklendi (tip adını açıkça kullanıyor);
      `MainMenuManager.cs` dolaylı kullandığı için (`var country`) değişmedi.
- [x] `TeamData.cs` (+ `TeamCollection`) — Tamamlandı: `Assets/_Scripts/TeamData.cs`
      → `Assets/_Scripts/Data/TeamData.cs` (GUID korunarak `git mv`), `Game.Data`
      namespace'ine taşındı, bozuk Türkçe karakterli yorumlar (`YEN�`, `kontrol�`)
      temiz XML doc yorumlarıyla değiştirildi, gereksiz `using UnityEngine` kaldırıldı.
      `TeamCollection.teams` `TeamData[]` → `List<TeamData>` oldu (`CountryCollection`/
      `CardCollection` ile tutarlı; `TeamManager`'daki `new List<TeamData>(data.teams)`
      değişmeden çalışıyor). Alan sayısı/adları `teams.json` ile birebir uyumlu,
      hiçbir alan eklenmedi/çıkarılmadı. `TeamManager.cs`, `MainMenuManager.cs`,
      `TransferUIManager.cs`'e `using Game.Data;` eklendi (tip adını açıkça
      kullanıyorlar); `GameEvents.cs` ve `SeasonManager.cs` zaten sahipti, değişmedi.
- [ ] `CrisisData.cs` (+ `CrisisCollection`) — `condition` enum + `costsLife` eklenecek
- [ ] `EndingData.cs` (+ `EndingCollection`)
- [ ] `SeasonSchedule.cs` (+ `ScheduleCollection`)
- [ ] `SeasonHistoryData.cs` (StatsManager içinden çıkarılıp buraya taşınacak)

### Katman 2 — `Game.Services` (saf C#, test edilebilir) — **BEKLİYOR**
- [ ] `StatService.cs`
- [ ] `RequirementService.cs` (cache invalidation + cooldown entegrasyonu eklenecek)
- [ ] `CardRegistry.cs`
- [ ] `CardDeckBuilder.cs` (cooldown + requirement filtresi gerçekten bağlanacak)
- [ ] `MatchSimulator.cs` (büyük ölçüde korunacak, örnek kalitede bir dosya)
- [ ] `DiceService.cs` (BG3 tarzı zar mantığı, UI'dan ayrılmış)
- [ ] `SystemCardFactory.cs` (kriz/gameover/UCL synthetic kartları tek yerden üretir)

### Katman 3 — `Game.Gameplay` (manager'lar) — **BEKLİYOR**
- [ ] `GameManager.cs` (ince koordinatör; Stat/Life/Phase servislerine delege eder)
- [ ] `CardManager.cs` (synthetic üretim dışarı alınmış, `QueueCount`/`queueCount`
      çiftlemesi giderilmiş)
- [ ] `SeasonManager.cs` (sadece zaman akışı; UCL ve sezon-sonu ayrılmış)
- [ ] `UclFlowController.cs` (SeasonManager + eski UCLManager'ın UCL mantığı birleşik;
      risk/zar akışı `DiceService`'e gerçekten bağlanacak)
- [ ] `SeasonEndEvaluator.cs`
- [ ] `CrisisManager.cs` (Min/Max ayrımı + costsLife + tek-çıkış garantisi)
- [ ] `CountryManager.cs`
- [ ] `TeamManager.cs`
- [ ] `StatsManager.cs`
- [ ] `StoryManager.cs` (saf hafıza rolü korunuyor)
- [ ] `EndingManager.cs`
- [ ] `SaveManager.cs` (Newtonsoft + save versiyonlama + güvenli yükleme)
- [ ] `LocalizationManager.cs` → Unity Localization Package entegrasyonu

### Katman 4 — `Game.UI` — **BEKLİYOR**
- [ ] `CardUIHandler.cs`
- [ ] `CardController.cs`
- [ ] `DashboardManager.cs`
- [ ] `CareerSummaryUI.cs`
- [ ] `TransferUIManager.cs`
- [ ] `ResourceDisplay.cs` (stat bar güncellemesi `OnResourcesUpdated`'a bağlanacak — şu an ölü)
- [ ] `SnapToItem.cs`
- [ ] `MainMenuController.cs` (eski `MainMenuManager`'ın ince hali)
- [ ] `NationSelector.cs`
- [ ] `ContractSelector.cs`
- [ ] `TeamButtonView.cs`
- [ ] `DiceManager.cs` (artık sadece UI/animasyon; mantık `DiceService`'te)

### Son Adım — Takas ve Temizlik
- [ ] Asmdef dosyalarını katmanlara göre ekle (`Game.Data.asmdef`, `Game.Core.asmdef`, ...)
- [ ] Unity Editor'de eski `Assets/Scripts` yedeklenip yeni ağaçla değiştirilecek
      (bkz. sohbet: önce `.meta` dosyalarına dikkat, sahne/prefab Inspector
      referanslarının ekran görüntüsü alınacak)
- [ ] Tüm sahne/prefab referansları elle doğrulanacak
- [ ] Tam bir oynanış testi (yeni kariyer başlat → birkaç hafta ilerlet → kriz tetikle →
      sezon bitir → transfer → UCL finali → save/load)

---

## 10. Öncelik Sırası

1. Derlenmeyi engelleyen buglar (madde 6)
2. Katman 0 + Katman 1 tamamlanması (altyapı + veri modelleri)
3. Katman 2 (servisler) — cooldown, stat servisi, dice servisi burada gerçek işlevine
   kavuşuyor
4. Katman 3 (manager'lar) — özellikle `SeasonManager`/`UclFlowController` bölünmesi ve
   `CrisisManager` Min/Max düzeltmesi
5. Katman 4 (UI) — `ResourceDisplay` bar bağlantısı burada MVP için kritik
6. Asmdef + takas + tam oynanış testi

---

## 11. Netleşen Kullanıcı Kararları (referans — sorgulama, uygula)

- Lokalizasyon: **Unity Localization Package** kullanılacak (regex parser atılacak).
- Origin sistemi: **silinecek**, geri gelmeyecek.
- Sahne yapısı: **tek sahne** kalacak, bölünmeyecek.
- JSON: **Newtonsoft** kullanılacak.
- `DiceManager`: **ölü değil**, UCL final risk kartlarına bağlı; gerçek işlevine
  kavuşturulacak, silinmeyecek.
- Cooldown: kullanıcı orijinal amacını tam hatırlamıyor ama kart havuzu mantığından
  önce eklenmiş bir sistem; havuz mantığı zaten isteniyor, o yüzden **cooldown gerçek
  akışa bağlanacak** (silinmeyecek).
- Hafızalı zincir/dolgu kartlar ve trait sistemi: **MVP sonrası**, şimdi kod
  yazılmayacak ama mimari kapanmayacak (madde 4).

---

## 12. Claude Code Kullanım Notu

- Model: `opusplan` (Opus planlar, Sonnet yazar).
- Her faz: Plan Mode'a gir → bu dosyayı ve ilgili mevcut dosyaları oku → belirsizlik
  varsa soru sor → plan onayla → Sonnet yazsın → Unity konsolunda derleme kontrolü →
  temizse commit.
- Var olan `Game.Core` ve kısmi `Game.Data` dosyalarını **yeniden yazma**, olduğu gibi
  bırak, üzerine devam et.
- Bu plana aykırı bir karar gerekiyorsa önce bu dosyaya yeni bir madde olarak
  gerekçesiyle ekle, sonra kodu yaz.
