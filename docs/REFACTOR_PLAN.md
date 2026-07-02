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
EndingData, SeasonSchedule, RequirementService, CardRegistry, CardDeckBuilder,
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
- [x] `CrisisData.cs` (+ `CrisisCollection`) — Tamamlandı: `Assets/_Scripts/CrisisData.cs`
      → `Assets/_Scripts/Data/CrisisData.cs` (GUID korunarak `git mv`), `Game.Data`
      namespace'ine taşındı, bozuk Türkçe karakterli yorumlar (`YEN�`, `Ge�erli`) temiz
      XML doc yorumlarıyla değiştirildi, gereksiz `using UnityEngine` kaldırıldı.
      `condition` alanı `string` → yeni `CrisisCondition` enum (`Min`/`Max`, sıra
      önemli — default `Min`) oldu; yeni `costsLife` (bool) alanı eklendi (JSON'da
      vardı, modelde hiç yoktu, sessizce düşüyordu). `CrisisManager`/`CardManager`
      zaten `using Game.Data;` içerdiği için **hiçbir consumer değişmedi**.
      `minPhaseIndex`/`maxPhaseIndex` kullanıcı kararıyla **default'larıyla korundu**
      (crisis.json'da karşılığı yok, kaldırma kararı Katman 3'e ertelendi, madde 5).
      `GameConstants.CrisisConditions` (Game.Core, hiç kullanılmayan Min/Max string
      sabitleri) kullanıcı kararıyla **şimdilik bırakıldı** — Katman 3'te
      `CrisisManager` Min/Max seçim mantığına kavuşurken temizlenecek (enum tek doğru
      kaynak olacak, string sabitler silinecek).
      **Bilinen geçici durum (Katman 2'de çözülecek):** `CrisisManager` hâlâ
      `JsonUtility` ile yüklüyor; `JsonUtility` enum'u isimden çözemediği için
      `condition` şu an yüklenen krizlerde default `Min`'e düşüyor — runtime'da fark
      yaratmıyor çünkü `condition` hiçbir yerde henüz okunmuyor (seçim mantığı hâlâ
      yalnızca `targetStat`'a bakıyor, bkz. madde 5). Newtonsoft'a geçişle (Katman 2)
      doğru dolacak. `costsLife` (bool) `JsonUtility` ile şimdiden doğru yükleniyor.
- [x] `EndingData.cs` (+ `EndingCollection`) — Tamamlandı: `Assets/_Scripts/EndingData.cs`
      → `Assets/_Scripts/Data/EndingData.cs` (GUID korunarak `git mv`), `Game.Data`
      namespace'ine taşındı, bozuk Türkçe karakterli yorumlar (`Ya�land�`, `�zel`,
      `J�B�LE`) temiz XML doc yorumlarıyla değiştirildi, gereksiz `using UnityEngine`
      kaldırıldı. 6 alan `endings.json` ile birebir korundu, hiçbir alan eklenmedi/
      çıkarılmadı. `triggerType` kullanıcı kararıyla **string kaldı** (enum'a
      çevrilmedi) — kategori-string mimari kararıyla tutarlı, "Specific" ileride ID
      bazlı özel sonlara açık kalsın diye, ayrıca `JsonUtility`'nin enum-by-name
      pitfall'ı (bkz. `CrisisData.condition`'daki geçici bozulma) hiç yaşanmadı.
      `EndingManager.cs`'e `using Game.Data;` eklendi (tip adlarını doğrudan
      kullanıyor); `CardManager.cs` zaten `using Game.Data;` içerdiği için değişmedi.
- [x] `SeasonSchedule.cs` (+ `ScheduleCollection`) — Tamamlandı: `Assets/_Scripts/
      SeasonScheduleModels.cs` → `Assets/_Scripts/Data/SeasonSchedule.cs` (GUID
      `b8c27ea9822b88845bc295c149c5c648` korunarak `git mv`, dosya adı EndingData/
      CrisisData kalıbına uyacak şekilde birincil tipin adına çevrildi), `Game.Data`
      namespace'ine taşındı, XML doc yorumları eklendi. `storyStarterIDs` kullanıcı
      kararıyla **aynı isimle korundu** — silinen Origin/Story sistemiyle (madde 3)
      ilgisiz olduğu, MVP-sonrası hafızalı zincir kartları (madde 4) için bir kanca
      olduğu ve içeriğinin `CardManager.GetCardByID` ile çözülen kart ID'leri (JSON
      değil) olduğu belirten bir doc notu eklendi — `isStoryCard`/`isPlayableOrigin`'de
      yapılan netleştirme kalıbıyla aynı. `SeasonManager.cs` zaten `using Game.Data;`
      içerdiği için **hiçbir consumer değişmedi**. Projede henüz bir schedule JSON'u
      yok ve `SeasonManager.scheduleJsonFile` atanmamış görünüyor — sistem şu an
      uykuda, bu taşıma davranışı değiştirmedi.
- [x] `SeasonHistoryData.cs` (StatsManager içinden çıkarılıp buraya taşınacak) —
      Tamamlandı: `Assets/_Scripts/StatsManager.cs` içindeki inline tanım (5 alan:
      `age`, `teamName`, `goals`, `assists`, `rating`) silinip yeni
      `Assets/_Scripts/Data/SeasonHistoryData.cs`'e taşındı, `Game.Data` namespace'ine
      alındı, XML doc yorumları eklendi. Diğer Katman 1 tiplerinden farklı olarak bu
      tip JSON'dan yüklenmiyor (sezon sonunda `StatsManager.ArchiveCurrentSeason`
      tarafından runtime'da üretiliyor) — bu yüzden `git mv`/GUID koruması ve bir
      `Collection` sarmalayıcısı gerekmedi, sıfırdan yeni dosya oluşturuldu.
      `StatsManager.cs` zaten `using Game.Data;` içerdiği için `careerHistory` alanı ve
      `ArchiveCurrentSeason` içindeki `new SeasonHistoryData(...)` değişmeden çalışıyor.
      `CareerSummaryUI.cs`/`DashboardManager.cs` tipe `var` ile eriştiği için
      (`StatsManager.Instance.careerHistory`), tip adını hiç yazmıyorlar —
      **hiçbir consumer değişmedi**. `SampleScene.unity`'deki `careerHistory: []`
      referansı, namespace değişikliğinin düz `[System.Serializable]` sınıfların Unity
      serileştirmesini etkilememesi nedeniyle bozulmadı.

**Katman 1 TAMAMLANDI.**

### Katman 2 — `Game.Services` (saf C#, test edilebilir) — **TAMAMLANDI**
- [x] `StatService.cs` — Tamamlandı: `Assets/_Scripts/Services/StatService.cs` (yeni
      `Services/` klasörü, `Data/` klasörü emsaliyle tutarlı). Saf C# sınıf
      (MonoBehaviour DEĞİL, `GameStateMachine` gibi — sahibi Katman 3'te `GameManager`
      `new` ile oluşturacak). 5 kaynağı (`Dictionary<StatType,int>`) tutar; `Get`/`Set`/
      `Apply` hem `StatType` enum hem `string` (`GameConstants.Stats`) overload'larıyla
      çalışır; string→enum çözümü **TEK yerde** (`StatService.TryResolve`) yapılır —
      eskiden `GameManager` (`Enum.TryParse`), `RequirementService` (eski
      `CardConstants` switch'i) ve `DiceManager` (küçük-harf literal switch, **`money`
      dalı hiç yoktu** — sessizce 50 dönüyordu) olmak üzere 3 ayrı yerde tekrarlanan
      mantığın birleştiği nokta burası. `Set`/`Apply` clamp uygular ve `StatBoundary`
      (`None`/`Min`/`Max`) döner; `ApplyChoice(ChoiceData)` bir kart seçiminin 5 stat
      deltasını tek seferde uygulayıp sınıra çarpan statları döner.
      **Bilinçli tasarım kararı — dormant servis:** Bu adımda `StatService` hiçbir
      call-site'a (GameManager/RequirementService/DiceManager) bağlanmadı; sadece API
      kuruldu. Ayrıca `GameEvents`'e (`OnResourcesUpdated`/`OnStatCritical`) bilinçli
      olarak bağlanmıyor — sınır bilgisi yalnızca dönüş değeriyle (`StatBoundary`)
      veriliyor, event fırlatma Katman 3'te `GameManager`'ın işi olacak (servisi Unity
      statik'lerinden bağımsız/test edilebilir tutmak için). Bağlama sırası: `GameManager`
      → Katman 3, `RequirementService`'in kendi switch'ini bırakıp buna geçmesi →
      Katman 2 sıradaki adım, `DiceManager` (ve eksik-`money` bug'ının düzelmesi) →
      Katman 4.
      **Yan etki (scope genişlemesi, önceden bildirildi/onaylandı):** `StatType` enum'u
      (5 kaynağı temsil eden) `GameManager.cs`'in içinden (global namespace) çıkarılıp
      `Assets/_Scripts/StatType.cs`'e (`Game.Core`) taşındı — birden fazla servis
      kullanacağı için tek dosyaya özel kalmaması gerekiyordu. Bu taşıma, `StatType`'ı
      niteliksiz (unqualified) kullanan 3 dosyaya (`GameManager.cs`, `SeasonManager.cs`,
      `SeasonScoring.cs`) `using Game.Core;` eklenmesini zorunlu kıldı — bu dosyalarda
      başka hiçbir mantık değişmedi.
      **Bilinen ön-koşul (bu adımın kapsamı dışında):** Proje şu an zaten derlenmiyor —
      hiçbir dosya daha önce `using Game.Core;` içermiyordu ama eski manager katmanı
      (`GameManager`, `SeasonManager`, `SeasonScoring`, `StatsManager`, `CrisisManager`,
      `SaveManager` ...) `GameEvents`/`GameConstants`'ı niteliksiz kullanıyordu (madde 6,
      derlenmeyi engelleyen buglarla aynı kökten, henüz her manager Katman 3/4'te
      yeniden yazılmadığı için). `using Game.Core;` eklenen 3 dosyada bu artık çözüldü;
      geri kalan manager'lar kendi katman sıraları geldiğinde düzelecek.
- [x] `RequirementService.cs` — Tamamlandı: `Assets/_Scripts/RequirementService.cs`
      `Game.Services` namespace'ine taşındı (`StatService` emsaliyle tutarlı; `Game.Data`/
      `Game.Core` bağımlı, dosya kendisi `GameManager`/`StoryManager`'a — global
      namespace, üst katman — geçici yukarı-bağımlı kalıyor, bkz. aşağı). `EvaluateSingle`/
      `GetCurrentInt`'teki eski `CardConstants.*` sabitleri `GameConstants.ReqTypes.*`/
      `GameConstants.Stats.*`'a taşındı (`CardConstants.cs` **silinmedi** —
      `CardDeckBuilder` hâlâ `CardConstants.Category*` kullanıyor).
      **Cooldown entegrasyonu:** Yeni `IsCardEligible(CardData)` eklendi
      (`requirements && !StoryManager.IsOnCooldown`, cooldown her çağrıda canlı
      değerlendirilir, cache'e girmez). `CardRegistry.GetValidCards` ve
      `CardManager.LoadNewCard`'daki **kuyruk yolu** buna bağlandı — havuzdan gelen
      kartlar artık cooldown'a tabi. `CardManager`'daki **zorlanan yol**
      (`_forcedNextCardID`, `successCardID` ile zincir devamı) kullanıcı kararıyla
      **`CheckRequirements`'ta bırakıldı** — cooldown'ı bilinçli olarak bypass eder
      (anlatı akışı kesilmesin diye).
      **Cache invalidation:** `ClearCache()` zaten vardı ama hiç çağrılmıyordu; servis
      kendisi `GameEvents`'e abone OLMUYOR (saf kalması için) — sahibi `CardManager`
      tetikliyor: `OnCardResolved` (flag/variable/cooldown değişebilir),
      `OnResourcesUpdated`/`OnPhaseAdvanced` (parametresiz `Action`) ve
      `OnWeekAdvanced` (`Action<int>`, ayrı bir overload — `HandleRequirementStateChanged()`
      / `HandleRequirementStateChanged(int _)`) hepsinde cache temizleniyor.
      **Bilinçli olarak ERTELENDİ (Katman 3'e):** `GetCurrentInt`'in
      `StatService.TryGet`'e bağlanması yapılmadı — `StatService` hâlâ "dormant" (canlı
      sahibi yok, bkz. yukarıdaki `StatService.cs` maddesi), stat'lar hâlâ
      `GameManager`'ın düz alanlarında. Şimdi bağlansaydı her stat sabit 50 okunurdu
      (sessiz mantık hatası) — bu yüzden `GetCurrentInt` geçici olarak
      `GameManager.Instance.form/money/...` okumaya devam ediyor; değer-kaynağı takası,
      `GameManager` bir `StatService` örneğini sahiplendiğinde (Katman 3) atomik
      yapılacak. Aynı nedenle `RequirementService`'in `GameManager`/`StoryManager`'a
      olan yukarı-bağımlılığı (layer ihlali, global namespace'ten `Game.Services`'e
      inject edilmesi gereken bir bağımlılık) da Katman 3'e erteleniyor.
      **Yan etki (scope genişlemesi, önceden bildirildi/onaylandı):** `CardRegistry.cs`
      ve `CardDeckBuilder.cs`'e (Katman 2) `using Game.Services;` eklendi;
      `CardManager.cs`'e (Katman 3 dosyası) `using Game.Core;`/`using Game.Services;`
      eklendi ve yukarıdaki kuyruk-yolu/cache-invalidation değişiklikleri yapıldı — bu
      dokunuşlar mekanik/minimal, önceki katman adımlarındaki `using` ekleme kalıbıyla
      aynı türden, `CardManager`'ın kendisi henüz yeniden yazılmadı.
- [x] `CardRegistry.cs` — Tamamlandı: `Assets/_Scripts/CardRegistry.cs` → `Assets/_Scripts/
      Services/CardRegistry.cs` (GUID korunarak `git mv`), `Game.Services` namespace'ine
      alındı (`StatService`/`RequirementService` emsaliyle tutarlı), gereksiz `using
      Game.Services;` (kendi namespace'i) kaldırıldı, bozuk Türkçe karakter (`hatas�`)
      düzeltildi. `GetValidCards` artık `reqService.IsCardEligible(c, bypassCooldown:
      false)` çağırıyor — havuz seçimi olduğu için cooldown açıkça uygulanıyor. Bunun
      için `RequirementService.IsCardEligible`'a defaultlu `bool bypassCooldown = false`
      parametresi eklendi (`CardManager.cs:139`'daki tek argümanlı çağrı default sayesinde
      değişmeden çalışıyor, davranışı aynı kalıyor). `CardManager.cs` ve
      `CardDeckBuilder.cs` zaten `using Game.Services;` içerdiği için namespace
      değişiminden etkilenmediler, dokunulmadı.
- [x] `CardDeckBuilder.cs` — Tamamlandı: `Game.Services` namespace'ine alındı
      (`StatService`/`RequirementService`/`CardRegistry` emsaliyle tutarlı, `using
      Game.Services;` gereksizleşti kaldırıldı, `using Game.Core;` eklendi). Eski
      `CardConstants.Category*` sabitleri `GameConstants.Categories.*`'a taşındı —
      dosya artık eski `CardConstants`'a hiç bağımlı değil. **Filtre notu:**
      "cooldown + requirement filtresi gerçekten bağlanacak" maddesi bu adımdan önce
      zaten tamamlanmıştı (`CardRegistry.GetValidCards` → `IsCardEligible`, bkz.
      yukarıdaki `CardRegistry.cs` maddesi); `CardDeckBuilder.BuildSeasonDeck` zaten
      `_registry.GetValidCards(_reqService)` çağırıyordu — burada yalnızca
      **doğrulandı**, yeniden bağlanmadı. Ölü kod temizliği: yalnızca
      `result.Count`'u okumak için var olan `result` listesi kaldırılıp yerine
      `reservedCount` (int) hesaplaması kondu — davranış birebir aynı, sadece bozuk
      Türkçe karakterli yorum (`Ge�ici, sona ekleyece�iz`) da bu vesileyle kalktı.
      Deste kurma algoritmasının kendisi (gap/match/transition/filler yerleştirme)
      değişmedi. `CardConstants.cs` **silinmedi** — `CardUIHandler.cs` (Katman 4)
      hâlâ `SuffixDesc/SuffixRight/SuffixLeft/PhaseIndicator` lokalizasyon eklerini
      kullanıyor; o dosya `GameConstants.Loc.*`'a geçince silinecek. Tek tüketici
      `CardManager.cs` zaten `using Game.Services;` içerdiği ve tipi niteliksiz
      kullandığı için değişmedi.
- [x] `MatchSimulator.cs` — Tamamlandı: `Assets/_Scripts/MatchSimulator.cs` → `Assets/_Scripts/
      Services/MatchSimulator.cs` (GUID korunarak `git mv`, `StatService`/`CardRegistry`
      emsaliyle tutarlı), `Game.Services` namespace'ine alındı, bozuk Türkçe karakterli
      XML doc yorumları temiz UTF-8 karşılıklarıyla değiştirildi. **Algoritma/mantık hiç
      değişmedi** — dosya zaten "örnek kalitede" olduğu için yalnızca taşıma + kozmetik
      temizlik yapıldı. `MatchSimulationConfig` (ScriptableObject) bilinçli olarak global
      namespace'te bırakıldı (diğer config ScriptableObject'lerle — `CardSwipeConfig` —
      aynı muamele; bu adımın kapsamı `MatchSimulator`). Tek tüketici `StatsManager.cs`
      (Katman 3 dosyası) tipi unqualified kullandığı için `using Game.Services;` eklendi
      — başka hiçbir satır değişmedi.
- [x] `DiceService.cs` — Tamamlandı: `Assets/_Scripts/Services/DiceService.cs` (yeni).
      Saf C# sınıf (MonoBehaviour DEĞİL, `StatService`/`MatchSimulator` gibi — sahibi
      Katman 4'te `DiceManager` olacak). Eski `DiceManager`'daki matematiksel çekirdek
      (hedef belirleme 10-15, zar 1-20, stat bonus kademeleri, kritik 1/20 kuralları,
      `total >= target` karşılaştırması) **birebir davranış korunarak** taşındı; yeni
      `DiceMode` enum'u (`High`/`Low`) eski `mode` string parametresinin ("high"/"low")
      yerini alıyor, çözüm tek yerde (`DiceService.ParseMode`). Sonuç, UI'nin ihtiyaç
      duyduğu her alanı taşıyan `readonly struct DiceRoll` olarak dönüyor.
      **Bilinçli tasarım kararı — dormant + yukarı-bağımlılık yok:** `DiceManager`'ın
      `GetStatValue` (→ `GameManager`) ve `GetMomentumBonus` (→
      `StoryManager.GetVariable("ucl_momentum")`) metotları `DiceService`'e taşınmadı;
      `Resolve(statValue, momentumBonus, mode)` bu değerleri parametre olarak alıyor —
      `Game.Services`'in `Game.Gameplay`'e (üst katman) bağımlı olmaması kuralı gereği.
      `DiceManager`'ın kendisi bu adımda değiştirilmedi/bağlanmadı (Katman 4'te UI/animasyon
      dışındaki tüm mantığını `DiceService`'e devredecek).
- [x] `SystemCardFactory.cs` — Tamamlandı: `Assets/_Scripts/Services/SystemCardFactory.cs`
      (yeni, `static` sınıf, durumsuz). Kodda elle kurulan (JSON'dan gelmeyen) 5 synthetic
      `CardData`'yı tek yerden üretir — eskiden `CardManager` (CRISIS_CARD, GAMEOVER_CARD),
      `SeasonManager` (ucl_eliminated, dyn_ucl_group_N) ve `UCLManager` (ucl_final_end_node)
      olmak üzere 3 manager'a dağılmıştı (madde 7-H). Ham string ID/kategori yerine
      `GameConstants.SystemCardIds.*`/`Categories.*`/`Vars.*` kullanıyor. Üretilen kartlar
      mevcut inline sürümlerle **birebir aynı** (davranış parity; hardcoded/lokalize
      edilmemiş kriz-gameover metinleri de dahil — lokalizasyona geçiş ayrı bir iş).
      **Bilinçli olarak dışarıda bırakılanlar (Katman 3'e):** zincir kurma / havuzdan
      rastgele seçim (`UCLManager.AddCardIfExists` → `UclFlowController`'a taşınacak), UI
      yan etkileri (`ApplyTheme`/`ManageBlinkingDisplay`, ilgili manager'da kalacak), kriz
      çıkışının çift işlenmesi (`CrisisManager`+`CardManager`, madde 7-H, manager
      rewrite'ında tek sorumluya indirilecek). **Dormant:** `CardManager`/`SeasonManager`/
      `UCLManager` içindeki inline `new CardData{...}` blokları bu adımda SÖKÜLMEDİ; bu üç
      manager Katman 3'te yeniden yazılırken (`UCLManager` → `UclFlowController`'a
      dönüşürken) fabrikaya bağlanacaklar.

**Katman 2 TAMAMLANDI.**

### Katman 3 — `Game.Gameplay` (manager'lar) — **DEVAM EDİYOR**
- [x] `GameManager.cs` (ince koordinatör; Stat/Life/Phase servislerine delege eder) —
      Tamamlandı: `MonoBehaviour` + elle `Instance` → `Singleton<GameManager>`
      (`OnSingletonAwake`). `StatService` (Katman 2'de dormant yazılmıştı) artık burada
      `new` ile sahiplenilip `public StatService Stats` olarak dışarı açıldı;
      `form/money/fame/relations/focus` `Stats.Get`'e delege eden salt-okunur uyumluluk
      property'lerine dönüştü (5 tüketici dosya — `DiceManager`, `RequirementService`,
      `SaveManager`, `StatsManager`, `TeamManager` — değişmeden derleniyor, hiçbiri bu
      alanlara yazmıyordu). `ApplyStatChange(string/StatType)`/`SetStat(string)` artık
      kendi switch/clamp mantığı yerine `StatService.Apply/Set` + `StatService.TryResolve`
      (string→enum TEK çözücü) kullanıyor; eski `ClampAndCheckStats`/`CheckCrisis`/
      `crisisTriggeredThisTurn` silindi. **Kriz tetikleme kararı:** artık yalnızca
      `HandleCardSwiped` (→ `Stats.ApplyChoice` dönüşündeki ilk `StatBoundaryHit`) kriz
      fırlatıyor; `ApplyStatChange`/`SetStat` (sezon ödülleri, save/load, kriz-reset)
      bilinçli olarak fırlatmıyor — eski `crisisTriggeredThisTurn` bayrağı yalnızca kart
      swipe'ında sıfırlandığı için bu yollar zaten tutarsız/spurious kriz üretiyordu.
      `GameStateMachine` (Katman 0'da yazılmış ama sahipsizdi) artık `GameManager`
      tarafından `new` ile oluşturuluyor (`public GameStateMachine State`),
      `OnStateChanged` → `GameEvents.OnGameStateChanged` köprüsü kuruldu;
      `StartCareer` → `Playing`, can bitince → `GameOver` geçişi eklendi. **Can
      (lives) artık düzgün dışarı açık:** `public int CurrentLives` (salt okunur) +
      `public void RestoreLives(int)` (save/load için) — madde 6'daki derlenmeyi
      engelleyen buglardan biri (`SaveManager`'ın var olmayan `currentLives` alanını
      okuyup yazması) kökten çözüldü. `lifeSystem.OnGameOver`/`OnLifeLost` artık isimli
      metotlarla (`HandleGameOver`/`HandleLifeLost`) bağlanıp `OnDestroy`'da doğru
      çözülüyor (eski kod lambda ile bağlayıp lambda ile çözmeye çalışıyordu — hiç
      çözülmüyordu, latent leak düzeldi). Kullanılmayan `static OnPhaseAdvanced`
      silindi; `AdvanceToNextPhase` artık `GameEvents.OnPhaseAdvanced` fırlatıyor (eski
      static event'e hiçbir dinleyici ulaşamıyordu — `CardManager`'ın cache-invalidation
      aboneliği artık gerçekten tetikleniyor, latent bug düzeldi). `static OnGameOver`
      geriye dönük uyumluluk için korundu (`EndingManager` hâlâ ona abone).
      **Namespace ertelendi (bilinçli):** `GameManager` bu adımda global namespace'te
      kaldı, `Game.Gameplay`'e taşınmadı — 12 tüketici dosya henüz global namespace'te;
      taşıma tüm Gameplay manager'larıyla birlikte tek koordineli geçişte yapılacak.
      **Yan etki (scope genişlemesi, önceden bildirildi/onaylandı):**
      `LifeSystem.cs` `Game.Gameplay` namespace'ine taşındı (somut oyun mekaniği,
      Core'daki altyapı sınıflarından farklı; sahibi `GameManager` ile aynı katmanda
      durması kararlaştırıldı) — bozuk Türkçe karakterli yorumlar temizlendi, ölü
      `ResetStats()` silindi, yeni `SetLives(int)` eklendi (save/load restore için).
      `SaveManager.cs`'e madde 6'daki İKİNCİ derlenmeyi engelleyen bug için de dokunuldu:
      silinmiş `StatsManager.earnedAwards`'a 3 referans (`GameSaveData.earnedAwards`
      alanı + save/load satırları) kaldırıldı (madde 3'teki "award kalıntıları
      temizlenecek" kararıyla tutarlı); `data.lives` artık `CurrentLives`/
      `RestoreLives` üzerinden okunup yazılıyor.
      **Doğrulama:** `dotnet build Assembly-CSharp.csproj` ile derleme kontrolü yapıldı
      (Unity Editor'e ek olarak/onun yerine — bu ortamda kullanılabilir bir CLI yol).
      Değişiklik öncesi 39 benzersiz derleme hatası vardı; sonrasında 35 kaldı, kalan
      35'i bu üç dosyayla ilgisiz (henüz migrate edilmemiş `StoryManager`,
      `CardUIHandler`, `StatsManager`, `UCLManager`, `EndingManager`, `CardController`,
      `SeasonManager`, `CrisisManager`, `TeamManager` — hepsi kendi Katman 3 sırasını
      bekliyor, `GameEvents`'i niteliksiz kullanıyorlar ya da `SeasonManager` eski
      `OnUCLFinalTriggered/Completed` adlarını kullanıyor). `GameManager.cs`/
      `LifeSystem.cs`/`SaveManager.cs`'in kendisi sıfır hata veriyor (yalnızca
      önceden de var olan tarzda zararsız bir `CS0414` uyarısı — kullanılmayan
      `resetValueAfterDeath` alanı, bu adımın kapsamı dışında bırakıldı).
- [x] `CardManager.cs` (synthetic üretim dışarı alınmış, `QueueCount`/`queueCount`
      çiftlemesi giderilmiş) — Tamamlandı: manuel `Instance` yönetimi + `Awake()` →
      `Singleton<CardManager>` (`GameManager` ile birebir aynı kalıp,
      `OnSingletonAwake()`'e taşındı). `LoadCrisisCard`/`EnterGameOverMode` içindeki
      elle kurulan `new CardData{...}` blokları (Katman 2'de yazılmış ama dormant olan)
      `SystemCardFactory.CreateCrisisCard`/`CreateGameOverCard`'a devredildi — UI yan
      etkileri (`ManageBlinkingDisplay`/`ApplyTheme`, fabrika saf/durumsuz olduğu için)
      CardManager'da kaldı. `queueCount` (camelCase, hiç tüketicisi yoktu) silindi,
      `QueueCount` (tek tüketici `SeasonManager`) korundu. `HandleCardResolved`'daki
      `"CRISIS_CARD"` magic string'i `GameConstants.SystemCardIds.Crisis`'e taşındı.
      **Kullanıcı kararı (bu adımda alındı, ertelendi):** `isCrisisActive`/`isGameOver`
      boolean bayraklarının `GameStateMachine`'e taşınması bilinçli olarak
      YAPILMADI — `GameState.UclFinal` ile kriz-sırasında-UCL-finali çakışması ve
      `CrisisManager`'ın (henüz migrate edilmemiş, kendi `activeCrisis`'ini ve çıkış
      yolunu tutan) koordinasyonu gerektirdiği için bu, `CrisisManager`/
      `UclFlowController` adımlarına (madde 7-H "tek-çıkış garantisi") bırakıldı.
      Tüketiciler (`CardController`, `CrisisManager`, `EndingManager`, `SeasonManager`,
      `UCLManager`) **değişmedi** — hepsi `CardManager.Instance.*` üzerinden erişiyor,
      public API/davranış birebir korundu. Namespace bilinçli olarak global kaldı
      (`GameManager` kararıyla tutarlı, tüm Gameplay manager'ları tek koordineli
      geçişte `Game.Gameplay`'e taşınacak).
      **Doğrulama:** `dotnet build Assembly-CSharp.csproj` — 35 hata (GameManager
      adımındaki baseline ile birebir aynı sayı/dosyalar: `StoryManager`,
      `StatsManager`, `UCLManager`, `CardUIHandler`, `EndingManager`, `TeamManager`,
      `SeasonManager`, `CardController`, `CrisisManager` — henüz migrate edilmemiş
      manager'lar). `CardManager.cs`'e ait sıfır hata.
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
