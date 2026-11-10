# Mimari notları

## Amaç

Windows koordinatöründen güvenilir LAN'daki laptop worker'larını, daha sonraki aşamada da uygun Android telefon worker'larını kaydetmek, izlemek ve yerel LLM çıkarımını koordine etmek.

## Bileşenler

- **Desktop:** WPF koordinatör arayüzü; cihazları, kaynakları, izinleri, işleri ve sağlık durumunu gösterir.
- **Core:** Cihaz/iş modelleri, worker protokolü ve seçim kuralları. WPF, işletim sistemi veya ağ kütüphanelerine bağımlı olmamalı.
- **Infrastructure:** Kimliği doğrulanmış LAN iletişimi, yapılandırma, loglama ve inference backend adaptörleri.
- **Worker:** Laptop/masaüstünde çalışacak, yalnızca tanımlı inference ve sağlık protokolünü sunan servis.
- **Mobile:** Sonraki aşama için mobil worker istemcisi. Android ilk hedef; iOS desteği ayrı teknik doğrulamaya tabidir.
- **Core.Tests:** Cihaz seçimi, uygunluk ve izin kurallarının birim testleri.

## Çalışma biçimlerini ayır

1. **İstek dağıtımı:** Her iş tek bir uyumlu worker'a gönderilir. İlk gerçek kullanım için tercih edilen yöntemdir.
2. **Model kopyaları:** Model her cihazda ayrı yüklenir; bağımsız istekler cihazlar arasında paylaştırılır. Her cihazın yeterli belleği gerekir.
3. **Modeli bölme (model parallelism):** Tek bir modelin katmanları/tensörleri cihazlara paylaştırılır. Bu, worker kaydı veya istek dağıtımından farklıdır ve ancak kullanılan inference backend'i bunu cihaz kombinasyonu için destekliyorsa uygulanabilir.

Bir modelin toplam boyutunu cihaz RAM/VRAM değerlerini toplayarak doğrudan karşılanabilir saymayın. Backend uyumluluğu, model biçimi/quantization, bellek yükü, cihazlar arası bant genişliği ve gecikme ölçülmelidir. Telefon desteği donanım ve işletim sistemi kısıtları nedeniyle isteğe bağlıdır; ilk sürümde telefonlar izleme veya sınırlı worker rolüyle başlayabilir.

## Cihaz yaşam döngüsü ve güvenlik

- Worker yalnızca kullanıcı tarafından eşleştirme/onay sonrası kaydedilir; otomatik açık ağ keşfi varsayılan olmamalıdır.
- Her worker kimliği doğrulanmalı; eşleştirme sırrı güvenli saklanmalı ve cihazın erişimi iptal edilebilmelidir.
- Trafik şifrelenmeli; cihazlar internete açılmamalı ve genel amaçlı uzaktan komut/terminal çalıştırma sunulmamalıdır.
- İstek, model dosyası veya hassas veriler ağ üzerinden gönderilecekse bu açıkça belirtilmeli; mümkün olduğunda aktarım en aza indirilmelidir.
- Worker'lar destekledikleri backend/model türlerini ve kullanılabilir kaynakları raporlar. İş seçimi bunlara ve kullanıcı limitlerine göre yapılır.
- Mobil worker pil, sıcaklık, arka plan çalıştırma ve işletim sistemi politikalarına uyar; kullanıcıya durdurma ve kaynak sınırı sunar.

## Aşamalı uygulama planı

1. Protokol, cihaz kaydı ve simüle worker testleri.
2. Tek laptop worker'ı; sağlık kontrolü ve sınırlı bir inference isteği.
3. Birden fazla laptopta istek kuyruğu/dağıtımı; kapasite ve gecikme gözlemi.
4. Android cihazın backend ve cihaz uyumluluğunu doğrulayıp mobil worker prototipi.
5. Ölçümler ve backend desteği uygunsa model bölme deneyi. Bu aşama başarısız olursa istek dağıtımı desteklenmeye devam eder.

