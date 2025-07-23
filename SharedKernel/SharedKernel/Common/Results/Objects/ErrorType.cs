namespace SharedKernel.Common.Results.Objects;

using System.ComponentModel.DataAnnotations;

public enum ErrorType
{
    [Display(Name = "Yok")]
    None,

    [Display(Name = "Doğrulama Hatası")]
    Validation,

    [Display(Name = "Domain Kural Hatası")]
    Domain,

    [Display(Name = "İş Kural Hatası")]
    Business,

    [Display(Name = "Altyapı Hatası")]
    Infrastructure,

    [Display(Name = "Beklenmeyen Hata")]
    Unexpected,

    [Display(Name = "Bulunamadı")]
    NotFound,

    [Display(Name = "Yetki Yok (401)")]
    Unauthorized,

    [Display(Name = "Erişim Reddedildi (403)")]
    Forbidden,

    [Display(Name = "Çakışma (409)")]
    Conflict,

    [Display(Name = "Zaman Aşımı")]
    Timeout,

    [Display(Name = "İstek İptal Edildi")]
    Canceled,

    [Display(Name = "Dış Servis Hatası")]
    DependencyFailure,

    [Display(Name = "Konfigürasyon Hatası")]
    Configuration,

    [Display(Name = "Serileştirme/Parse Hatası")]
    Serialization,

    [Display(Name = "Harici Servis Hatası")]
    External,

    [Display(Name = "İş Kuralı Hatası")]
    BusinessRule,

    [Display(Name = "Sınır Aşıldı (429)")]
    RateLimitExceeded,

    [Display(Name = "Kopya Kayıt")]
    Duplicate,

    [Display(Name = "Önkoşul Sağlanmadı")]
    PreconditionFailed,

    [Display(Name = "Kimlik Doğrulama Hatası")]
    Authentication,

    [Display(Name = "Yetkilendirme Hatası")]
    Authorization,

    [Display(Name = "Servis Kullanılamıyor")]
    ServiceUnavailable,

    [Display(Name = "Entegrasyon Hatası")]
    Integration,

    [Display(Name = "Desteklenmeyen İşlem")]
    UnsupportedOperation,

    [Display(Name = "Güvenlik Hatası")]
    Security,

    [Display(Name = "Audit Kaydı Hatası")]
    AuditFailure,

    [Display(Name = "Önbellek Hatası")]
    Cache,

    [Display(Name = "Mesajlaşma Hatası")]
    Messaging,

    [Display(Name = "Dosya Sistemi Hatası")]
    FileSystem,

    [Display(Name = "Ağ Hatası")]
    Network,

    [Display(Name = "Veritabanı Hatası")]
    Database
}
