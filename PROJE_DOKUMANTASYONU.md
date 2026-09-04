# AI-Powered Automated Playtesting & Level Difficulty Analyzer

Unity içinde çalışan, puzzle level'lerini otomatik oynayıp zorluk seviyesini ölçen bir sistem. Bu doküman, projenin mimarisini, her parçanın görevini ve projeyi nasıl genişleteceğini özetler.

## 1. Genel Mimari

Sistem şu pipeline üzerinden çalışır:

```
LevelScanner  ->  IInteractable objeler  ->  SearchEngine (BFS)
      |                                            |
      v                                            v
  GameState  <---------------------------  cozum (action listesi)
      |
      v
BotBrain / PlayerSimBrain (State Machine: IDLE -> SCANNING -> PLANNING -> EXECUTING -> EVALUATING)
      |
      v
LevelManager (Win/Fail event'leri)  ->  TestResult'lar
      |
      v
MetricsCollector  ->  MetricsSummary
      |
      v
DifficultyAnalyzer  ->  DifficultyReport (0-10 skor + siniflandirma)
      |
      v
PlaybotTesterWindow (EditorWindow UI)  +  TestReporter (JSON/TXT kaydi)
```

`MultiLevelTestRunner`, bu tüm zinciri her level için sırayla çalıştıran orkestra şefidir.

## 2. Klasör Yapısı

```
Assets/
  Editor/              -> Sadece Unity Editor'a ait scriptler (PlaybotTesterWindow)
  Scripts/             -> Tum runtime kodu
  Reports/             -> Test sonuclari (JSON/TXT), otomatik olusturulur
  Scenes/               -> SampleScene
```

## 3. Class'ların Görevleri

| Class | Görevi |
|---|---|
| `IInteractable` | Tıklanabilir/etkileşilebilir her objenin uyması gereken sözleşme (`Interact()`). |
| `IResettable` | Test tekrarları arasında sıfırlanması gereken objelerin sözleşmesi (`ResetState()`). |
| `ButtonInteraction`, `KeyInteraction`, `DoorInteraction`, `TrapInteraction` | Somut interactable türleri. `DoorInteraction` bir `List<KeyInteraction>` bekler (AND mantığı). |
| `LevelManager` | Tek bir levelin win/fail durumunu tutar, `OnLevelCompleted`/`OnLevelFailed` event'lerini yayınlar (Singleton, `Instance` üzerinden erişilir). |
| `LevelScanner` | Sahnedeki tüm `IInteractable` objeleri bulur, `GameState` (o anki durum) üretir. |
| `GameState` | Anlık durumun (level status + interactable listesi) veri kutusu. |
| `LevelDefinition` | Bir levelin arama kuralları için soyut, veri-güdümlü tanım (`KeyNames`, `DoorName`, `RequiredKeysForDoor`, `TrapNames`, `DecoyNames`). Inspector'dan doldurulur. |
| `SearchState`, `SearchEngine` | BFS ile, `LevelDefinition`'a bakarak (gerçek sahneye dokunmadan) en kısa çözüm yolunu zihinsel olarak arar. |
| `BotBrain` | Solver Agent — bulunan çözümü harfiyen uygular, hiç hata yapmaz. |
| `PlayerSimBrain` | Player Simulation Agent — çözümü rehber alır ama `mistakeChance` olasılıkla bilerek yanlış/rastgele hamle yapar. |
| `TestResult` | Tek bir test koşusunun sonucu (Success, Steps, Time, WrongMoves). |
| `MetricsSummary`, `MetricsCollector` | Birden fazla `TestResult`'ı istatistiksel özete (başarı oranı, ortalama adım vb.) çevirir. |
| `DifficultyReport`, `DifficultyAnalyzer` | Solver + Player özetlerinden 0-10 arası bir Difficulty Score ve sınıflandırma (EASY/MEDIUM/HARD/VERY HARD) üretir. |
| `LevelTestConfig` | Bir levelin testi için gereken her şeyi (isim, root obje, reset edilecek key'ler, level tanımı) bir arada tutan veri paketi. |
| `MultiLevelTestRunner` | Birden fazla level'i sırayla aktif edip test eden orkestra şefi; sonuçları statik alanlarda tutar. |
| `TestReporter` | Sonuçları `.json` ve `.txt` olarak `Assets/Reports/` altına kaydeder. |
| `PlaybotTesterWindow` | `Tools > Playbot Tester` menüsünden açılan EditorWindow; testi başlatır, sonuçları gösterir. |
| `LevelResultListener` | Event-driven mimariyi göstermek için win/fail event'lerine abone olup Console'a log basan basit bir örnek. |

## 4. Nasıl Çalışır (Uçtan Uca)

1. `PlaybotTesterWindow`'da **START TEST**'e basılır -> Play Mode başlar.
2. `MultiLevelTestRunner.Start()` çalışır, `levels` listesindeki her `LevelTestConfig` için sırayla:
   - İlgili levelin `levelRoot`'u aktif, diğerleri pasif yapılır.
   - `SearchEngine`, o levelin `LevelDefinition`'ına bakarak BFS ile çözümü bulur.
   - `solverTestCount` kez: level resetlenir, `BotBrain.RunTest()` bulunan çözümü uygular, sonuç kaydedilir.
   - `playerTestCount` kez: level resetlenir, `PlayerSimBrain.RunTest()` aynı çözümü rehber alıp bazen hata yaparak uygular, sonuç kaydedilir.
   - `MetricsCollector` iki ayrı özet (`solverSummary`, `playerSummary`) üretir.
   - `DifficultyAnalyzer` bu özetlerden bir `DifficultyReport` üretir.
   - `TestReporter` sonucu JSON/TXT olarak kaydeder.
3. Tüm levellar bitince `MultiLevelTestRunner.AllTestsCompleted = true` olur, `PlaybotTesterWindow` bunu görüp sonuçları ekranda listeler.

## 5. Yeni Bir Level Nasıl Eklenir

Mevcut mimari, Key/Door/Trap/Decoy (buton gibi zararsız obje) kalıbına uyan levelleri **kod yazmadan** destekler:

1. Sahnede yeni interactable objeler oluştur (`KeyInteraction`, `DoorInteraction`, `TrapInteraction`, `ButtonInteraction` component'lerini kullanarak), hepsini boş bir `LevelXRoot` objesinin altına topla, başlangıçta pasif bırak.
2. `MultiLevelTestRunner`'ın `Levels` listesine yeni bir eleman ekle:
   - `Level Name`, `Level Root`, `Keys To Reset` (tüm Key objeleri).
   - `Level Definition`: `Key Names`, `Door Name`, `Required Keys For Door`, `Trap Names`, `Decoy Names` — hepsi **gerçek GameObject isimleriyle birebir** eşleşmeli (fazladan boşluk bırakma!).
3. Kaydet, test et.

Not: Şu anki sistem sadece **tek bir Door** kalıbını destekler (birden fazla kapı/oda zinciri için `SearchEngine` ve `LevelDefinition`'ın genişletilmesi gerekir).

## 6. Yeni Bir Interaction Türü Nasıl Eklenir

1. `IInteractable`'ı implement eden yeni bir `MonoBehaviour` script yaz (`Interact()` metodunu doldur).
2. Eğer bu tür bir "state" taşıyorsa (Key gibi) ve testler arası resetlenmesi gerekiyorsa `IResettable`'ı da implement et.
3. Eğer bu yeni tür arama mantığını etkiliyorsa (örneğin "Lever" leveri çekince bir kapı açılıyor gibi), `SearchEngine.Simulate()` metoduna ve `LevelDefinition`'a yeni bir alan/dal eklemen gerekir.

## 7. Bilinen Sınırlamalar / Gelecek İyileştirmeler

- `BotBrain` ve `PlayerSimBrain` çok benzer state machine kodunu tekrar ediyor — ortak bir temel sınıfa (`abstract class BotBase`) çıkarılabilir.
- `SearchEngine` sadece "N anahtar -> 1 kapı" kalıbını destekliyor; daha karmaşık bağımlılık zincirleri (kapı zinciri, lever, sıralı odalar) için genişletilmesi gerekir.
- Sadece **BFS** var; **DFS** ve **A\*** henüz eklenmedi (PHASE 9'da bilinçli olarak ertelendi).
- Sadece **Click** interaction'ı var; **Drag & Drop** eklenmedi (ihtiyaç doğunca eklenecek şekilde tasarlandı, `IInteractable` bunu kısıtlamıyor).
- Difficulty formülü (`(FailureRate x 6) + (WrongMoveFactor x 2) + (SolverLengthFactor x 2)`) başlangıç seviyesinde, sabit ağırlıklarla — daha fazla level test edildikçe kalibre edilmeli.

## 8. Testi Nasıl Çalıştırırım

1. Unity üst menüsünden **Tools > Playbot Tester**.
2. Açılan pencerede **START TEST**'e bas (otomatik Play Mode'a geçer).
3. Console'da ilerlemeyi izleyebilirsin.
4. Bitince EditorWindow'da her level için Difficulty Score görünür.
5. Detaylı sonuçlar `Assets/Reports/` klasöründe `.json` ve `.txt` olarak durur.
