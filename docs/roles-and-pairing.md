# Roller ve eşleştirme akışı

## Roller

- **Coordinator:** cihaz kayıtlarını, onayları, heartbeat durumunu ve görev kuyruğunu yönetir.
- **Worker:** kendi kimliği ve yetenekleriyle kayıt isteği gönderir; onaydan sonra sağlık heartbeat'i yollar.
- **CoordinatorAndWorker:** aynı bilgisayar hem koordine eder hem de kendi yerel kaynaklarını worker olarak sunar. Bu seçenek protokol modelinde tanımlıdır; uygulama arayüzü ve gerçek ağ taşıması henüz eklenmemiştir.

İlk sürüm tek coordinator kullanır. Coordinator seçimi açık yapılandırma/kurulum adımı olmalıdır; otomatik master seçimi veya birden çok coordinator arasında uzlaşma kapsam dışıdır. Worker'lar için ayrı uygulama dağıtımı gerekebilir ancak core modelleri ve protokol ortaktır.

## Eşleştirme

1. Worker kayıt isteğinde sabit cihaz kimliği, görünen ad, platform, bağlantı türü ve yeteneklerini bildirir.
2. İstek `PendingApproval` durumunda kalır; kayıt isteği tek başına güven oluşturmaz.
3. Coordinator kullanıcısı isteği onaylar veya reddeder.
4. Onaylanan cihaz `Trusted=true` ve başlangıçta `Offline` olarak kaydedilir.
5. Yalnızca onaylı cihazdan gelen geçerli heartbeat cihazı `Online` yapabilir.

## Heartbeat doğrulama

Prototip service CPU/RAM/GPU yüzde aralıklarını, sıcaklığı, cihaz kimliğini ve geleceğe aşırı kaçan zaman damgalarını doğrular. Eski heartbeat cihazı sağlıklı saymaz. Uygulama yeniden başlatıldığında veya bağlantı kesildiğinde cihaz yeniden heartbeat göndermelidir.

> **Sınır:** Bu commit uygulama katmanı ve testleri sağlar; henüz HTTP/TLS sunucusu, ağ üzerinden kimlik doğrulama, sırların güvenli saklanması, kalıcı onay deposu veya görev gönderimi yoktur. Bu servisleri açık LAN portunda kullanmayın. Gerçek ağ taşıması eklenirken TLS, güvenli eşleştirme sırrı, tekrar saldırısı koruması ve iptal edilebilir kimlikler tasarlanmalıdır.
