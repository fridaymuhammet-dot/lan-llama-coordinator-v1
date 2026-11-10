# LAN Llama Coordinator

Windows-first masaüstü uygulaması: güvenilir yerel ağdaki `llama.cpp` sunucularını ve RPC worker'larını tek yerden izlemek ve koordine etmek.

> **Durum:** Başlangıç aşamasında. Bu depoda henüz çalışan uygulama kodu yok; aşağıdaki yapı ilk geliştirme adımları için temel oluşturur.

## Teknoloji temeli

- **.NET 8 ve WPF** — Windows masaüstü arayüzü
- **C#** — uygulama ve servis kodu
- **xUnit** — birim testleri

## Depo düzeni

```text
.
├── docs/
│   └── architecture.md              # Bileşenler ve temel tasarım kararları
├── src/
│   ├── LanLlamaCoordinator.Desktop/ # WPF uygulaması ve kullanıcı arayüzü
│   ├── LanLlamaCoordinator.Core/    # Alan modelleri, arayüzler ve koordinasyon kuralları
│   └── LanLlamaCoordinator.Infrastructure/ # Ağ, llama.cpp ve yapılandırma adaptörleri
└── tests/
    └── LanLlamaCoordinator.Core.Tests/ # Core birim testleri
```

## Hedeflenen ilk sürüm

1. Worker ve sunucu adreslerini yapılandırabilmek.
2. LAN bileşenlerinin erişilebilirlik durumunu göstermek.
3. `llama.cpp` sunucu durumunu izlemek.
4. Worker'ları ve temel sağlık bilgilerini tek ekranda görüntülemek.
5. Ağ hatalarını anlaşılır biçimde kaydetmek ve göstermek.

İlk aşamada otomatik görev yönlendirme veya uzaktan komut çalıştırma kapsam dışıdır; önce güvenilir izleme ve yapılandırma hedeflenir.

## Güvenlik notu

Uygulama güvenilir LAN kullanımı için tasarlanıyor. LAN keşfi veya herhangi bir servis erişimi eklendiğinde, güven sınırları ve erişim kontrolleri ayrıca tasarlanmalıdır. Uygulamayı internete açık biçimde çalıştırmayın.

## Geliştirme

Uygulama projeleri henüz eklenmedi. Proje iskeleti eklendikten sonra bu bölüme derleme, çalıştırma ve test komutları konacaktır.

## Lisans

MIT. Ayrıntılar için [LICENSE](LICENSE) dosyasına bakın.
