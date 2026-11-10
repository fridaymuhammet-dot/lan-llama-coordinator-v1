# Desktop prototipi

İlk WPF ekranı cihazları yerel olarak ekleme/kaldırma ve temel bilgilerini listeleme işlevi sağlar.

## Bilinen sınırlar

- Kayıtlar şu anda uygulama belleğinde tutulur; uygulama yeniden başlatıldığında kaybolur.
- Worker'larla ağ iletişimi, kimlik doğrulamalı eşleştirme, heartbeat alımı ve gerçek sağlık durumu güncellemesi henüz uygulanmadı.
- Ekrandan kaldırma yalnızca yerel kayıt listesini temizler; bağlı cihaza uzaktan durdurma/iptal komutu göndermez.
- Yeni kayıtlar `Offline` ve güvenilmeyen olarak oluşturulur. Ağ bağlantısı varmış gibi gösterilmez.

Bir sonraki adım kalıcı cihaz deposu ve eşleştirme/heartbeat API'sini eklemek olmalıdır.
