# LAN Llama Coordinator

Windows-first masaüstü uygulaması; güvenilir yerel ağdaki laptopları ve uygun olduğunda cep telefonlarını kullanarak yerel LLM çıkarımını izlemeyi ve koordine etmeyi hedefler.

> **Durum:** Erken prototip. .NET çözüm iskeleti, Core modelleri/servisleri ve WPF ekranı repoda bulunuyor. Ancak worker kayıt ve heartbeat kodu henüz gerçek HTTP/TLS ağına bağlı değil; UI'daki cihaz ekleme ve görev işlemleri yerel prototip davranışıdır, gerçek inference çalıştırmaz.

## Teknoloji temeli

- **.NET 8 ve WPF** — Windows koordinatör arayüzü
- **C#** — koordinatör ve worker servisleri
- **Android öncelikli mobil worker** — mobil işletim sistemi ve inference backend'i doğrulandıktan sonra eklenecek
- **xUnit** — birim testleri

## Depo düzeni

```text
.
├── docs/
│   ├── architecture.md
│   ├── network-api-design.md                  # Worker API sözleşmesi ve güvenlik tasarımı
│   ├── device-management.md
│   ├── worker-protocol.md
│   └── roles-and-pairing.md
├── src/
│   ├── LanLlamaCoordinator.Desktop/           # Windows WPF koordinatör arayüzü
│   ├── LanLlamaCoordinator.Core/               # Modeller, protokoller ve koordinasyon kuralları
│   ├── LanLlamaCoordinator.Infrastructure/     # Ağ, yapılandırma ve inference adaptörleri
│   ├── LanLlamaCoordinator.Worker/             # Laptop/masaüstü worker prototipi
│   └── LanLlamaCoordinator.Mobile/              # Sonraki aşama: mobil worker
└── tests/
    └── LanLlamaCoordinator.Core.Tests/
```

## Hedeflenen geliştirme aşamaları

1. Coordinator, simüle worker'lar ve sağlık görünümü.
2. Tek Windows Worker: onaylı eşleştirme, HTTPS heartbeat ve durum görünümü.
3. Önceden başlatılmış, yerel Worker'a bağlı inference backend'ine tek isteği yönlendirme.
4. Birden fazla laptop için kuyruk ve failover.
5. Android worker ve çok cihazlı model bölme araştırması; yalnızca backend/cihaz ölçümleri destekliyorsa.

## Dağıtık çalışma biçimleri

Birden fazla cihazda ayrı istekleri çalıştırmak, tek bir büyük modeli cihazlara bölmekle aynı şey değildir. İlk sürümde bir istek tek uyumlu Worker'a gönderilir. Model bölme; backend, model biçimi, bellek ve ağ uyumluluğu ölçülmeden hedeflenmez.

## Güvenlik ve gizlilik

- Cihazlar onaylı eşleştirme olmadan güvenilir sayılmaz.
- Ağ iletişimi uygulanırken HTTPS/TLS, Worker başına iptal edilebilir kimlik ve Coordinator sertifika doğrulaması zorunludur. “LAN içinde” olmak tek başına güvenlik sağlamaz.
- Worker sadece tanımlı inference ve sağlık işlemlerini sunmalı; genel amaçlı uzaktan komut çalıştırmamalıdır.
- Prompt ve sonuç Worker'a aktarılabileceğinden kullanıcı hangi cihazın çalıştıracağını görmeli; hassas içerik varsayılan loglardan çıkarılmalıdır.
- Worker ve model backend'leri internete açılmamalı; portlar güvenlik duvarıyla sınırlandırılmalıdır.

## Geliştirme

Gerçek ağ API'si henüz uygulanmadı. Tasarım ve endpoint sözleşmesi için [Network API design](docs/network-api-design.md) ve [Architecture](docs/architecture.md) belgelerine bakın. Derleme/çalıştırma/test adımları, ilk doğrulanmış build'den sonra eklenecektir.

## Lisans

MIT. Ayrıntılar için [LICENSE](LICENSE) dosyasına bakın.
