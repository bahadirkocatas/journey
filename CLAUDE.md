# Proje: Futbol Kariyer Kart Oyunu (Reigns tarzı)

Unity + C#. Kart kaydırmalı, faz tabanlı futbol kariyer simülasyonu. Solo geliştirici,
büyük ölçekli bir mimari refactor sürecinin ortasındayız.

**Devam eden refactor için bkz. `docs/REFACTOR_PLAN.md`.** Her yeni iş bu plana uygun
olmalı. Plandan sapman gereken bir durum çıkarsa önce orada gerekçelendir (yeni madde
olarak ekle), sonra kodu yaz.

## Kod Standardı

- En profesyonel, endüstri standardı Unity/C# kodu yaz. Kör onaylama — daha iyi/daha
  doğru bir yol varsa öner, gerekçesini kısaca açıkla.
- Yorumlar Türkçe, kod (değişken/metot/sınıf adları) İngilizce ve PascalCase/camelCase
  standardında.
- Tüm dosyalar UTF-8 (BOM'suz).

## Mimari Kararlar (değişmez — sorgulamadan uygula)

- **Namespace katmanları, bağımlılık yönü tek yönlü:**
  `Game.Data` (bağımlılıksız) → `Game.Core` (Data) → `Game.Services` (Core, Data) →
  `Game.Gameplay` (Services, Core, Data) → `Game.UI` (Gameplay, Services, Core)
  Üst katman alt katmana bağımlı olabilir, tersi asla olamaz.
- **Singleton:** Tüm manager'lar `Game.Core.Singleton<T>` (sahne içi) veya
  `Game.Core.PersistentSingleton<T>` (DontDestroyOnLoad) sınıflarından türer. Awake
  override edilmez, `OnSingletonAwake()` kullanılır.
- **Durum yönetimi:** Dağınık boolean bayrak yok (`isCrisisActive`, `isGameOver` gibi).
  Tek doğru kaynak `Game.Core.GameStateMachine` + `GameState` enum'u.
- **Event'ler:** Tüm oyun içi iletişim `Game.Core.GameEvents` üzerinden. Yeni statik
  event dosyalara dağıtılmaz, hepsi bu dosyada toplanır.
- **Magic string yok:** Tüm sabitler `Game.Core.GameConstants` altında (Categories,
  ReqTypes, Stats, Flags, Vars, SystemCardIds gibi alt sınıflarda).
- **Kart verisi:** `CardData` sağ/sol seçimi için `ChoiceData` alt yapısını kullanır
  (eski kod sağ/sol alanlarını ayrı ayrı kopyalıyordu — bu düzeltildi, tekrarlanmasın).
- **JSON:** Newtonsoft (`com.unity.nuget.newtonsoft-json`). Unity'nin `JsonUtility`'si
  kullanılmaz (dictionary/polimorfizm desteklemiyor).
- **Lokalizasyon:** Unity Localization Package. Eski regex tabanlı manuel parser
  tamamen kaldırılıyor.
- **Sahne yapısı:** Tek sahne, panel aç/kapa ile menü geçişleri. Sahne bölünmüyor.

## Kapsam Dışı (MVP'de YOK — sildik, geri getirme)

- Origin/Story seçim sistemi (`StoryData`, `stories.json`, origin tag'leri). Bilinçli
  olarak silindi, geri eklenmesi istenmedi.
- Ödül (award) sistemi. Zaten sökülmüştü, kalıntılar temizleniyor.

## MVP Sonrası Planlı (ŞİMDİ YAZMA, ama mimariyi buna kapatma)

- Hafızalı dolgu/zincir kartları (geçmiş sezon sonucuna bağlı kartlar)
- Trait sistemi + trait'e bağlı hafızalı kartlar

Bu yüzden: kart kategorileri enum değil string sabit (yeni kategori JSON'dan
eklenebilsin), flag/variable deposu (`StoryManager`) generic tutulur.

## Doğrulama Alışkanlığı

Her faz sonunda: Unity Editor konsolunda derleme hatası kontrolü → temizse
`git commit`. Commit mesajı hangi katman/dosya grubunun bittiğini belirtir.
