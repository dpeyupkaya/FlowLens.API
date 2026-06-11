# FlowLens

FlowLens, GitHub depolarındaki C# projelerini analiz ederek kod yapısını, ilişkileri ve temel metrikleri çıkaran bir .NET Web API projesidir. GitHub OAuth ile kullanıcı girişi yapar, erişilebilir repoyu indirir, Roslyn tabanlı analiz motoru ile sınıf/metot/bağımlılık ilişkilerini işler ve sonucu grafik veri yapısı olarak döndürür.

## Özellikler

- GitHub OAuth ile güvenli kullanıcı girişi
- Public ve yetkili private GitHub repoları için erişim kontrolü
- C# kaynak kodlarını Roslyn ile statik analiz etme
- Kod yapısı, ilişkiler ve metrikler için graph çıktısı üretme
- SignalR üzerinden analiz ilerleme logları
- Kullanıcı bazlı günlük analiz limiti
- PostgreSQL üzerinde kullanıcı ve ayar verisi saklama
- JWT, şifreli cookie, antiforgery token ve rate limiting desteği
- Scalar/OpenAPI ile geliştirme ortamında API dokümantasyonu

## Teknolojiler

- .NET 9
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- MediatR
- FluentValidation
- Roslyn / Microsoft.CodeAnalysis
- SignalR
- GitHub REST API
- Scalar OpenAPI

## Proje Yapısı

```text
FlowLens.API/             HTTP API, controller'lar, middleware ve uygulama başlangıcı
FlowLens.Application/     CQRS handler'ları, DTO'lar, validator'lar ve arayüzler
FlowLens.Domain/          Entity'ler, ortak domain modelleri ve repository arayüzleri
FlowLens.Infrastructure/  GitHub servisleri, JWT, şifreleme, Roslyn analiz motoru, SignalR
FlowLens.Persistence/     EF Core DbContext, repository implementasyonları ve veritabanı kayıtları
```

## Gereksinimler

- .NET 9 SDK
- PostgreSQL
- GitHub OAuth App

GitHub OAuth App için callback URL değeri, frontend veya istemci uygulamanızın kullandığı callback adresiyle aynı olmalıdır. API tarafında bu değer `GitHub:RedirectUri` olarak okunur.

## Kurulum

Repoyu klonlayın:

```bash
git clone https://github.com/dpeyupkaya/FlowLens.git
cd FlowLens
```

Bağımlılıkları geri yükleyin:

```bash
dotnet restore FlowLens.slnx
```

Gerekli konfigürasyon değerlerini user-secrets ile ekleyin:

```bash
dotnet user-secrets init --project FlowLens.API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=flowlens;Username=postgres;Password=your-password" --project FlowLens.API
dotnet user-secrets set "GitHub:ClientId" "your-github-client-id" --project FlowLens.API
dotnet user-secrets set "GitHub:ClientSecret" "your-github-client-secret" --project FlowLens.API
dotnet user-secrets set "GitHub:RedirectUri" "https://localhost:5173/auth/callback" --project FlowLens.API
dotnet user-secrets set "JwtSettings:Secret" "your-long-random-jwt-secret" --project FlowLens.API
dotnet user-secrets set "JwtSettings:Issuer" "FlowLens" --project FlowLens.API
dotnet user-secrets set "JwtSettings:Audience" "FlowLensClient" --project FlowLens.API
dotnet user-secrets set "JwtSettings:ExpiryInMinutes" "10080" --project FlowLens.API
dotnet user-secrets set "SecuritySettings:CookieEncryptionKey" "your-cookie-protector-key" --project FlowLens.API
dotnet user-secrets set "Encryption:Key" "base64-encoded-32-byte-aes-key" --project FlowLens.API
```

AES anahtarı üretmek için örnek PowerShell komutu:

```powershell
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Maximum 256 }))
```

Veritabanını hazırlayın:

```bash
dotnet ef database update --project FlowLens.Persistence --startup-project FlowLens.API
```

Uygulamayı çalıştırın:

```bash
dotnet run --project FlowLens.API
```

Geliştirme ortamında API dokümantasyonu:

```text
https://localhost:7209/scalar/v1
http://localhost:5225/scalar/v1
```

## Konfigürasyon

| Anahtar | Açıklama |
| --- | --- |
| `ConnectionStrings:DefaultConnection` | PostgreSQL bağlantı adresi |
| `GitHub:ClientId` | GitHub OAuth App client ID değeri |
| `GitHub:ClientSecret` | GitHub OAuth App client secret değeri |
| `GitHub:RedirectUri` | OAuth dönüş adresi |
| `JwtSettings:Secret` | JWT imzalama anahtarı |
| `JwtSettings:Issuer` | JWT issuer değeri |
| `JwtSettings:Audience` | JWT audience değeri |
| `JwtSettings:ExpiryInMinutes` | Token geçerlilik süresi |
| `SecuritySettings:CookieEncryptionKey` | Auth cookie içindeki JWT'yi korumak için kullanılan Data Protection amacı |
| `Encryption:Key` | GitHub access token gibi hassas verileri şifrelemek için Base64 AES anahtarı |

## API Uçları

| Metot | Endpoint | Açıklama |
| --- | --- | --- |
| `GET` | `/api/auth/github-url` | GitHub OAuth giriş URL'si üretir |
| `POST` | `/api/auth/github-login` | GitHub OAuth `code` ve `state` bilgisiyle oturum açar |
| `POST` | `/api/auth/logout` | Oturumu kapatır |
| `GET` | `/api/users/me` | Giriş yapan kullanıcının profil ve ayar bilgilerini getirir |
| `PUT` | `/api/users/me/settings` | Kullanıcı analiz/görselleştirme/veri tercihlerini günceller |
| `GET` | `/api/github/csharp-repos` | Kullanıcının C# GitHub repolarını listeler |
| `POST` | `/api/analysis/start` | Seçilen GitHub deposu için statik analiz başlatır |

Analiz isteği örneği:

```json
{
  "repoUrl": "https://github.com/dotnet/samples",
  "ignoredFolders": ["bin", "obj", "Migrations"],
  "maxDepth": 3,
  "timezoneOffsetMinutes": 180,
  "analysisId": "client-generated-analysis-id"
}
```

## SignalR

Analiz ilerleme logları SignalR üzerinden yayınlanır.

```text
/analysisHub
```

İstemci tarafı aynı `analysisId` grubuna katılarak `ReceiveAnalysisLog` event'ini dinleyebilir.

## Güvenlik Notları

- JWT doğrudan istemciye açık şekilde saklanmaz; şifreli HTTP-only cookie içinde tutulur.
- Antiforgery token `Xflwns-snwf` cookie'si ve `X-Xflwns-snwf` header'ı ile çalışır.
- GitHub access token veritabanında AES ile şifrelenerek saklanır.
- Anonim istekler ve giriş yapan kullanıcılar için ayrı rate limit politikası bulunur.
- Repo indirme ve zip çıkarma işlemlerinde dosya boyutu, dosya sayısı ve izin verilen uzantılar sınırlandırılır.

## Geliştirme

Projeyi derlemek için:

```bash
dotnet build FlowLens.slnx
```

Kodda kullanılan temel akış:

1. Kullanıcı GitHub OAuth ile giriş yapar.
2. GitHub token şifrelenerek veritabanında saklanır.
3. Kullanıcı analiz edilecek repoyu seçer.
4. API repo erişimini doğrular ve kaynak kodu geçici dizine indirir.
5. Roslyn analiz motoru C# dosyalarını tarar.
6. Analiz sonucu graph ve rapor verisi olarak döndürülür.

## Lisans

Bu proje MIT lisansı ile lisanslanmıştır. Detaylar için `LICENSE.txt` dosyasına bakın.
