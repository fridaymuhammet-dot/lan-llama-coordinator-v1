# LAN Llama Coordinator

Windows-first masaüstü uygulaması; güvenilir yerel ağdaki laptopları ve uygun olduğunda cep telefonlarını kullanarak yerel LLM çıkarımını izlemeyi ve koordine etmeyi hedefler.

> **Durum:** Tasarım / başlangıç aşamasında. Bu depoda henüz çalışan uygulama kodu yok. Telefonların ve farklı donanımların ortak bir modeli çalıştırıp çalıştıramayacağı, seçilecek inference backend'iyle doğrulanmalıdır.

## Teknoloji temeli

- **.NET 8 ve WPF** — Windows koordinatör arayüzü
- **C#** — koordinatör ve worker servisleri
- **Android öncelikli mobil worker** — mobil işletim sistemi ve inference backend'i doğrulandıktan sonra eklenecek
- **xUnit** — birim testleri

## Depo düzeni

```text
.
├── docs/
│   └── architecture.md                         # Bileşenler, cihazlar ve çalışma modları
├── src/
│   ├── LanLlamaCoordinator.Desktop/            # Windows WPF koordinatör arayüzü
│   ├── LanLlamaCoordinator.Core/               # Modeller, protokoller ve koordinasyon kuralları
│   ├── LanLlamaCoordinator.Infrastructure/     # Ağ, yapılandırma ve inference adaptörleri
│   ├── LanLlamaCoordinator.Worker/             # Laptop/masaüstü arka plan worker'ı
│   └── LanLlamaCoordinator.Mobile/             # Sonraki aşama: mobil worker uygulaması
└── tests/
    └── LanLlamaCoordinator.Core.Tests/         # Core birim testleri
```

## Hedeflenen geliştirme aşamaları

1. Koordinatör, cihaz kaydı ve örnek/simüle worker'larla sağlık görünümü.
2. Kullanıcının açıkça eklediği laptoplarda worker servisi ve kaynak/uyumluluk raporu.
3. Android telefonlarda, uygulama ve donanımın desteklediği ölçüde isteğe bağlı worker desteği.
4. Bir isteği uygun tek cihaza yönlendirme ve kuyruklama.
5. Ancak backend, model ve cihaz uyumluluğu kanıtlandıktan sonra tek bir modelin birden çok cihaz arasında bölünmesini deneme.

## Önemli: dağıtık çalışma biçimleri

Birden fazla cihazda ayrı istekleri çalıştırmak, tek bir büyük modeli cihazlara bölmekle aynı şey değildir. İlk sürümlerde cihaz seçimi, sağlık kontrolü ve istek kuyruğu hedeflenir. Modeli bölerek çalıştırma; uyumlu inference backend'i, model biçimi, donanım belleği ve hızlı/düşük gecikmeli ağ gerektiren ayrı bir araştırma aşamasıdır. Telefonlar otomatik olarak her modelin bir parçasını çalıştırabilir varsayılmamalıdır.

## Güvenlik ve gizlilik

- Cihazlar otomatik olarak ağa katılmaz: kullanıcı onayı ve eşleştirme gerekir; cihaz erişimi sonradan iptal edilebilir.
- Worker protokolü yalnızca tanımlı inference ve sağlık işlemlerini sunmalı; genel amaçlı uzaktan komut çalıştırmamalıdır.
- Ağ trafiği kimliği doğrulanmış ve şifrelenmiş olmalı; sadece güvenilen LAN'da kullanılmalıdır.
- İstek metni ve model verisi worker cihazlara gidebilir; kullanıcı bu aktarımı bilmeli ve izin vermelidir.
- Telefon worker'ı kaynak kullanımını açıkça göstermeli; pil, sıcaklık ve işletim sistemi kısıtlarına uymalıdır. Kullanıcı durdurabilmelidir.

## Geliştirme

Uygulama projeleri henüz eklenmedi. Proje iskeleti eklendikten sonra derleme, çalıştırma ve test komutları eklenecektir.

## Lisans

MIT. Ayrıntılar için [LICENSE](LICENSE) dosyasına bakın.
