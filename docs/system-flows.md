# Sistem akışları ve çalışma mantığı

## 1. Cihaz kaydı ve ekleme akışı

```text
Kullanıcı cihaz ekler
  -> Worker, koordinatöre register isteği gönderir
  -> Koordinatör cihaz kimliğini ve yetenekleri doğrular
  -> Cihaz bir güvenli cihaz listesine eklenir
  -> Koordinatör cihaz için heartbeat izleme başlatır
  -> Cihaz online olarak iş atamalarına açılır
```

## 2. Cihaz çıkartma ve kaldırma akışı

```text
Cihaz kapatılır / kullanıcı çıkarır
  -> Worker son heartbeat'i gönderir
  -> Koordinatör cihaz durumunu "offline" olarak işaretler
  -> Çalışan iş varsa:
       - tamamlanabilir mi kontrol edilir
       - tamamlanamıyorsa farklı uygun worker'a aktarılır
       - aktarılamıyorsa iş güvenli şekilde iptal edilir
  -> Cihaz cihaz listeden kaldırılır
```

## 3. Sağlık kontrol akışı

```text
Her 10-20 saniyede bir:
  -> Worker; CPU, RAM, GPU, sıcaklık, bellek ve yük durumunu rapor eder
  -> Koordinatör cihazın kullanılabilirliğini değerlendirir
  -> Aşağıdaki koşullarda cihaz uygunluk dışı sayılır:
       - çok yüksek CPU / GPU yükü
       - bellek doluluk oranı kritik seviyede
       - sıcaklık limitlerine yaklaşma
       - heartbeat süresi aşımı
       - model uyumluluğu yok
```

## 4. İş dağıtım akışı

```text
Yeni iş oluşturulur
  -> Koordinatör model uyumluluğunu kontrol eder
  -> Kullanılabilir cihazları filtreler
  -> Aday cihazlar skorlanır:
       1. Ethernet > Wi‑Fi > Mobil
       2. Yüksek hafıza / GPU uygunluğu
       3. Düşük gecikme
       4. Daha düşük anlık yük
  -> En uygun cihaz seçilir
  -> İş worker'a atanır
  -> Worker iş durumunu bildirir
  -> Sonuç koordinatöre gönderilir
```

## 5. Failover akışı

```text
Worker iş yapamıyor / cevap vermiyor
  -> Koordinatör cihazı "unhealthy" olarak işaretler
  -> İşi başka uygun worker'a taşır
  -> Başarısız iş için retry kuralı kontrol edilir
  -> İzin verilen deneme sayısını aşarsa işlem reddedilir
```

## 6. Ethernet ve Wi‑Fi politikası

- Ethernet bağlantılı cihazlar öncelikli adaydır.
- Wi‑Fi cihazlar kullanılabilir, ama gecikme ve bağlantı kararsızlığı nedeniyle daha aşağı öncelik alır.
- Mobil cihazlar yalnızca uygunluk doğrulandıktan sonra dahil edilir.
- Kablolu cihazların eklenmesi sistemi yavaşlatmaz; çoğu durumda gecikmeyi düşürür.

## 7. İlk görev sınıflandırması

### A. Uygun ilk sürüm davranışı

- Tek iş / tek uygun worker
- Çoklu laptop koordinasyonu
- Tescilli cihazlar
- Health checks
- Retry failover
- Küçük model seti / uyumlu model listesi

### B. Sonraki aşama

- Android/telefon worker desteği
- Uzaktan hedef model yükleme
- Daha gelişmiş kuyruk ve iş öncelik yönetimi
- Model parallelism / çok cihazlı model bölme

## 8. Güvenlik ve erişim

- Cihazların her biri güvenli bir kimlik ile kaydolur.
- Koordinatör, kullanıcı tarafından onaylanan cihazları kabul eder.
- Kaldırılmış cihazların erişimi otomatik yazılır.
- Genel amaçlı uzaktan komut çalıştırma desteklenmez; sadece tanımlı inference ve sağlık iletişimi vardır.
